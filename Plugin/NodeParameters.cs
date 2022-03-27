namespace FileFlows.Plugin
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;

    public class NodeParameters
    {
        /// <summary>
        /// The original filename of the file
        /// </summary>
        public string FileName { get; init; }
        /// <summary>
        /// Gets or sets the file relative to the library path
        /// </summary>
        public string RelativeFile { get; set; }

        /// <summary>
        /// The current working file as it is being processed in the flow, 
        /// this is what a node should save any changes too, and if the node needs 
        /// to change the file path this should be updated too
        /// </summary>
        public string WorkingFile { get; private set; }

        public long WorkingFileSize { get; private set; }

        public ILogger? Logger { get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        public Func<string, string>? GetToolPathActual { get; set; }

        public Func<string, string> GetPluginSettingsJson { get; set; }
        public Func<string, string>? PathMapper { get; set; }

        public Action<ObjectReference> GotoFlow { get; set; }

        public bool IsDirectory { get; set; }
        public string LibraryPath { get; set; }

        public string TempPath { get; set; }

        public Action<float>? PartPercentageUpdate { get; set; }

        public ProcessHelper Process { get; set; }

        private bool Fake = false;

        public NodeParameters(string filename, ILogger logger, bool isDirectory, string libraryPath)
        {
            Fake = string.IsNullOrEmpty(filename);
            this.IsDirectory = isDirectory;
            this.FileName = filename;
            this.LibraryPath = libraryPath;
            this.WorkingFile = filename;
            if (Fake == false)
            {
                try
                {
                    this.WorkingFileSize = IsDirectory ? GetDirectorySize(filename) : new FileInfo(filename).Length;
                }
                catch (Exception) { } // can fail in unit tests
            }
            this.RelativeFile = string.Empty;
            this.TempPath = string.Empty;
            this.Logger = logger;
            InitFile(filename);
            this.Process = new ProcessHelper(logger, this.Fake);
        }


        public long GetDirectorySize(string path)
        {
            if (Fake) return 100_000_000_000;
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public string MapPath(string path)
        {
            if (Fake) return path;
            if (PathMapper == null)
                return path;
            return PathMapper(path);
        }


        private bool initDone = false;
        private void InitFile(string filename)
        {
            if (Fake) return;
            try
            {
                if (IsDirectory)
                {
                    var di = new DirectoryInfo(filename);
                    UpdateVariables(new Dictionary<string, object> {
                        { "folder.Name", di.Name ?? "" },
                        { "folder.FullName", di.FullName ?? "" }
                    });
                    if(initDone == false)
                    {
                        initDone = true;
                        var diOriginal = new DirectoryInfo(this.FileName);
                        UpdateVariables(new Dictionary<string, object> {
                            { "folder.Date", diOriginal.CreationTime },
                            { "folder.Date.Year", diOriginal.CreationTime.Year },
                            { "folder.Date.Month", diOriginal.CreationTime.Month },
                            { "folder.Date.Day", diOriginal.CreationTime.Day},

                            { "folder.Orig.Name", diOriginal.Name ?? "" },
                            { "folder.Orig.FullName", diOriginal.FullName ?? "" },
                        });

                    }
                }
                else
                {
                    var fi = new FileInfo(filename);
                    UpdateVariables(new Dictionary<string, object> {
                        { "ext", fi.Extension ?? "" },
                        { "file.Name", Path.GetFileNameWithoutExtension(fi.Name ?? "") },
                        { "file.FullName", fi.FullName ?? "" },
                        { "file.Extension", fi.Extension ?? "" },
                        { "file.Size", fi.Exists ? fi.Length : 0 },

                        { "folder.Name", fi.Directory?.Name ?? "" },
                        { "folder.FullName", fi.DirectoryName ?? "" },
                    });

                    if(initDone == false)
                    {
                        initDone = true;
                        var fiOriginal = new FileInfo(this.FileName);
                        UpdateVariables(new Dictionary<string, object> {
                            { "file.Create", fiOriginal.CreationTime },
                            { "file.Create.Year", fiOriginal.CreationTime.Year },
                            { "file.Create.Month", fiOriginal.CreationTime.Month },
                            { "file.Create.Day", fiOriginal.CreationTime.Day },

                            { "file.Modified", fiOriginal.LastWriteTime },
                            { "file.Modified.Year", fiOriginal.LastWriteTime.Year },
                            { "file.Modified.Month", fiOriginal.LastWriteTime.Month },
                            { "file.Modified.Day", fiOriginal.LastWriteTime.Day },

                            { "file.Orig.Extension", fiOriginal.Extension ?? "" },
                            { "file.Orig.FileName", Path.GetFileNameWithoutExtension(fiOriginal.Name ?? "") },
                            { "file.Orig.FullName", fiOriginal.FullName ?? "" },
                            { "file.Orig.Size", fiOriginal.Exists? fiOriginal.Length: 0 },

                            { "folder.Orig.Name", fiOriginal.Directory?.Name ?? "" },
                            { "folder.Orig.FullName", fiOriginal.DirectoryName ?? "" }
                        });
                    }
                }
            }
            catch (Exception) { }

        }

        public void ResetWorkingFile()
        {
            if (Fake) return;
            SetWorkingFile(this.FileName);
        }

        public bool FileExists(string filename)
        {
            try
            {
                return File.Exists(filename);
            }
            catch (Exception) { return false; } 
        }

        public void SetWorkingFile(string filename, bool dontDelete = false)
        {
            if (Fake) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            {
                if (filename?.ToLower().StartsWith(TempPath.ToLower()) == true)
                {
                    Logger.ILog("Changing owner on working file: " + filename);
                    Helpers.FileHelper.ChangeOwner(Logger, filename, file: true);
                }
                else
                {
                    Logger.ILog("NOT changing owner on working file: " + filename + ", temp path: " + TempPath);
                }
            }

            if (this.WorkingFile == filename)
            {
                Logger?.ILog("Working file same as new filename: " + filename);
                return;
            }
            if (this.WorkingFile != this.FileName)
            {
                this.WorkingFileSize = new FileInfo(filename).Length;
                Logger?.ILog("New working file size: " + this.WorkingFileSize);
                string fileToDelete = this.WorkingFile;
                if (dontDelete == false)
                {
                    // delete the old working file
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2_000); // wait 2 seconds for the file to be released if used
                        try
                        {
                            File.Delete(fileToDelete);
                        }
                        catch (Exception ex)
                        {
                            Logger?.WLog("Failed to delete temporary file: " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    });
                }
            }
            this.WorkingFile = filename;
            InitFile(filename);
        }

        public T GetParameter<T>(string name)
        {
            if (Parameters.ContainsKey(name) == false)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)string.Empty;
                return default(T)!;
            }
            return (T)Parameters[name];
        }

        public void SetParameter(string name, object value)
        {
            if (Parameters.ContainsKey(name) == false)
                Parameters[name] = value;
            else
                Parameters.Add(name, value);
        }

        public bool MoveFile(string destination)
        {
            if (Fake) return true;

            FileInfo file = new FileInfo(destination);
            if (string.IsNullOrEmpty(file.Extension) == false)
            {
                // just ensures extensions are lowercased
                destination = new FileInfo(file.FullName.Substring(0, file.FullName.LastIndexOf(file.Extension)) + file.Extension.ToLower()).FullName;
            }

            Logger?.ILog("About to move file to: " + destination);
            destination = MapPath(destination);

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                if (destination.ToLower() == WorkingFile?.ToLower())
                {
                    Logger?.ILog("Source and destination are the same, skipping move");
                    return true;
                }
            }
            else
            {
                // linux, is case sensitive
                if(destination == WorkingFile)
                {
                    Logger?.ILog("Source and destination are the same, skipping move");
                    return true;
                }
            }


            bool moved = false;
            long fileSize = new FileInfo(WorkingFile).Length;
            Task task = Task.Run(() =>
            {
                try
                {

                    var fileInfo = new FileInfo(destination);
                    if (fileInfo.Exists)
                        fileInfo.Delete();
                    else
                        CreateDirectoryIfNotExists(fileInfo?.DirectoryName);

                    bool isTempFile = this.WorkingFile.ToLower().StartsWith(this.TempPath.ToLower()) == true;

                    Logger?.ILog($"Moving file: \"{WorkingFile}\" to \"{destination}\"");                    
                    File.Move(WorkingFile, destination, true);
                    Logger?.ILog("File moved successfully");

                    if (isWindows == false && isTempFile)
                        Helpers.FileHelper.ChangeOwner(Logger, destination, file: true);

                    this.WorkingFile = destination;
                    try
                    {
                        // this can fail if the file is then moved really quickly by another process, radarr/sonarr etc
                        Logger?.ILog("Initing new moved file");
                        InitFile(destination);
                    }
                    catch (Exception) { }

                    moved = true;
                }
                catch (Exception ex)
                {
                    Logger?.ELog("Failed to move file: " + ex.Message);
                }
            });

            while (task.IsCompleted == false)
            {
                long currentSize = 0;
                var destFileInfo = new FileInfo(destination);
                if (destFileInfo.Exists)
                    currentSize = destFileInfo.Length;

                if (PartPercentageUpdate != null)
                    PartPercentageUpdate(currentSize / fileSize * 100);
                Thread.Sleep(50);
            }

            if (moved == false)
                return false;

            if (PartPercentageUpdate != null)
                PartPercentageUpdate(100);
            return true;
        }


        public bool CopyFile(string destination)
        {
            if (Fake) return true;

            FileInfo file = new FileInfo(destination);
            if (string.IsNullOrEmpty(file.Extension) == false)
            {
                // just ensures extensions are lowercased
                destination = new FileInfo(file.FullName.Substring(0, file.FullName.LastIndexOf(file.Extension)) + file.Extension.ToLower()).FullName;
            }

            destination = MapPath(destination);

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                if (destination.ToLower() == WorkingFile?.ToLower())
                {
                    Logger?.ILog("Source and destination are the same, skipping move");
                    return true;
                }
            }
            else
            {
                // linux, is case sensitive
                if (destination == WorkingFile)
                {
                    Logger?.ILog("Source and destination are the same, skipping move");
                    return true;
                }
            }


            bool copied = false;
            long fileSize = new FileInfo(WorkingFile).Length;
            Task task = Task.Run(() =>
            {
                try
                {

                    var fileInfo = new FileInfo(destination);
                    if (fileInfo.Exists)
                        fileInfo.Delete();
                    else
                        CreateDirectoryIfNotExists(fileInfo?.DirectoryName);

                    bool isTempFile = this.WorkingFile.ToLower().StartsWith(this.TempPath.ToLower()) == true;

                    Logger?.ILog($"Copying file: \"{WorkingFile}\" to \"{destination}\"");
                    File.Copy(WorkingFile, destination, true);
                    Logger?.ILog("File copied successfully");

                    if (isWindows == false && isTempFile)
                        Helpers.FileHelper.ChangeOwner(Logger, destination, file: true);

                    this.WorkingFile = destination;
                    try
                    {
                        // this can fail if the file is then moved really quickly by another process, radarr/sonarr etc
                        Logger?.ILog("Initing new copied file");
                        InitFile(destination);
                    }
                    catch (Exception) { }

                    copied = true;
                }
                catch (Exception ex)
                {
                    Logger?.ELog("Failed to move file: " + ex.Message);
                }
            });

            while (task.IsCompleted == false)
            {
                long currentSize = 0;
                var destFileInfo = new FileInfo(destination);
                if (destFileInfo.Exists)
                    currentSize = destFileInfo.Length;

                if (PartPercentageUpdate != null)
                    PartPercentageUpdate(currentSize / fileSize * 100);
                Thread.Sleep(50);
            }

            if (copied == false)
                return false;

            if (PartPercentageUpdate != null)
                PartPercentageUpdate(100);
            return true;
        }

        public void Cancel()
        {
            this.Process?.Cancel();
        }

        public void UpdateVariables(Dictionary<string, object> updates)
        {
            if (updates == null)
                return;
            foreach (var key in updates.Keys)
            {
                if (Variables.ContainsKey(key))
                    Variables[key] = updates[key];
                else
                    Variables.Add(key, updates[key]);
            }
        }

        /// <summary>
        /// Replaces variables in a given string
        /// </summary>
        /// <param name="input">the input string</param>
        /// <param name="variables">the variables used to replace</param>
        /// <param name="stripMissing">if missing variables shouild be removed</param>
        /// <param name="cleanSpecialCharacters">if special characters (eg directory path separator) should be replaced</param>
        /// <returns>the string with the variables replaced</returns>
        public string ReplaceVariables(string input, bool stripMissing = false, bool cleanSpecialCharacters = false) => VariablesHelper.ReplaceVariables(input, Variables, stripMissing, cleanSpecialCharacters);


        /// <summary>
        /// Gets a safe filename with any reserved characters removed or replaced
        /// </summary>
        /// <param name="fullFileName">the full filename of the file to make safe</param>
        /// <returns>the safe filename</returns>
        public FileInfo GetSafeName(string fullFileName)
        {
            var dest = new FileInfo(fullFileName);

            string destName = dest.Name;
            string destDir = dest?.DirectoryName ?? "";

            // replace these here to avoid double spaces in name
            if (Path.GetInvalidFileNameChars().Contains(':'))
            {
                destName = destName.Replace(" : ", " - ");
                destName = destName.Replace(": ", " - ");
                destDir = destDir.Replace(" : ", " - ");
                destDir = destDir.Replace(": ", " - ");
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (c == ':')
                {
                    destName = destName.Replace(c.ToString(), " - ");
                    if(c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                        destDir = destDir.Replace(c.ToString(), " - ");
                }
                else
                {
                    destName = destName.Replace(c.ToString(), "");
                    if (c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                        destDir = destDir.Replace(c.ToString(), "");
                }
            }
            // put the drive letter back if it was replaced iwth a ' - '
            destDir = System.Text.RegularExpressions.Regex.Replace(destDir, @"^([a-z]) \- ", "$1:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return new FileInfo(Path.Combine(destDir, destName));
        }

        public bool CreateDirectoryIfNotExists(string directory)
        {
            if (Fake) return true;
            return Helpers.FileHelper.CreateDirectoryIfNotExists(Logger, directory);
        }

        /// <summary>
        /// Executes a cmd and returns the result
        /// </summary>
        /// <param name="args">The execution parameters</param>
        /// <returns>The result of the command</returns>
        public ProcessResult Execute(ExecuteArgs args)
        {
            if (Fake) return new ProcessResult {  ExitCode = 0, Completed = true };
            var result = Process.ExecuteShellCommand(args).Result;
            return result;
        }


        /// <summary>
        /// Gets a new guid as a string
        /// </summary>
        /// <returns>a new guid as a string</returns>
        public string NewGuid() => Guid.NewGuid().ToString();


        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        /// <typeparam name="T">The plugin settings to load</typeparam>
        /// <returns>The plugin settings</returns>
        public T GetPluginSettings<T>() where T : IPluginSettings
        {
            if (Fake) return default(T);
            string name = typeof(T).Namespace;
            name = name.Substring(name.IndexOf(".") + 1);
            string json = GetPluginSettingsJson(name);
            if (string.IsNullOrEmpty(json))
                return default(T);
            return (T)JsonSerializer.Deserialize<T>(json);
        }

        public string GetToolPath(string tool)
        {
            if (Fake || GetToolPathActual == null) return string.Empty;
            return GetToolPathActual(tool);
        }
    }


    public enum NodeResult
    {
        Failure = 0,
        Success = 1,
    }
}