// using FileFlows.Plugin;
// using FileFlows.Shared.Models;
// using System.ComponentModel;
// using System.Reflection.Metadata.Ecma335;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Timers;
// using FileFlows.Plugin.Helpers;
// using FileFlows.Server.Hubs;
// using FileFlows.Services;
// using FileFlows.ServerShared.Models;
// using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;
// using TimeHelper = FileFlows.ServerShared.Helpers.TimeHelper;
//
// namespace FileFlows.Server.Workers;
//
// /// <summary>
// /// A watched library is a folder that imports files into FileFlows
// /// </summary>
// public class WatchedLibrary:IDisposable
// {
//     /// <summary>
//     /// The file system watcher that watches for file system events
//     /// </summary>
//     private FileSystemWatcher Watcher;
//
//     /// <summary>
//     /// The string helper instance
//     /// </summary>
//     private StringHelper _StringHelper;
//     
//     /// <summary>
//     /// Gets or sets the library being watched
//     /// </summary>
//     public Library Library { get;private set; } 
//
//     /// <summary>
//     /// Gets or sets if the scan is complete
//     /// </summary>
//     public bool ScanComplete { get;private set; }
//     
//     /// <summary>
//     /// Gets if the scanner should be used instead of file watched for this library
//     /// </summary>
//     public bool UseScanner { get; private set; }
//     
//     /// <summary>
//     /// If this is disposed or not
//     /// </summary>
//     private bool Disposed;
//
//     /// <summary>
//     /// The scan mutex
//     /// </summary>
//     private readonly Mutex ScanMutex = new ();
//
//     /// <summary>
//     /// The queue of files to process
//     /// </summary>
//     private readonly Queue<string> QueuedFiles = new ();
//
//     private readonly System.Timers.Timer QueueTimer;
//     
//     /// <summary>
//     /// The lgoger to use
//     /// </summary>
//     private Plugin.ILogger Logger { get; init; }
//
//     /// <summary>
//     /// Constructs a instance of a Watched Library
//     /// </summary>
//     /// <param name="logger">the logger to use</param>
//     /// <param name="library">The library to watch</param>
//     public WatchedLibrary(Plugin.ILogger logger, Library library)
//     {
//         this.Logger = logger;
//         this.Library = library;
//         this.UseScanner = library.Scan;
//         this._StringHelper = new(logger);
//
//         if (Directory.Exists(library.Path) == false)
//         {
//             Logger.WLog("Library does not exist, falling back to scanner: " + library.Path);
//             this.UseScanner = true;
//         }
//
//         if(UseScanner == false)
//             SetupWatcher();
//         
//         
//         QueueTimer = new();
//         QueueTimer.Elapsed += QueueTimerOnElapsed;
//         QueueTimer.AutoReset = true;
//         QueueTimer.Interval = 5 * 60 * 1000;
//         QueueTimer.Start();
//     }
//
//
//     private void LogQueueMessage(string message, Settings? settings = null)
//     {
//         if (settings == null)
//             settings = ServiceLoader.Load<ISettingsService>().Get().Result;
//
//         if (settings?.LogQueueMessages != true)
//             return;
//         
//         Logger.DLog(message);
//     }
//
//     /// <summary>
//     /// Fired when the queue timer elapses
//     /// </summary>
//     /// <param name="sender">the sender</param>
//     /// <param name="e">the event args</param>
//     private void QueueTimerOnElapsed(object? sender, ElapsedEventArgs e)
//         => ProcessQueue();
//
//     private readonly SemaphoreSlim processorLock= new (1);
//     
//     /// <summary>
//     /// Processes the queue
//     /// </summary>
//     private void ProcessQueue()
//     {
//         if (processorLock.Wait(1000) == false)
//             return;
//
//         Task.Run(() =>
//         {
//             try
//             {
//                 while (true)
//                 {
//                     lock (QueuedFiles)
//                     {
//                         if (QueuedFiles.Any() == false)
//                             break;
//                     }
//                     ProcessQueuedItem();
//                 }
//             }
//             catch (Exception)
//             {
//             }
//             finally
//             {
//                 processorLock.Release();
//             }
//         });
//     }
//
//     private void Worker_DoWork(object? sender, DoWorkEventArgs e)
//     {
//         while (Disposed == false)
//         {
//             ProcessQueuedItem();
//             if(QueuedHasItems() != true)
//             {
//                 LogQueueMessage($"{Library.Name} nothing queued");
//                 Thread.Sleep(1000);
//             }
//         }
//     }
//
//     private void ProcessQueuedItem()
//     {
//         try
//         {
//             string? fullpath;
//             lock (QueuedFiles)
//             {
//                 if (QueuedFiles.TryDequeue(out fullpath) == false)
//                     return;
//             }
//
//             LogQueueMessage($"{Library.Name} Dequeued: {fullpath}");
//
//             if (CheckExists(fullpath) == false)
//             {
//                 Logger.DLog($"{Library.Name} file does not exist: {fullpath}");
//                 return;
//             }
//
//             if (Library.TopLevelOnly)
//             {
//                 if (Library.Folders)
//                 {
//                     var dir = new DirectoryInfo(fullpath);
//                     if (dir.Parent.FullName != Library.Path)
//                     {
//                         return; // only top level files
//                     }
//                 }
//                 else
//                 {
//                     var dir = new FileInfo(fullpath);
//                     if (dir.Directory.FullName != Library.Path)
//                     {
//                         return; // only top level files
//                     }
//                     
//                 }
//             }
//             
//             if (this.Library.ExcludeHidden)
//             {
//                 if (FileIsHidden(fullpath))
//                 {
//                     LogQueueMessage($"{Library.Name} file is hidden: {fullpath}");
//                     return;
//                 }
//             }
//
//             if (IsMatch(fullpath) == false || fullpath.EndsWith("_"))
//             {
//                 LogQueueMessage($"{Library.Name} file does not match pattern or ends with _: {fullpath}");
//                 return;
//             }
//
//             if (fullpath.ToLower().StartsWith(Library.Path.ToLower()) == false)
//             {
//                 Logger.ILog($"Library file \"{fullpath}\" no longer belongs to library \"{Library.Path}\"");
//                 return; // library was changed
//             }
//
//             StringBuilder scanLog = new StringBuilder();
//             DateTime dtTotal = DateTime.UtcNow;
//
//             FileSystemInfo fsInfo = Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);
//
//             var (knownFile, fingerprint, duplicate) = IsKnownFile(fullpath, fsInfo);
//             if (knownFile && duplicate == null)
//                 return;
//
//             string type = Library.Folders ? "folder" : "file";
//
//             if (Library.Folders && Library.WaitTimeSeconds > 0)
//             {
//                 DirectoryInfo di = (DirectoryInfo)fsInfo;
//                 try
//                 {
//                     var files = di.GetFiles("*.*", Library.TopLevelOnly ? SearchOption.TopDirectoryOnly: SearchOption.AllDirectories);
//                     if (files.Any())
//                     {
//                         var lastWriteTime = files.Select(x => x.LastWriteTimeUtc).Max();
//                         if (lastWriteTime > DateTime.UtcNow.AddSeconds(-Library.WaitTimeSeconds))
//                         {
//                             Logger.ILog(
//                                 $"Changes recently written to folder '{di.FullName}' cannot add to library yet");
//                             Thread.Sleep(2000);
//                             QueueItem(fullpath);
//                             return;
//                         }
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     Logger.ILog(
//                         $"Error reading folder '{di.FullName}' cannot add to library yet, will try again: " +
//                         ex.Message);
//                     Thread.Sleep(2000);
//                     QueueItem(fullpath);
//                     return;
//                 }
//             }
//
//             Logger.DLog($"New unknown {type}: {fullpath}");
//
//             if (Library.SkipFileAccessTests == false && Library.Folders == false &&
//                 CanAccess((FileInfo)fsInfo, Library.FileSizeDetectionInterval).Result == false)
//             {
//                 Logger.WLog($"Failed access checks for file: " + fullpath +"\n" +
//                                      "These checks can be disabled in library settings, but ensure the flow can read and write to the library.");
//
//                 _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Information,
//                     $"'{Library.Name}' failed access checks for file", fullpath);
//                 
//                 return;
//             }
//
//
//             long size = Library.Folders ? 0 : ((FileInfo)fsInfo).Length;
//             var lf = new LibraryFile
//             {
//                 Name = fullpath,
//                 RelativePath = GetRelativePath(fullpath),
//                 Status = duplicate != null ? FileStatus.Duplicate : FileStatus.Unprocessed,
//                 IsDirectory = fsInfo is DirectoryInfo,
//                 Fingerprint = fingerprint ?? string.Empty,
//                 OriginalSize = size,
//                 CreationTime = fsInfo.CreationTimeUtc,
//                 LastWriteTime = fsInfo.LastWriteTimeUtc,
//                 Duplicate = duplicate,
//                 HoldUntil = Library.HoldMinutes > 0 ? DateTime.UtcNow.AddMinutes(Library.HoldMinutes) : DateTime.MinValue,
//                 Library = new ObjectReference
//                 {
//                     Name = Library.Name,
//                     Uid = Library.Uid,
//                     Type = Library.GetType()?.FullName ?? string.Empty
//                 },
//                 Order = -1
//             };
//
//             LibraryFile result;
//             if (knownFile)
//             {
//                 // update the known file, we can't add it again
//                 result = ServiceLoader.Load<LibraryFileService>().Update(lf).Result;
//             }
//             else
//             {
//                 ServiceLoader.Load<LibraryFileService>().Insert(lf).Wait();
//                 result = lf;
//             }
//
//             if (result != null && result.Uid != Guid.Empty)
//             {
//                 SystemEvents.TriggerFileAdded(result, Library);
//                 Logger.DLog(
//                     $"Time taken \"{(DateTime.UtcNow.Subtract(dtTotal))}\" to successfully add new library file: \"{fullpath}\"");
//                 
//                 if (ServiceLoader.Load<ISettingsService>().Get()?.Result?.ShowFileAddedNotifications == true)
//                     ClientServiceManager.Instance.SendToast(LogType.Info, "New File: " + result.RelativePath);
//             }
//             else
//             {
//                 Logger.ELog(
//                     $"Time taken \"{(DateTime.UtcNow.Subtract(dtTotal))}\" to fail to add new library file: \"{fullpath}\"");
//             }
//         }
//         catch (Exception ex)
//         {
//             Logger.ELog("Error in queue: " + ex.Message + Environment.NewLine + ex.StackTrace);
//         }
//     }
//
//     private string GetRelativePath(string fullpath)
//     {
//         int skip = Library.Path.Length;
//         if (Library.Path.EndsWith('/') == false && Library.Path.EndsWith('\\') == false)
//             ++skip;
//
//         return fullpath[skip..];
//     }
//
//     private bool MatchesDetection(string fullpath)
//     {
//         FileSystemInfo info = this.Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);
//         long size = this.Library.Folders ? Helpers.FileHelper.GetDirectorySize(fullpath) : ((FileInfo)info).Length;
//
//         return MatchesDetection(Library, info, size);
//     }
//
//     /// <summary>
//     /// Tests if a file in a library matches the detection settings for hte library
//     /// </summary>
//     /// <param name="library">the library to test</param>
//     /// <param name="info">the info for the file or folder to test</param>
//     /// <param name="size">the size of the file or folder in bytes</param>
//     /// <returns>true if matches detection, otherwise false</returns>
//     public static bool MatchesDetection(Library library, FileSystemInfo info, long size)
//     {
//         if(MatchesValue((int)DateTime.UtcNow.Subtract(info.CreationTimeUtc).TotalMinutes, library.DetectFileCreation, library.DetectFileCreationLower, library.DetectFileCreationUpper, info.CreationTimeUtc, library.DetectFileCreationDate) == false)
//             return false;
//
//         if(MatchesValue((int)DateTime.UtcNow.Subtract(info.LastWriteTimeUtc).TotalMinutes, library.DetectFileLastWritten, library.DetectFileLastWrittenLower, library.DetectFileLastWrittenUpper, info.LastWriteTimeUtc, library.DetectFileLastWrittenDate) == false)
//             return false;
//         
//         if(MatchesValue(size, library.DetectFileSize, library.DetectFileSizeLower, library.DetectFileSizeUpper, null, null) == false)
//             return false;
//         
//         return true;
//         
//     }
//     
//     private static bool MatchesValue(long value, MatchRange range, long low, long high, DateTime? dateValue, DateTime? dateTest)
//     {
//         if (range == MatchRange.Any)
//             return true;
//         if (range == MatchRange.After)
//             return dateValue > dateTest;
//         if (range == MatchRange.Before)
//             return dateValue < dateTest;
//         
//         if (range == MatchRange.GreaterThan)
//             return value > low;
//         if (range == MatchRange.LessThan)
//             return value < low;
//         bool between = value >= low && value <= high;
//         return range == MatchRange.Between ? between : !between;
//     }
//
//     private (bool known, string? fingerprint, ObjectReference? duplicate) IsKnownFile(string fullpath, FileSystemInfo fsInfo)
//     {
//         var service = ServiceLoader.Load<LibraryFileService>();
//         var knownFile = service.GetFileIfKnown(fullpath, Library.Uid).Result;
//         string? fingerprint = null;
//         if (knownFile != null)
//         {
//             if (Library.DownloadsDirectory && knownFile.Status == FileStatus.Processed)
//             {
//                 Logger.DLog("Processed file found in download library, reprocessing: " + fullpath);
//                 knownFile.Status = FileStatus.Unprocessed;
//                 knownFile.HoldUntil = Library.HoldMinutes > 0
//                     ? DateTime.UtcNow.AddMinutes(Library.HoldMinutes)
//                     : DateTime.MinValue;
//                 ServiceLoader.Load<LibraryFileService>().Update(knownFile).Wait();
//                 return (true, null, null);
//             }
//             // FF-393 - check to see if the file has been modified
//             var creationDiff = Math.Abs(fsInfo.CreationTimeUtc.Subtract(knownFile.CreationTime).TotalSeconds);
//             var writeDiff = Math.Abs(fsInfo.LastWriteTimeUtc.Subtract(knownFile.LastWriteTime).TotalSeconds);
//             bool needsReprocessing = false;
//             if (Library.UseFingerprinting && (creationDiff > 5 || writeDiff > 5))
//             {
//                 // file has been modified, recalculate the fingerprint to see if it needs to be reprocessed
//                 fingerprint = FileHelper.CalculateFingerprint(fullpath);
//                 if (
//                     string.IsNullOrEmpty(knownFile.FinalFingerprint) == false // only check this after FF-425 and we have the final fingerprint  
//                     && fingerprint?.EmptyAsNull() != knownFile.Fingerprint?.EmptyAsNull() // fingerprint doesnt match original
//                     && fingerprint?.EmptyAsNull() != knownFile.FinalFingerprint // fingerprint doesnt match the final fingerprint we have saved for it
//                     )
//                 {
//                     // so if we have the final fingerprint, and the current fingerprint doesnt match the 
//                     // 1. original = in case the final action copies or moves a newly created file to another directory,
//                     //    then the original file should remain with original fingerprint
//                     // 2. final fingerprint = if the user chose to replace original, the original file now should have 
//                     //    the final fingerprint as its fingerprint
//                     // so this file doesnt match either fingerprints, therefore, we must reprocess it.
//                     Logger.ILog(
//                         $"File '{fullpath}' has been modified since last was processed by FileFlows, marking for reprocessing");
//                     needsReprocessing = true;
//                 }
//             }
//
//             if (needsReprocessing == false)
//             {
//                 if (Library.ReprocessRecreatedFiles == false || creationDiff < 5)
//                 {
//                     LogQueueMessage($"{Library.Name} skipping known file '{fullpath}'");
//                     // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
//                     return (true, null, null);
//                 }
//
//                 Logger.DLog(
//                     $"{Library.Name} file '{fullpath}' creation time has changed, reprocessing file '{fsInfo.CreationTimeUtc}' vs '{knownFile.CreationTime}'");
//             }
//
//             knownFile.CreationTime = fsInfo.CreationTimeUtc;
//             knownFile.LastWriteTime = fsInfo.LastWriteTimeUtc;
//             knownFile.Status = FileStatus.Unprocessed;
//             knownFile.Fingerprint = fingerprint?.EmptyAsNull() ?? FileHelper.CalculateFingerprint(fullpath);
//             //new LibraryFileController().Update(knownFile).Wait();
//             service.Update(knownFile).Wait();
//             // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
//             return (true, null, null);
//         }
//
//         if (Library.UseFingerprinting && Library.Folders == false)
//         {
//             fingerprint = FileHelper.CalculateFingerprint(fullpath);
//             if (string.IsNullOrEmpty(fingerprint) == false)
//             {
//                 knownFile = service.GetFileByFingerprint(Library.Uid, fingerprint).Result;
//                 if (knownFile != null)
//                 {
//                     if (knownFile.Name != fullpath && Library.UpdateMovedFiles && knownFile.LibraryUid == Library.Uid)
//                     {
//                         // library is set to update moved files, so check if the original file still exists
//                         if (File.Exists(knownFile.Name) == false)
//                         {
//                             // original no longer exists, update the original to be this file
//                             knownFile.CreationTime = fsInfo.CreationTimeUtc;
//                             knownFile.LastWriteTime = fsInfo.LastWriteTimeUtc;
//                             if (knownFile.OutputPath == knownFile.Name)
//                                 knownFile.OutputPath = fullpath;
//                             knownFile.Name = fullpath;
//                             knownFile.RelativePath = GetRelativePath(fullpath);
//                             service.UpdateMovedFile(knownFile).Wait();
//                             // new LibraryFileController().Update(knownFile).Wait();
//                             // file has been updated, we return this is known and tell the scanner to just continue
//                             return (true, null, null);
//                         }
//                     }
//                     return (false, fingerprint, new ObjectReference()
//                     {
//                         Name = knownFile.Name,
//                         Type = typeof(LibraryFile).FullName,
//                         Uid = knownFile.Uid
//                     });
//                 }
//             }
//         }
//
//         return (false, fingerprint, null);
//     }
//
//     /// <summary>
//     /// Checks if a path exists
//     /// </summary>
//     /// <param name="fullpath">the full path</param>
//     /// <returns>true if exists, otherwise false</returns>
//     private bool CheckExists(string fullpath)
//     {
//         try
//         {
//             if (Library.Folders)
//                 return Directory.Exists(fullpath);
//             return File.Exists(fullpath);
//         }
//         catch (Exception)
//         {
//             return false;
//         }
//     }
//
//     /// <summary>
//     /// Checks if a file is hidden
//     /// </summary>
//     /// <param name="fullpath">the full path to the file</param>
//     /// <returns>true if it is hidden</returns>
//     private bool FileIsHidden(string fullpath)
//     {
//         try
//         {
//             FileAttributes attributes = File.GetAttributes(fullpath);
//             if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
//                 return true;
//         }
//         catch (Exception)
//         {
//             return false;
//         }
//
//         // recursively search the directories to see if its hidden
//         var dir = new FileInfo(fullpath).Directory;
//         int count = 0;
//         while(dir.Parent != null)
//         {
//             if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                 return true;
//             dir = dir.Parent;
//             if (++count > 20)
//                 break; // infinite recursion safety check
//         }
//         return false;
//     }
//     
//     /// <summary>
//     /// Disposes of the watched library
//     /// </summary>
//     public void Dispose()
//     {
//         Disposed = true;            
//         DisposeWatcher();
//         //worker.Dispose();
//         //QueueTimer?.Dispose();
//     }
//
//     /// <summary>
//     /// Sets up the file system watcher
//     /// </summary>
//     void SetupWatcher()
//     {
//         try
//         {
//             DisposeWatcher();
//
//             Watcher = new FileSystemWatcher(Library.Path);
//             Watcher.NotifyFilter =
//                              NotifyFilters.CreationTime |
//                              NotifyFilters.DirectoryName |
//                              NotifyFilters.FileName |
//                              NotifyFilters.LastWrite |
//                              NotifyFilters.Size;
//             Watcher.IncludeSubdirectories = true;
//             Watcher.Changed += Watcher_Changed;
//             Watcher.Created += Watcher_Changed;
//             Watcher.Renamed += Watcher_Changed;
//             Watcher.EnableRaisingEvents = true;
//         }
//         catch (Exception ex)
//         {
//             Logger.ELog($"Failed to create file system watcher for '{Library.Path}': {ex.Message}");
//             DisposeWatcher();
//             this.UseScanner = true;
//         }
//     }
//
//     /// <summary>
//     /// Disposes of the watcher
//     /// </summary>
//     void DisposeWatcher()
//     {
//         try
//         {
//             if (Watcher != null)
//             {
//                 Watcher.Changed -= Watcher_Changed;
//                 Watcher.Created -= Watcher_Changed;
//                 Watcher.Renamed -= Watcher_Changed;
//                 Watcher.EnableRaisingEvents = false;
//                 Watcher.Dispose();
//             }
//             Watcher = null;
//         }
//         catch (Exception)
//         {
//         }
//     }
//
//     /// <summary>
//     /// Tests a path to see if its allowed based on the filters and extensions
//     /// </summary>
//     /// <param name="input">the input path</param>
//     /// <returns>true if filters/extensions allow this path</returns>
//     private bool IsMatch(string input)
//     {
//         if (Library.ExclusionFilters?.Any() == true)
//         {
//             foreach (var filter in Library.ExclusionFilters)
//             {
//                 if (string.IsNullOrWhiteSpace(filter))
//                     continue;
//                 if (_StringHelper.Matches(filter, input))
//                 {
//                     Logger.ILog($"Exclusion Filter Match [{filter}]: {input}");
//                     return false;
//                 }
//             }
//         }
//
//         if (Library.Filters?.Any() == true)
//         {
//             foreach (var filter in Library.Filters)
//             {
//                 if (string.IsNullOrWhiteSpace(filter))
//                     continue;
//                 if (_StringHelper.Matches(filter, input))
//                 {
//                     Logger.ILog($"Inclusion Filter Match [{filter}]: {input}");
//                     return true;
//                 }
//             }
//             return false;
//         }
//
//         if (Library.Extensions?.Any() != true)
//         {
//             // default to true
//             return true;
//         }
//
//         foreach (var extension in Library.Extensions)
//         {
//             var ext = extension.ToLowerInvariant();
//             if (string.IsNullOrWhiteSpace(ext))
//                 continue;
//             if (ext.StartsWith('.') == false)
//                 ext = "." + ext;
//             if (input.ToLowerInvariant().EndsWith(ext))
//                 return true;
//         }
//
//         // didnt match extensions
//         return false;
//     }
//
//     private void Watcher_Changed(object sender, FileSystemEventArgs e)
//     {
//         try
//         {
//             if (Library.Folders == false && Directory.Exists(e.FullPath))
//             {
//                 foreach (var file in Directory.GetFiles(e.FullPath, "*.*", Library.TopLevelOnly? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
//                 {
//                     FileChangeEvent(file);
//                 }
//             }
//             else
//             {
//                 var file = new FileInfo(e.FullPath);
//                 if (file.Exists == false)
//                     return;
//                                     
//                 long size = file.Length;
//                 Thread.Sleep(20_000);
//                 if (size < file.Length)
//                     return; // if the file is being copied, we need to wait for that to finish, which will fire a new event
//
//                 FileChangeEvent(e.FullPath);
//             }
//         }
//         catch (Exception ex)
//         {
//             if (ex.Message?.StartsWith("Could not find a part of the path") == true)
//                 return; // can happen if file is being moved quickly
//             Logger.ELog("Watched Exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
//         }
//     }
//
//     private void FileChangeEvent(string fullPath)
//     { 
//         if (IsMatch(fullPath) == false)
//         {
//             if (fullPath.Contains("_UNPACK_"))
//                 return; // dont log this, too many
//             return;
//         }
//
//         if (QueueContains(fullPath) == false)
//         {
//             LogQueueMessage($"{Library.Name} queueing file: {fullPath}");
//             QueueItem(fullPath);
//         }
//     }
//
//     internal void UpdateLibrary(Library library)
//     {
//         this.Library = library;
//         if (Directory.Exists(library.Path) == false)
//         {
//             UseScanner = true;
//         }
//         else if (UseScanner && library.Scan == false)
//         {
//             Logger.ILog($"Library '{library.Name}' switched to watched mode, starting watcher");
//             UseScanner = true;
//             SetupWatcher();
//         }
//         else if(UseScanner == false && library.Scan == true)
//         {
//             Logger.ILog($"Library '{library.Name}' switched to scan mode, disposing watcher");
//             UseScanner = false;
//             DisposeWatcher();
//         }
//         else if(UseScanner == false && Watcher != null && Watcher.Path != library.Path)
//         {
//             // library path changed, need to change watcher
//             Logger.ILog($"Library '{library.Name}' path changed, updating watched path");
//             SetupWatcher(); 
//         }
//
//         if (library.Enabled && library.LastScanned < new DateTime(2020, 1, 1) && Directory.Exists(library.Path))
//         {
//             ScanComplete = false; // this could happen if they click "Rescan" on the library page, this will force a full new scan
//             Logger.ILog($"Library '{library.Name}' marked for re-scan");
//         }
//     }
//
//     public void Scan()
//     {
//         if (ScanMutex.WaitOne(1) == false)
//             return;
//         DateTime start = DateTime.UtcNow;
//         try
//         {
//             if (Library.ScanInterval < 10)
//                 Library.ScanInterval = 60;
//
//             if (Library.Enabled == false)
//                 return;
//
//             if (TimeHelper.InSchedule(Library.Schedule) == false)
//             {
//                 Logger.ILog($"Library '{Library.Name}' outside of schedule, scanning skipped.");
//                 return;
//             }
//
//             if (string.IsNullOrEmpty(Library.Path) || Directory.Exists(Library.Path) == false)
//             {
//                 Logger.WLog($"Library '{Library.Name}' path not found: {Library.Path}");
//                 return;
//             }
//             
//             Logger.ILog($"Scan started on '{Library.Name}': {Library.Path}");
//             
//             int count = 0;
//             if (Library.Folders)
//             {
//                 var dirs = new DirectoryInfo(Library.Path).GetDirectories();
//                 foreach (var dir in dirs)
//                 {
//                     if (QueueContains(dir.FullName) == false)
//                     {
//                         QueueItem(dir.FullName);
//                         ++count;
//                     }
//                 }
//             }
//             else
//             {
//                 var service = new LibraryFileService();
//                 var knownFiles = service.GetKnownLibraryFilesWithCreationTimes(Library.Uid).Result;
//
//                 var files = GetFiles(new DirectoryInfo(Library.Path));
//                 var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
//                 foreach (var file in files)
//                 {
//                     if (IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
//                         continue;
//
//                     if (MatchesDetection(file.FullName) == false)
//                         continue;
//
//                     if (knownFiles.TryGetValue(file.FullName.ToLowerInvariant(), out KnownFileInfo? info) && info != null)
//                     {
//                         if (Library.DownloadsDirectory && info.Status == FileStatus.Processed)
//                         {
//                             
//                         }
//                         else if (DatesAreSame(file, info))
//                             continue;
//                     }
//
//
//                     if (QueueContains(file.FullName) == false)
//                     {
//                         LogQueueMessage($"{Library.Name} queueing file for scan: {file.FullName}", settings);
//                         QueueItem(file.FullName);
//                         ++count;
//                     }
//                 }
//             }
//
//             Logger.ILog($"Files queued for '{Library.Name}': {count} / {QueueCount()}");
//             ScanComplete = true;
//             
//             Library.LastScanned = DateTime.UtcNow;
//             ServiceLoader.Load<LibraryService>().UpdateLastScanned(Library.Uid).Wait();
//         }
//         catch(Exception ex)
//         {
//             while(ex.InnerException != null)
//                 ex = ex.InnerException;
//
//             Logger.ELog("Failed scanning for files: " + ex.Message + Environment.NewLine + ex.StackTrace);
//
//             _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
//                 $"'{Library.Name}' failed scanning for files",
//                 ex.Message);
//         }
//         finally
//         {
//             Logger.ILog($"Scan finished on '{Library.Name}': {Library.Path} ({DateTime.UtcNow.Subtract(start)})");
//             ScanMutex.ReleaseMutex();
//         }
//     }
//
//     /// <summary>
//     /// Checks if the dates on a file are the same as the known file
//     /// </summary>
//     /// <param name="file">the file to check</param>
//     /// <param name="knownFile">the known file</param>
//     /// <returns>true if the dates are the same</returns>
//     private bool DatesAreSame(FileInfo file, KnownFileInfo knownFile)
//     {
//         if (datesSame(
//                 file.CreationTime, knownFile.CreationTime,
//                 file.LastWriteTime, knownFile.LastWriteTime
//             ))
//             return true;
//         
//         if (datesSame(
//                 file.CreationTimeUtc, knownFile.CreationTime,
//                 file.LastWriteTimeUtc, knownFile.LastWriteTime
//             ))
//             return true;
//         
//         return false;
//
//         bool datesSame(DateTime create1, DateTime create2, DateTime write1, DateTime write2)
//         {
//             var createDiff = (int) Math.Abs(create1.Subtract(create2).TotalSeconds);
//             var writeDiff = (int)Math.Abs(write1.Subtract(write2).TotalSeconds);
//             
//             bool create = createDiff < 5;
//             bool write = writeDiff < 5;
//             return create && write;
//         }
//     }
//
//     /// <summary>
//     /// Checks if a file can be accessed
//     /// </summary>
//     /// <param name="file">the file to check</param>
//     /// <param name="fileSizeDetectionInterval">the interval to check if the file size has changed in seconds</param>
//     /// <returns>true if the file can be accessed</returns>
//     private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
//     {
//         DateTime now = DateTime.UtcNow;
//         bool canRead = false, canWrite = false, checkedAccess = false;
//         try
//         {
//             if (file.LastWriteTimeUtc > DateTime.UtcNow.AddSeconds(-10))
//             {
//                 // check if the file size changes
//                 long fs = file.Length;
//                 if (fileSizeDetectionInterval > 0)
//                     await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);
//
//                 if (fs != file.Length)
//                 {
//                     Logger.ILog("File size has changed, skipping for now: " + file.FullName);
//                     return false; // file size has changed, could still be being written too
//                 }
//             }
//
//             checkedAccess = true;
//
//             await using (var fs = FileOpenHelper.OpenForCheckingReadWriteAccess(file.FullName))
//             {
//                 if(fs.CanRead == false)
//                 {
//                     Logger.WLog("Cannot read file: " + file.FullName);
//                     return false;
//                 }
//                 canRead = true;
//                 if (fs.CanWrite == false)
//                 {
//                     Logger.WLog("Cannot write file: " + file.FullName);
//                     return false;
//                 }
//
//                 canWrite = true;
//             }
//
//             return true;
//         }
//         catch (Exception)
//         {
//             if (checkedAccess)
//             {
//                 if (canRead == false)
//                     Logger.WLog("Cannot read file: " + file.FullName);
//                 if (canWrite == false)
//                     Logger.WLog("Cannot write file: " + file.FullName);
//             }
//
//             return false;
//         }
//         finally
//         {
//             LogQueueMessage($"Time taken \"{(DateTime.UtcNow.Subtract(now))}\" to test can access file: \"{file}\"");
//         }
//     }
//
//     /// <summary>
//     /// Gets the files in recursively for a given folder
//     /// </summary>
//     /// <param name="dir">the folder</param>
//     /// <returns>all the files in the folder</returns>
//     public IEnumerable<FileInfo> GetFiles(DirectoryInfo dir)
//     {
//         DirectoryInfo[] subDirs;
//         try
//         {
//             subDirs = dir.GetDirectories();
//         }
//         catch (Exception)
//         {
//             subDirs = new DirectoryInfo[] { };
//         }
//
//
//         if (Library.TopLevelOnly == false)
//         {
//             foreach (var subdir in subDirs)
//             {
//                 foreach (var file in GetFiles(subdir))
//                     yield return file;
//             }
//         }
//
//         FileInfo[] files;
//         try
//         {
//             files = dir.GetFiles();
//         }
//         catch (Exception)
//         {
//             files = new FileInfo[] { };
//         }
//
//         foreach (var file in files)
//             yield return file;
//     }
//
//
//     /// <summary>
//     /// Safely gets the number of queued items
//     /// </summary>
//     /// <returns>the number of queued items</returns>
//     private int QueueCount()
//     {
//         lock (QueuedFiles)
//         {
//             return QueuedFiles.Count();
//         }
//     }
//
//     /// <summary>
//     /// Safely checks if the queue has items
//     /// </summary>
//     /// <returns>if the queue has items</returns>
//     private bool QueuedHasItems()
//     {
//         lock (QueuedFiles)
//         {
//             return QueuedFiles.Any();
//         }   
//     }
//     
//     /// <summary>
//     /// Safely adds an item to the queue
//     /// </summary>
//     /// <param name="fullPath">the item to add</param>
//     public void QueueItem(string fullPath)
//     {
//         if (MatchesDetection(fullPath) == false)
//         {
//             Logger.DLog($"{Library.Name} file failed file detection: {fullPath}");
//             return;
//         }
//         
//         lock (QueuedFiles)
//         {
//             if(QueuedFiles.Contains(fullPath) == false)
//                 QueuedFiles.Enqueue(fullPath);
//         }
//
//         ProcessQueue();
//     }
//
//     /// <summary>
//     /// Safely checks if the queue contains an item
//     /// </summary>
//     /// <param name="item">the item to check</param>
//     /// <returns>true if the queue contains it</returns>
//     private bool QueueContains(string item)
//     {
//         lock (QueuedFiles)
//         {
//             return QueuedFiles.Contains(item);
//         }
//     }
// }