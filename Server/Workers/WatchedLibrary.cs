using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.Extensions.FileProviders;

namespace FileFlows.Server.Workers;

/// <summary>
/// A watched library is a folder that imports files into FileFlows
/// </summary>
public class WatchedLibrary:IDisposable
{
    private FileSystemWatcher Watcher;
    public Library Library { get;private set; } 

    private bool ScanComplete = false;
    private bool UseScanner = false;
    private bool Disposed = false;

    private Mutex ScanMutex = new Mutex();

    private Queue<string> QueuedFiles = new Queue<string>();

    //private BackgroundWorker worker;
    private System.Timers.Timer QueueTimer;

    /// <summary>
    /// Constructs a instance of a Watched Library
    /// </summary>
    /// <param name="library">The library to watch</param>
    public WatchedLibrary(Library library)
    {
        this.Library = library;
        this.UseScanner = library.Scan;
        if(UseScanner == false)
            SetupWatcher();

        // worker = new BackgroundWorker();
        // worker.DoWork += Worker_DoWork;
        // worker.RunWorkerAsync();
        QueueTimer = new();
        QueueTimer.Elapsed += QueueTimerOnElapsed;
        QueueTimer.AutoReset = false;
        QueueTimer.Interval = 1;
        QueueTimer.Start();
    }


    private void LogQueueMessage(string message, Settings settings = null)
    {
        if (settings == null)
            settings = new SettingsController().Get().Result;

        if (settings?.LogQueueMessages != true)
            return;
        
        Logger.Instance.DLog(message);
    }

