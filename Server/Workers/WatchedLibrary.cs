using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace FileFlows.Server.Workers
{
    public class WatchedLibrary:IDisposable
    {
        private FileSystemWatcher Watcher;
        public Library Library { get;private set; } 

        private Regex? Filter;

        private bool ScanComplete = false;
        private bool UseScanner = false;
        private bool Disposed = false;

        private Mutex ScanMutex = new Mutex();

        private Queue<string> QueuedFiles = new Queue<string>();

        private BackgroundWorker worker;

        public WatchedLibrary(Library library)
        {
            this.Library = library;
            this.UseScanner = library.Scan;
            if(string.IsNullOrEmpty(library.Filter) == false)
            {
                try
                {
                    Filter = new Regex(library.Filter, RegexOptions.IgnoreCase);
                }
                catch (Exception) { }
            }
            if(UseScanner == false)
                SetupWatcher();

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync();
        }
        
        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            while (Disposed == false)
            {
                try
                {
                    string? fullpath;
                    if (QueuedFiles.TryDequeue(out fullpath) == false)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (this.Library.ExcludeHidden)
                    {
                        if (FileIsHidden(fullpath))
                            continue;
                    }


                    if (IsMatch(fullpath) == false || fullpath.EndsWith("_"))
                        continue;

                    if (fullpath.ToLower().StartsWith(Library.Path.ToLower()) == false)
                    {
                        Logger.Instance?.ILog($"Library file \"{fullpath}\" no longer belongs to library \"{Library.Path}\"");
                        continue; // library was changed
                    }

                    StringBuilder scanLog = new StringBuilder(); 
                    DateTime dtTotal = DateTime.Now;

                    var libFiles = new LibraryFileController().GetData().Result;
                    var knownFiles = libFiles.DistinctBy(x => x.Value.Name.ToLower()).ToDictionary(x => x.Value.Name.ToLower(), x => x.Key);
                    var knownOutputFiles = libFiles.Where(x => string.IsNullOrEmpty(x.Value.OutputPath) == false).DistinctBy(x => x.Value.OutputPath.ToLower()).ToDictionary(x => x.Value.OutputPath.ToLower(), x => x.Key);
                    var knownFingerprints = libFiles.Where(x => string.IsNullOrEmpty(x.Value.Fingerprint) == false)
                                                .DistinctBy(x => x.Value.Fingerprint)
                                                .ToDictionary(x => x.Value.Fingerprint.ToLower(), x => new ObjectReference { Name = x.Value.Name, Uid = x.Key, Type = x.Value.GetType().FullName });
                    
                    if (knownFiles.ContainsKey(fullpath.ToLower()))
                        continue;
                    if (knownOutputFiles.ContainsKey(fullpath.ToLower()))
                        continue;

                    string type = Library.Folders ? "folder" : "file";

                    Logger.Instance.DLog($"New unknown {type}: {fullpath}");

                    FileSystemInfo fsInfo = Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);

                    if (Library.Folders == false && CanAccess((FileInfo)fsInfo, Library.FileSizeDetectionInterval).Result == false)
                    {
                        Logger.Instance.DLog($"Cannot access file: " + fullpath);
                        continue;
                    }

                    int skip = Library.Path.Length;
                    // check if the length is != 3 incase its jusst a directory, eg "Z:\"
                    // else if the path is windows and just "Z:" we will include the "\" to skip by increasing the skip count
                    // else its in a folder and we have to increase the skip by 1 to add the directory separator
                    if (Globals.IsWindows == false || Library.Path.Length != 3)
                        ++skip;

                    long size = Library.Folders ? 0 : ((FileInfo)fsInfo).Length;

                    string relative = fullpath.Substring(skip);
                    var lf = new LibraryFile
                    {
                        Name = fullpath,
                        RelativePath = relative,
                        Status = FileStatus.Unprocessed,
                        IsDirectory = fsInfo is DirectoryInfo,
                        Fingerprint = string.Empty,
                        OriginalSize = size,
                        Library = new ObjectReference
                        {
                            Name = Library.Name,
                            Uid = Library.Uid,
                            Type = Library.GetType()?.FullName ?? string.Empty
                        },
                        Order = -1
                    };


                    if (Library.UseFingerprinting)
                    {
                        string fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(fullpath);
                        if (string.IsNullOrEmpty(fingerprint) == false)
                        {
                            lf.Fingerprint = fingerprint;   
                            if (knownFingerprints.ContainsKey(fingerprint))
                            {
                                lf.Status = FileStatus.Duplicate;
                                lf.Duplicate = knownFingerprints[fingerprint];
                            }
                        }
                    }

                    var result = new LibraryFileController().Add(lf).Result;

                    if(result != null && result.Uid != Guid.Empty)
                    {
                        knownFiles.Add(fullpath.ToLower(), result.Uid);
                        if(string.IsNullOrEmpty(result.Fingerprint) == false && knownFingerprints.ContainsKey(result.Fingerprint) == false)
                        {
                            knownFingerprints.Add(result.Fingerprint, new ObjectReference
                            {
                                Name = result.Name,
                                Uid = result.Uid,
                                Type = result.GetType()?.FullName ?? string.Empty
                            });
                        }
                        Logger.Instance.DLog($"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to successfully add new library file: \"{fullpath}\"");
                    }
                    else
                    {
                        Logger.Instance.ELog($"Time taken \"{(DateTime.Now.Subtract(dtTotal))}\" to fail to add new library file: \"{fullpath}\"");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Error in queue: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        private bool FileIsHidden(string fullpath)
        {
            FileAttributes attributes = File.GetAttributes(fullpath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;

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
            worker.Dispose();
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
            if (Filter == null)
                return true;
            return Filter.IsMatch(input);
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

            if(QueuedFiles.Contains(fullPath) == false)
                QueuedFiles.Enqueue(fullPath);
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
            Logger.Instance?.ILog($"Library '{Library.Name}' scan");
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
                        if (QueuedFiles.Contains(dir.FullName) == false) {
                            QueuedFiles.Enqueue(dir.FullName);
                            ++count;
                        }
                    }
                }
                else 
                {
                    var files = GetFiles(new DirectoryInfo(Library.Path));
                    foreach (var file in files)
                    {
                        if (QueuedFiles.Contains(file.FullName) == false)
                        {
                            QueuedFiles.Enqueue(file.FullName);
                            ++count;
                        }
                    }
                }

                Logger.Instance.DLog($"Files queued for '{Library.Name}': {count} / {QueuedFiles.Count}");
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
                Logger.Instance.DLog($"Time taken \"{(DateTime.Now.Subtract(now))}\" to test can access file: \"{file}\"");
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
    }
}
