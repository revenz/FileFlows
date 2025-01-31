using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using FileFlows.Plugin.Services;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;

namespace FileFlows.FlowRunner;

public class RunInstance
{
    
    /// <summary>
    /// Gets the runner instance UID
    /// </summary>
    public Guid Uid { get; private set; }
    /// <summary>
    /// Gets the Node UID
    /// </summary>
    public Guid NodeUid { get; private set; }

    /// <summary>
    /// The flow logger
    /// </summary>
    public FlowLogger Logger;
    
    /// <summary>
    /// Gets or sets the configuration that is currently being executed
    /// </summary>
    public ConfigurationRevision Config { get; set; }
    
    /// <summary>
    /// Gets or sets the directory where the configuration is saved
    /// </summary>
    public string ConfigDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the working directory
    /// </summary>
    public string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the processing node this is running on
    /// </summary>
    public ProcessingNode ProcessingNode { get; set; }

    /// <summary>
    /// Runs the runner
    /// </summary>
    /// <param name="args">the args for the runner</param>
    /// <returns>the exit code of the runner</returns>
    public int Run(string[] args)
    {
        Logger = new (this, null);
        LogInfo("Flow Runner Version: " + Globals.Version);

        string encrypted = args[0];
        string decrypted = Decrypter.Decrypt(encrypted, "hVYjHrWvtEq8huShjTkA" + args[1] + "oZf4GW3jJtjuNHlMNpl9");
        RunnerParameters parameters = JsonSerializer.Deserialize<RunnerParameters>(decrypted)!;
        
        ServicePointManager.DefaultConnectionLimit = 50;
        try
        {
            Uid = parameters.Uid;
            NodeUid = parameters.NodeUid;
            LogInfo("Base URL: " + parameters.BaseUrl);
            RemoteService.ServiceBaseUrl = parameters.BaseUrl;
            RemoteService.AccessToken = parameters.AccessToken;
            RemoteService.NodeUid = parameters.RemoteNodeUid;

            string tempPath = parameters.TempPath;
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
                throw new Exception("Temp path doesnt exist: " + tempPath);
            LogInfo("Temp Path: " + tempPath);

            string cfgPath = parameters.ConfigPath;
            if (string.IsNullOrEmpty(cfgPath) || Directory.Exists(cfgPath) == false)
                throw new Exception("Configuration Path doesnt exist: " + cfgPath);
            LogInfo("Configuration Path: " + cfgPath);

            string cfgFile = Path.Combine(cfgPath, "config.json");
            if (File.Exists(cfgFile) == false)
                throw new Exception("Configuration file doesnt exist: " + cfgFile);
            LogInfo("Configuration File: " + cfgFile);

            string cfgKey = parameters.ConfigKey;
            if (string.IsNullOrEmpty(cfgKey))
                throw new Exception("Configuration Key not set");
            bool noEncrypt = cfgKey == "NO_ENCRYPT";
            string cfgJson;
            if (noEncrypt)
            {
                LogInfo("No Encryption for Node configuration");
                cfgJson = File.ReadAllText(cfgFile);
            }
            else
            {
                LogInfo("Using configuration encryption key: " + cfgKey);
                cfgJson = ConfigDecrypter.DecryptConfig(cfgFile, cfgKey);
            }

            var config = JsonSerializer.Deserialize<ConfigurationRevision>(cfgJson);

            string hostname = parameters.Hostname?.EmptyAsNull() ?? Environment.MachineName;

            Globals.IsDocker = parameters.IsDocker;
            LogInfo("Docker: " + Globals.IsDocker);

            string workingDir = Path.Combine(tempPath, "Runner-" + Uid);
            #if(DEBUG)
            if (string.IsNullOrWhiteSpace(parameters.RunnerTempPath) == false)
                workingDir = Path.Combine(tempPath, parameters.RunnerTempPath);
            #endif
            WorkingDirectory = workingDir;
            LogInfo("Working Directory: " + workingDir);
            try
            {
                if (Directory.Exists(workingDir) == false)
                {
                    Directory.CreateDirectory(workingDir);
                    LogInfo("Created Directory: " + workingDir);
                }
            }
            catch (Exception ex) when (Globals.IsDocker)
            {
                // this can throw if mapping inside a docker container is not valid, or the mapped location has become unavailable
                LogError("==========================================================================================");
                LogError(
                    "Failed to create working directory, this is likely caused by the mapped '/temp' directory is missing or has become unavailable from the host machine");
                LogError(ex.Message);
                LogError("==========================================================================================");
                return (int)FileStatus.ProcessingFailed;
            }

            var libfileUid = parameters.LibraryFile;
            HttpHelper.Client = HttpHelper.GetDefaultHttpClient(RemoteService.ServiceBaseUrl);
            var result = Execute(new()
            {
                IsServer = parameters.IsInternalServerNode,
                Config = config,
                ConfigDirectory = cfgPath,
                TempDirectory = tempPath,
                LibraryFileUid = libfileUid,
                WorkingDirectory = workingDir,
                Hostname = hostname
            });

            // we only want to return 0 here if to execute complete, the file may have finished in that, but it's been
            // successfully recorded/completed by that, so we don't need to tell the Node to update this file anymore

            return result.Success ? (result.KeepFiles ? 100 : 0) : (int)FileStatus.ProcessingFailed;
        }
        catch (Exception ex)
        {
            LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            while (ex.InnerException != null)
            {
                LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                ex = ex.InnerException;
            }

            return (int)FileStatus.ProcessingFailed;
        }
    }
    
    
    /// <summary>
    /// Executes the runner
    /// </summary>
    /// <param name="args">the args</param>
    /// <returns>the library file status, or null if library file was not loaded</returns>
    /// <exception cref="Exception">error was thrown</exception>
    (bool Success, bool KeepFiles) Execute(ExecuteArgs args)
    {
        ProcessingNode? node;
        var nodeService = ServiceLoader.Load<INodeService>();
        try
        {
            string address = args.IsServer ? "INTERNAL_NODE" : args.Hostname;
            LogInfo("Address: " + address);
            node = nodeService.GetByUidAsync(NodeUid).Result;  // throws if not found
            LogInfo("Node SignalrUrl: " + node.SignalrUrl);
            ProcessingNode = node;
        }
        catch (Exception ex)
        {
            LogInfo("Failed to register node: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }

        if ((node.Address == "FileFlowsServer" || node.SignalrUrl == "flow") && string.IsNullOrEmpty(RemoteService.ServiceBaseUrl) == false)
            FlowRunnerCommunicator.SignalrUrl = RemoteService.ServiceBaseUrl.EndsWith('/')
                ? RemoteService.ServiceBaseUrl + "flow"
                : RemoteService.ServiceBaseUrl + "/flow";
        else
            FlowRunnerCommunicator.SignalrUrl = node.SignalrUrl;

        var libFileService = ServiceLoader.Load<ILibraryFileService>();
        var libFile = libFileService.Get(args.LibraryFileUid).Result;
        if (libFile == null)
        {
            LogInfo("Library file not found, must have been deleted from the library files.  Nothing to process");
            return (true, false); // nothing to process
        }

        // string workingFile = node.Map(libFile.Name);
        string workingFile = libFile.Name;

        var libfileService = ServiceLoader.Load<ILibraryFileService>();
        var lib = args.Config.Libraries.FirstOrDefault(x => x.Uid == libFile.LibraryUid);
        if (lib == null && libFile.LibraryUid == CommonVariables.ManualLibraryUid)
        {
            lib = new()
            {
                Uid = CommonVariables.ManualLibraryUid, Name = CommonVariables.ManualLibrary, Enabled = true,
                Schedule = new string('1', 672)
            };
        }

        if (lib == null)
        {
            LogInfo("Library was not found, deleting library file");
            libFile.Status = FileStatus.MissingLibrary;
            FinishEarly(libFile);
            libfileService.Delete(libFile.Uid).Wait();
            return (true, false);
        }


        var flow = args.Config.Flows.FirstOrDefault(x => 
            x.Uid == (libFile.FlowUid != null && libFile.FlowUid != Guid.Empty ? libFile.FlowUid : lib.Flow?.Uid ?? Guid.Empty));
        if (flow == null || flow.Uid == Guid.Empty)
        {
            LogInfo("Flow not found, cannot process file: " + workingFile);
            libFile.Status = FileStatus.FlowNotFound;
            FinishEarly(libFile);
            return (true, false);
        }
        LogInfo("Flow: " + flow.Name);
        // update the library file to reference the updated flow (if changed)
        if (libFile.Flow?.Name != flow.Name || libFile.Flow?.Uid != flow.Uid)
        {
            libFile.Flow = new ObjectReference
            {
                Uid = flow.Uid,
                Name = flow.Name,
                Type = typeof(Flow)?.FullName ?? string.Empty
            };
            // libfileService.Update(libFile).Wait();
        }


        IFileService _fileService;
        bool remoteFile = false;
        long initialSize = 0;

        if (Regex.IsMatch(workingFile, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            // url
            remoteFile = true;
            if (args.IsServer)
                _fileService = new LocalFileService(args.Config.DontUseTempFilesWhenMovingOrCopying);
            else if (args.Config.AllowRemote)
                _fileService = new RemoteFileService(Uid, RemoteService.ServiceBaseUrl, args.WorkingDirectory,
                    Logger,
                    libFile.Name.Contains('/') ? '/' : '\\', RemoteService.AccessToken, RemoteService.NodeUid,
                    args.Config.DontUseTempFilesWhenMovingOrCopying);
            else
                _fileService = new MappedFileService(node, Logger, args.Config.DontUseTempFilesWhenMovingOrCopying);
        }
        else
        {
            FileSystemInfo file = lib.Folders ? new DirectoryInfo(workingFile) : new FileInfo(workingFile);
            bool fileExists = file.Exists; // set to variable so we can set this to false in debugging easily

#if(DEBUG)
            if (args.IsServer == false)
                fileExists = false;
#endif
            if (fileExists)
            {
                _fileService = args.IsServer
                    ? new LocalFileService(args.Config.DontUseTempFilesWhenMovingOrCopying) { Logger = Logger }
                    : new MappedFileService(node, Logger, args.Config.DontUseTempFilesWhenMovingOrCopying);
            }
            else if (args.IsServer || libfileService.ExistsOnServer(libFile.Uid).Result == false)
            {
                // doesnt exist
                libFile.Status = FileStatus.Processed;
                LogInfo("Library file does not exist, deleting from library files: " + file.FullName);
                FinishEarly(libFile);
                libfileService.Delete(libFile.Uid).Wait();
                return (true, false);
            }
            else
            {
                _fileService = new MappedFileService(node, Logger, args.Config.DontUseTempFilesWhenMovingOrCopying);
                bool exists = lib.Folders
                    ? _fileService.DirectoryExists(workingFile)
                    : _fileService.FileExists(workingFile);

                if (exists == false)
                {
                    // need to try a remote
                    if (args.Config.AllowRemote == false)
                    {
                        string mappedPath = _fileService.GetLocalPath(workingFile);
                        libFile.FailureReason =
                            "Library file exists but is not accessible from node: " + mappedPath;
                        LogInfo("Mapped Path: " + mappedPath);
                        LogError(libFile.FailureReason);
                        libFile.Status = FileStatus.MappingIssue;
                        libFile.ExecutedNodes = new List<ExecutedNode>();
                        FinishEarly(libFile);
                        return (true, false);
                    }

                    if (lib.Folders)
                    {
                        libFile.Status = FileStatus.MappingIssue;
                        libFile.FailureReason =
                            "Library folder exists, but remote file server is not available for folders: " +
                            file.FullName;
                        LogError(libFile.FailureReason);
                        libFile.ExecutedNodes = new List<ExecutedNode>();
                        FinishEarly(libFile);
                        return (true, false);
                    }

                    remoteFile = true;
                    _fileService = new RemoteFileService(Uid, RemoteService.ServiceBaseUrl, args.WorkingDirectory,
                        Logger,
                        libFile.Name.Contains('/') ? '/' : '\\', RemoteService.AccessToken, RemoteService.NodeUid,
                        args.Config.DontUseTempFilesWhenMovingOrCopying);
                }
            }

            initialSize = lib.Folders
                ? GetDirectorySize(workingFile)
                : _fileService.FileSize(workingFile).ValueOrDefault;
        }

        FileService.Instance = _fileService;

        libFile.Status = FileStatus.Processing;

        libFile.ProcessingStarted = DateTime.UtcNow;
        // libfileService.Update(libFile).Wait();
        Config = args.Config;
        ConfigDirectory = args.ConfigDirectory;

        var info = new FlowExecutorInfo
        {
            Uid = Uid,
            LibraryFile = libFile,
            //Log = String.Empty,
            NodeUid = node.Uid,
            NodeName = node.Name,
            RelativeFile = libFile.RelativePath,
            Library = libFile.Library,
            IsRemote = remoteFile,
            TotalParts = flow.Parts.Count,
            CurrentPart = 0,
            CurrentPartPercent = 0,
            CurrentPartName = string.Empty,
            StartedAt = DateTime.UtcNow,
            WorkingFile = workingFile,
            IsDirectory = lib.Folders,
            LibraryPath = lib.Path, 
            //Fingerprint = lib.UseFingerprinting,
            InitialSize = initialSize,
            AdditionalInfos = new ()
        };

        if (lib.Folders == false)
        {
            // FF-1563: Set original size of file as it processes
            libFile.OriginalSize = info.InitialSize;
        }
        
        LogInfo("Start Working File: " + info.WorkingFile);
        info.LibraryFile.OriginalSize = info.InitialSize;
        LogInfo("Initial Size: " + info.InitialSize);
        LogInfo("File Service: "  + _fileService.GetType().Name);
        

        var runner = new Runner(this, info, flow, node, args.WorkingDirectory);
        return runner.Run(Logger);
    }

    private void FinishEarly(LibraryFile libFile)
    {
        FlowExecutorInfo info = new()
        {
            Uid = Uid,
            LibraryFile = libFile,
            NodeUid = ProcessingNode.Uid,
            NodeName = ProcessingNode.Name,
            RelativeFile = libFile.RelativePath,
            Library = libFile.Library
        };
        var log = Logger.ToString();
        new FlowRunnerService().Finish(info).Wait();
    }

    private long GetDirectorySize(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception ex)
        {
            LogInfo("Failed retrieving directory size: " + ex.Message);
            return 0;
        }
    }


    internal void LogInfo(string message)
    {
        if(Logger != null)
            Logger.ILog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [INFO] -> " + message);
    }
    internal void LogWarning(string message)
    {
        if(Logger != null)
            Logger.WLog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [WARN] -> " + message);
    }

    internal void LogError(string message)
    {
        if (Logger != null)
            Logger.ELog(message);
        else
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [ERRR] -> " + message);
    }
}