    private void QueueTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            Logger.Instance.ILog("Processing Timer Queue: " + this.Library.Name);
            ProcessQueuedItem();
        }
        catch (Exception)
        {
        }
        finally
        {
            if (Disposed == false && QueuedFiles?.Any() == true)
            {
                QueueTimer.Start();
            }
        }
    }

    private void Worker_DoWork(object? sender, DoWorkEventArgs e)
    {
        while (Disposed == false)
        {
            ProcessQueuedItem();
            if(QueuedFiles?.Any() != true)
            {
                LogQueueMessage($"{Library.Name} nothing queued");
                Thread.Sleep(1000);
            }
        }
    }

    private void ProcessQueuedItem()
    {
        try
        {
            string? fullpath;
            if (QueuedFiles.TryDequeue(out fullpath) == false)
                return;

            LogQueueMessage($"{Library.Name} Dequeued: {fullpath}");

            if (CheckExists(fullpath) == false)
            {
                Logger.Instance.DLog($"{Library.Name} file does not exist: {fullpath}");
                return;
            }


            if (this.Library.ExcludeHidden)
            {
                if (FileIsHidden(fullpath))
                {
                    LogQueueMessage($"{Library.Name} file is hidden: {fullpath}");
                    return;
                }
            }

            if (IsMatch(fullpath) == false || fullpath.EndsWith("_"))
            {
                LogQueueMessage($"{Library.Name} file does not match pattern or ends with _: {fullpath}");
                return;
            }

            if (fullpath.ToLower().StartsWith(Library.Path.ToLower()) == false)
            {
                Logger.Instance?.ILog($"Library file \"{fullpath}\" no longer belongs to library \"{Library.Path}\"");
                return; // library was changed
            }

            StringBuilder scanLog = new StringBuilder();
            DateTime dtTotal = DateTime.Now;

            FileSystemInfo fsInfo = Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);

            var (knownFile, fingerprint, duplicate) = IsKnownFile(fullpath, fsInfo).Result;
            if (knownFile && duplicate == null)
                return;

            string type = Library.Folders ? "folder" : "file";

            if (Library.Folders && Library.WaitTimeSeconds > 0)
            {
                DirectoryInfo di = (DirectoryInfo)fsInfo;
                try
                {
                    var files = di.GetFiles("*.*", SearchOption.AllDirectories);
                    if (files.Any())
                    {
                        var lastWriteTime = files.Select(x => x.LastWriteTime).Max();
                        if (lastWriteTime > DateTime.Now.AddSeconds(-Library.WaitTimeSeconds))
                        {
                            Logger.Instance.ILog(
                                $"Changes recently written to folder '{di.FullName}' cannot add to library yet");
                            Thread.Sleep(2000);
                            QueueItem(fullpath);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.ILog(
                        $"Error reading folder '{di.FullName}' cannot add to library yet, will try again: " +
                        ex.Message);
                    Thread.Sleep(2000);
                    QueueItem(fullpath);
                    return;
                }
            }

            Logger.Instance.DLog($"New unknown {type}: {fullpath}");

            if (Library.Folders == false &&
                CanAccess((FileInfo)fsInfo, Library.FileSizeDetectionInterval).Result == false)
            {
                Logger.Instance.DLog($"Cannot access file: " + fullpath);
                return;
            }

            int skip = Library.Path.Length;
            if (Library.Path.EndsWith("/") == false && Library.Path.EndsWith("\\") == false)
                ++skip;

            long size = Library.Folders ? 0 : ((FileInfo)fsInfo).Length;

            string relative = fullpath.Substring(skip);
            var lf = new LibraryFile
            {
                Name = fullpath,
                RelativePath = relative,
                Status = duplicate != null ? FileStatus.Duplicate : FileStatus.Unprocessed,
                IsDirectory = fsInfo is DirectoryInfo,
                Fingerprint = fingerprint ?? string.Empty,
                OriginalSize = size,
                CreationTime = fsInfo.CreationTime,
                LastWriteTime = fsInfo.LastWriteTime,
                Duplicate = duplicate,
                Library = new ObjectReference
                {
                    Name = Library.Name,
                    Uid = Library.Uid,
                    Type = Library.GetType()?.FullName ?? string.Empty
                },
                Order = -1
            };

            var result = new LibraryFileController().Add(lf).Result;

            if (result != null && result.Uid != Guid.Empty)
            {
                Logger.Instance.DLog(
                    $"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to successfully add new library file: \"{fullpath}\"");
            }
            else
            {
                Logger.Instance.ELog(
                    $"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to fail to add new library file: \"{fullpath}\"");
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error in queue: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private async Task<(bool known, string? fingerprint, ObjectReference? duplicate)> IsKnownFile(string fullpath, FileSystemInfo fsInfo)
    {
        if (DbHelper.UseMemoryCache)
            return IsKnownFileInMemory(fullpath, fsInfo);
        return await IsKnownFileInDb(fullpath, fsInfo);
    }
    
    private (bool known, string? fingerprint, ObjectReference? duplicate) IsKnownFileInMemory(string fullpath, FileSystemInfo fsInfo)
    {
        var libFiles = new LibraryFileController().GetData().Result;
        var knownFiles = libFiles.DistinctBy(x => x.Value.Name.ToLower()).ToDictionary(x => x.Value.Name.ToLower(), x => x.Key);
        var knownOutputFiles = libFiles.Where(x => string.IsNullOrEmpty(x.Value.OutputPath) == false).DistinctBy(x => x.Value.OutputPath.ToLower()).ToDictionary(x => x.Value.OutputPath.ToLower(), x => x.Key);
        var knownFingerprints = libFiles.Where(x => string.IsNullOrEmpty(x.Value.Fingerprint) == false)
                                    .DistinctBy(x => x.Value.Fingerprint)
                                    .ToDictionary(x => x.Value.Fingerprint.ToLower(), x => new ObjectReference { Name = x.Value.Name, Uid = x.Key, Type = x.Value.GetType().FullName });

        
        if (knownFiles.ContainsKey(fullpath.ToLower()))
        {
            var knownFileUid = knownFiles[fullpath.ToLower()];
            var knownFile = libFiles[knownFileUid];
            if(Library.ReprocessRecreatedFiles == false || fsInfo.CreationTime <= knownFile.CreationTime)
            {
                LogQueueMessage($"{Library.Name} skipping known file '{fullpath}'");
                // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
                return (true, null, null);
            }
            Logger.Instance.DLog($"{Library.Name} file '{fullpath}' creation time has changed, reprocessing file");
            knownFile.CreationTime = fsInfo.CreationTime;
            knownFile.LastWriteTime = fsInfo.LastWriteTime;
            knownFile.Status = FileStatus.Unprocessed;
            knownFile.Fingerprint = GetFingerprint().Fingerprint;
            new LibraryFileController().Update(knownFile).Wait();
            // we dont return the duplicate here, or the hash since this could trigger a insertion, its already in the db, so we want to skip it
            return (true, null, null);
        }
        if (knownOutputFiles.ContainsKey(fullpath.ToLower()))
        {
            LogQueueMessage($"{Library.Name} skipping known output file '{fullpath}'");
            return (true, null, null);
        }

        var fingerprint = GetFingerprint();
        return (false, fingerprint.Fingerprint, fingerprint.Duplicate);

        (string Fingerprint, ObjectReference? Duplicate) GetFingerprint()
        {
            string fingerprint = string.Empty;
            ObjectReference? duplicate = null;
            if (Library.UseFingerprinting)
            {
                fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(fullpath);
                if (string.IsNullOrEmpty(fingerprint) == false)
                {   
                    if (knownFingerprints.ContainsKey(fingerprint) && knownFingerprints[fingerprint]?.Name?.ToLower() != fullpath.ToLower())
                    {
                        duplicate = knownFingerprints[fingerprint];
                    }
                }
            }

            return (fingerprint, duplicate);

        }
    }

    private async Task<(bool known, string fingerprint, ObjectReference? duplicate)> IsKnownFileInDb(string fullPath, FileSystemInfo fsInfo)
    {
        var existing = await DbHelper.FindKnownLibraryFile(fullPath);
        if (string.IsNullOrEmpty(existing?.Name))
        {
            if(Library.UseFingerprinting == false)
                return (false, String.Empty, null);

            var fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(fullPath);

            existing = await DbHelper.FindKnownLibraryByFingerprint(fingerprint);
            if (string.IsNullOrEmpty(existing?.Name))
                return (false, fingerprint, null);

            if (existing.Name.ToLower() != fullPath.ToLower())
            {
                // duplicate
                return (true, fingerprint,
                    new ObjectReference { Name = existing.Name, Uid = existing.Uid, Type = typeof(LibraryFile).FullName });
            }
        }


        if (Library.ReprocessRecreatedFiles && fsInfo.CreationTime > existing.CreationTime)
        {
            Logger.Instance.DLog($"{Library.Name} file '{fullPath}' creation time has changed, reprocessing file");
            existing.Fingerprint = Library.UseFingerprinting ? ServerShared.Helpers.FileHelper.CalculateFingerprint(fullPath) : string.Empty;
            existing.CreationTime = fsInfo.CreationTime;
            existing.LastWriteTime = fsInfo.LastWriteTime;
            existing.Status = FileStatus.Unprocessed;
            await DbHelper.Update(existing);
            return (true, existing.Fingerprint, null);
        }
        
        // known, and not a duplicate
        return (true, existing.Fingerprint, null);
    }
    
    private bool CheckExists(string fullpath)
    {
        try
        {
            if (Library.Folders)
                return Directory.Exists(fullpath);
            return File.Exists(fullpath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool FileIsHidden(string fullpath)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(fullpath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;
        }
        catch (Exception)
        {
            return false;
        }

        // recursively search the directories to see if its hidden
        var dir = new FileInfo(fullpath).Directory;
        int count = 0;
        while(dir.Parent != null)
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden))
                return true;
            dir = dir.Parent;
            if (++count > 20)
                break; // infinite recrusion safety check
        }
        return false;
    }

    public void Dispose()
    {
        Disposed = true;            
        DisposeWatcher();
        //worker.Dispose();
        QueueTimer?.Dispose();
    }

    void SetupWatcher()
    {
        DisposeWatcher();

        Watcher = new FileSystemWatcher(Library.Path);
        Watcher.NotifyFilter =
                         //NotifyFilters.Attributes |
                         NotifyFilters.CreationTime |
                         NotifyFilters.DirectoryName |
                         NotifyFilters.FileName |
                         // NotifyFilters.LastAccess |
                         NotifyFilters.LastWrite |
                         //| NotifyFilters.Security
                         NotifyFilters.Size;
        Watcher.IncludeSubdirectories = true;
        Watcher.Changed += Watcher_Changed;
        Watcher.Created += Watcher_Changed;
        Watcher.Renamed += Watcher_Changed;
        Watcher.EnableRaisingEvents = true;

    }

    void DisposeWatcher()
    {
        if (Watcher != null)
        {
            Watcher.Changed -= Watcher_Changed;
            Watcher.Created -= Watcher_Changed;
            Watcher.Renamed -= Watcher_Changed;
            Watcher.EnableRaisingEvents = false;
            Watcher.Dispose();
            Watcher = null;
        }
    }

    private bool IsMatch(string input)
    {
        if (string.IsNullOrWhiteSpace(Library.ExclusionFilter) == false)
        {
            try
            {
                if (Regex.IsMatch(input, Library.ExclusionFilter, RegexOptions.IgnoreCase))
                    return false;
            }
            catch (Exception) { }
        }
        
        if (string.IsNullOrWhiteSpace(Library.Filter) == false)
        {
            try
            {
                return Regex.IsMatch(input, Library.Filter, RegexOptions.IgnoreCase);
            }
            catch (Exception) { }
        }
        // default to true
        return true;
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (Library.Folders == false && Directory.Exists(e.FullPath))
            {
                foreach (var file in Directory.GetFiles(e.FullPath, "*.*", SearchOption.AllDirectories))
                {
                    FileChangeEvent(file);
                }
            }
            else
            {
                var file = new FileInfo(e.FullPath);
                if (file.Exists == false)
                    return;
                                    
                long size = file.Length;
                Thread.Sleep(20_000);
                if (size < file.Length)
                    return; // if the file is being copied, we need to wait for that to finish, which will fire a new event

                FileChangeEvent(e.FullPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("WatchedLibrary.Watched Exception: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private void FileChangeEvent(string fullPath)
    { 
        if (IsMatch(fullPath) == false)
        {
            if (fullPath.Contains("_UNPACK_"))
                return; // dont log this, too many
            return;
        }

        if (QueuedFiles.Contains(fullPath) == false)
        {
            LogQueueMessage($"{Library.Name} queueing file: {fullPath}");
            QueueItem(fullPath);
        }
    }

    internal void UpdateLibrary(Library library)
    {
        this.Library = library;

        if (UseScanner && library.Scan == false)
        {
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to watched mode, starting watcher");
            UseScanner = false;
            SetupWatcher();
        }
        else if(UseScanner == false && library.Scan == true)
        {
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' switched to scan mode, disposing watcher");
            UseScanner = true;
            DisposeWatcher();
        }
        else if(UseScanner == false && Watcher != null && Watcher.Path != library.Path)
        {
            // library path changed, need to change watcher
            Logger.Instance.ILog($"WatchedLibrary: Library '{library.Name}' path changed, updating watched path");
            SetupWatcher(); 
        }

        if (library.Enabled && library.LastScanned < new DateTime(2020, 1, 1))
        {
            ScanComplete = false; // this could happen if they click "Rescan" on the library page, this will force a full new scan
            Logger.Instance?.ILog($"WatchedLibrary: Library '{library.Name}' marked for full scan");
        }
    }

    public void Scan(bool fullScan = false)
    {
        if (ScanMutex.WaitOne(1) == false)
            return;
        try
        {
            if (Library.ScanInterval < 10)
                Library.ScanInterval = 60;

            if (Library.Enabled == false)
                return;

            if (TimeHelper.InSchedule(Library.Schedule) == false)
            {
                Logger.Instance?.ILog($"Library '{Library.Name}' outside of schedule, scanning skipped.");
                return;
            }

            if (fullScan == false)
                fullScan = Library.LastScanned < DateTime.Now.AddHours(-1); // do a full scan every hour just incase we missed something

            if (fullScan == false && Library.LastScanned > DateTime.Now.AddSeconds(-Library.ScanInterval))
            {
                if(Library.Scan) // only log this if set to scan mode
                    Logger.Instance?.ILog($"Library '{Library.Name}' need to wait until '{(Library.LastScanned.AddSeconds(Library.ScanInterval))}' before scanning again");
                return;
            }

            if (UseScanner == false && ScanComplete && fullScan == false)
            {
                Logger.Instance?.ILog($"Library '{Library.Name}' has full scan, using FileWatcherEvents now to watch for new files");
                return; // we can use the filesystem watchers for any more files
            }

            if (string.IsNullOrEmpty(Library.Path) || Directory.Exists(Library.Path) == false)
            {
                Logger.Instance?.WLog($"WatchedLibrary: Library '{Library.Name}' path not found: {Library.Path}");
                return;
            }

            Logger.Instance.DLog($"Scan started on '{Library.Name}': {Library.Path}");
            
            
            int count = 0;
            if (Library.Folders)
            {
                var dirs = new DirectoryInfo(Library.Path).GetDirectories();
                foreach (var dir in dirs)
                {
                    if (QueuedFiles.Contains(dir.FullName) == false)
                    {
                        QueueItem(dir.FullName);
                        ++count;
                    }
                }
            }
            else 
            {
                var libFiles = new LibraryFileController().GetData().Result;
                var knownFiles = libFiles.DistinctBy(x => x.Value.Name.ToLower()).ToDictionary(x => x.Value.Name.ToLower(), x => x.Key);
                var knownOutputFiles = libFiles.Where(x => string.IsNullOrEmpty(x.Value.OutputPath) == false).DistinctBy(x => x.Value.OutputPath.ToLower()).ToDictionary(x => x.Value.OutputPath.ToLower(), x => x.Key);

                var files = GetFiles(new DirectoryInfo(Library.Path));
                var settings = new SettingsController().Get().Result;
                foreach (var file in files)
                {
                    if (IsMatch(file.FullName) == false || file.FullName.EndsWith("_"))
                        continue;
                
                    if (knownFiles.ContainsKey(file.FullName.ToLower()))
                    {
                        var knownFileUid = knownFiles[file.FullName.ToLower()];
                        var knownFile = libFiles[knownFileUid];
                        if (Library.ReprocessRecreatedFiles == false ||
                            file.CreationTime <= knownFile.CreationTime)
                        {
                            continue; // known file that hasn't changed, skip it
                        }
                    }


                    if (QueuedFiles.Contains(file.FullName) == false)
                    {
                        LogQueueMessage($"{Library.Name} queueing file for scan: {file.FullName}", settings);
                        QueueItem(file.FullName);
                        ++count;
                    }
                }
            }

            LogQueueMessage($"Files queued for '{Library.Name}': {count} / {QueuedFiles.Count}");
            new LibraryController().UpdateLastScanned(Library.Uid).Wait();
        }
        catch(Exception ex)
        {
            while(ex.InnerException != null)
                ex = ex.InnerException;

            Logger.Instance.ELog("Failed scanning for files: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return;
        }
        finally
        {
            ScanMutex.ReleaseMutex();
        }
    }

    private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
    {
        DateTime now = DateTime.Now;
        try
        {
            if (file.LastWriteTime > DateTime.Now.AddSeconds(-10))
            {
                // check if the file size changes
                long fs = file.Length;
                if (fileSizeDetectionInterval > 0)
                    await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

                if (fs != file.Length)
                {
                    Logger.Instance.ILog("WatchedLibrary: File size has changed, skipping for now: " + file.FullName);
                    return false; // file size has changed, could still be being written too
                }
            }

            using (var fs = File.Open(file.FullName, FileMode.Open))
            {
                if(fs.CanRead == false)
                {
                    Logger.Instance.ILog("Cannot read file: " + file.FullName);
                    return false;
                }
                if (fs.CanWrite == false)
                {
                    Logger.Instance.ILog("Cannot write file: " + file.FullName);
                    return false;
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            LogQueueMessage($"Time taken \"{(DateTime.Now.Subtract(now))}\" to test can access file: \"{file}\"");
        }
    }

    public List<FileInfo> GetFiles(DirectoryInfo dir)
    {
        var files = new List<FileInfo>();
        try
        {
            foreach (var subdir in dir.GetDirectories())
            {
                files.AddRange(GetFiles(subdir));
            }
            files.AddRange(dir.GetFiles());
        }
        catch (Exception) { }
        return files;
    }

    private void QueueItem(string fullPath)
    {
        QueuedFiles.Enqueue(fullPath);
        QueueTimer.Start();
    }
}
