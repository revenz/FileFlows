using System.Net;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.JsonRpc;
using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Plugin.Services;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.FileServices;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileHelper = FileFlows.Helpers.FileHelper;

namespace FileFlows.FlowRunner;

/// <summary>
/// A runner instance
/// </summary>
/// <param name="_parameters">the parameters for the runner</param>
public class RunInstance(RunnerProperties properties)
{
    public JsonRpcClient RpcClient => properties.RpcClient;
    public LibraryFile LibraryFile => properties.LibraryFile;
    
    public RunnerProperties Properties => properties;

    /// <summary>
    /// Runs the runner
    /// </summary>
    /// <returns>the exit code of the runner</returns>
    public FileStatus Run()
    {
        RunnerParameters parameters = RpcClient.Parameters;
        properties.StartingFlow = parameters.Flow;
        properties.ProcessingNode = RpcClient.Node;
        
        ServicePointManager.DefaultConnectionLimit = 50;
        try
        {
            properties.Uid = parameters.Uid;
            properties.NodeUid = parameters.NodeUid;
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

            string cfgJson;
            if (Environment.GetEnvironmentVariable("FF_NO_ENCRYPT") == "1")
            {
                LogInfo("No Encryption for Node configuration");
                cfgJson = File.ReadAllText(cfgFile);
            }
            else
            {
                LogInfo("Loading encrypted config");
                cfgJson = ConfigEncrypter.DecryptConfig(cfgFile);
            }

            var config = JsonSerializer.Deserialize<ConfigurationRevision>(cfgJson);

            string hostname = parameters.Hostname?.EmptyAsNull() ?? Environment.MachineName;

            Globals.IsDocker = parameters.IsDocker;
            LogInfo("Docker: " + Globals.IsDocker);

            string workingDir = parameters.WorkingDirectory;

            HttpHelper.Client = HttpHelper.GetDefaultHttpClient(RemoteService.ServiceBaseUrl);
            var result = Execute(new()
            {
                IsServer = parameters.IsInternalServerNode,
                Config = config,
                ConfigDirectory = cfgPath,
                TempDirectory = tempPath,
                LibraryFileUid = LibraryFile.Uid,
                WorkingDirectory = workingDir,
                Hostname = hostname
            });

            // we only want to return 0 here if to execute complete, the file may have finished in that, but it's been
            // successfully recorded/completed by that, so we don't need to tell the Node to update this file anymore

            return result.result;
        }
        catch (Exception ex)
        {
            LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            while (ex.InnerException != null)
            {
                LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                ex = ex.InnerException;
            }

            return FileStatus.ProcessingFailed;
        }
    }
    
    
    /// <summary>
    /// Executes the runner
    /// </summary>
    /// <param name="args">the args</param>
    /// <returns>the library file status, or null if library file was not loaded</returns>
    /// <exception cref="Exception">error was thrown</exception>
    (FileStatus result, bool KeepFiles) Execute(ExecuteArgs args)
    {
        ProcessingNode node = RpcClient.Basic.GetNode().Result;

        properties.ProcessingNode = node;
        properties.WorkingDirectory = args.WorkingDirectory;
            
        string workingFile = properties.LibraryFile.Name;
        
        if (properties.StartingFlow == null || properties.StartingFlow.Uid == Guid.Empty)
        {
            LogInfo("Flow not found, cannot process file: " + workingFile);
            properties.LibraryFile.FailureReason = "Flow not found";
            return (FileStatus.ProcessingFailed, false);
        }
        LogInfo("Flow: " + properties.StartingFlow.Name);
        // update the library file to reference the updated flow (if changed)
        if (properties.LibraryFile.Flow?.Name != properties.StartingFlow.Name || properties.LibraryFile.Flow?.Uid != properties.StartingFlow.Uid)
        {
            properties.LibraryFile.Flow = new ObjectReference
            {
                Uid = properties.StartingFlow.Uid,
                Name = properties.StartingFlow.Name,
                Type = typeof(Flow)?.FullName ?? string.Empty
            };
            RpcClient.LibraryFileHandler.UpdateLibraryFile(properties.LibraryFile).Wait();
        }


        IFileService _fileService;
        long initialSize = 0;
        
        string libPath = properties.LibraryFile.Library == null || string.IsNullOrWhiteSpace(properties.LibraryFile.RelativePath) || 
                         properties.LibraryFile.Library.Uid == Guid.Empty  || properties.LibraryFile.Library.Uid ==  CommonVariables.ManualLibraryUid ?
                        string.Empty : properties.LibraryFile.Name[..^(properties.LibraryFile.RelativePath.Length + 1)];
        Properties.IsDirectory = properties.StartingFlow.Parts.FirstOrDefault(x => x.Type == FlowElementType.Input)?.FlowElementUid
            ?.Contains("Folder") == true;

        if (Regex.IsMatch(workingFile, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            // url
            Properties.IsRemote = true;
            if (args.IsServer)
                _fileService = new LocalFileService(args.Config.DontUseTempFilesWhenMovingOrCopying);
            else if (args.Config.AllowRemote)
                _fileService = new RemoteFileService(properties.Uid, RemoteService.ServiceBaseUrl, args.WorkingDirectory,
                    properties.Logger,
                    properties.LibraryFile.Name.Contains('/') ? '/' : '\\', RemoteService.AccessToken, RemoteService.NodeUid,
                    args.Config.DontUseTempFilesWhenMovingOrCopying);
            else
                _fileService = new MappedFileService(node, properties.Logger, args.Config.DontUseTempFilesWhenMovingOrCopying);
        }
        else
        {
            FileSystemInfo file = Properties.IsDirectory ? new DirectoryInfo(workingFile) : new FileInfo(workingFile);
            bool fileExists = file.Exists; // set to variable so we can set this to false in debugging easily

#if(DEBUG)
            if (args.IsServer == false)
                fileExists = false;
#endif
            if (fileExists)
            {
                _fileService = args.IsServer
                    ? new LocalFileService(args.Config.DontUseTempFilesWhenMovingOrCopying) { Logger = properties.Logger }
                    : new MappedFileService(node, properties.Logger, args.Config.DontUseTempFilesWhenMovingOrCopying);
            }
            else
            {
                if (args.IsServer)
                {
                    // doesnt exist
                    //LogInfo("Library file does not exist, deleting from library files: " + file.FullName);
                    // RpcClient.LibraryFileHandler.DeleteLibraryFile(properties.LibraryFile.Uid).Wait();

                    properties.LibraryFile.FailureReason = "Library file does not exist.";
                    return (FileStatus.ProcessingFailed, false);
                }

                var existsResult = RpcClient.LibraryFileHandler.ExistsOnServer(properties.LibraryFile.Name, properties.LibraryFile.IsDirectory).Result;
                if (existsResult.Failed(out var error))
                {
                    properties.Logger.WLog(error);
                    return (FileStatus.ProcessingFailed, false);
                }

                if (existsResult.Value == false)
                {
                    // doesnt exist
                    //LogInfo("Library file does not exist, deleting from library files: " + file.FullName);
                    //RpcClient.LibraryFileHandler.DeleteLibraryFile(properties.LibraryFile.Uid).Wait();
                    properties.LibraryFile.FailureReason = "Library file does not exist. Not running on server.";
                    return (FileStatus.ProcessingFailed, false);
                }

                _fileService = new MappedFileService(node, properties.Logger,
                    args.Config.DontUseTempFilesWhenMovingOrCopying);
                
                bool exists = properties.IsDirectory
                    ? _fileService.DirectoryExists(workingFile)
                    : _fileService.FileExists(workingFile);

                if (exists == false)
                {
                    // need to try a remote
                    if (args.Config.AllowRemote == false)
                    {
                        string mappedPath = _fileService.GetLocalPath(workingFile);
                        properties.LibraryFile.FailureReason =
                            "Library file exists but is not accessible from node: " + mappedPath;
                        properties.Logger.ILog("Mapped Path: " + mappedPath);
                        properties.Logger.ELog(properties.LibraryFile.FailureReason);
                        properties.LibraryFile.ExecutedNodes = new List<ExecutedNode>();
                        return (FileStatus.ProcessingFailed, false);
                    }

                    if (properties.IsDirectory)
                    {
                        properties.LibraryFile.Status = FileStatus.ProcessingFailed;
                        properties.LibraryFile.FailureReason =
                            "Library folder exists, but remote file server is not available for folders: " +
                            file.FullName;
                        properties.Logger.ELog(properties.LibraryFile.FailureReason);
                        properties.LibraryFile.ExecutedNodes = new List<ExecutedNode>();
                        return (FileStatus.ProcessingFailed, false);
                    }

                    properties.IsRemote = true;
                    _fileService = new RemoteFileService(properties.Uid, RemoteService.ServiceBaseUrl,
                        args.WorkingDirectory,
                        properties.Logger,
                        properties.LibraryFile.Name.Contains('/') ? '/' : '\\', RemoteService.AccessToken,
                        RemoteService.NodeUid,
                        args.Config.DontUseTempFilesWhenMovingOrCopying);
                }
            }


            initialSize = properties.IsDirectory
                ? FileHelper.GetDirectorySize(workingFile)
                : _fileService.FileSize(workingFile).ValueOrDefault;
        }

        FileService.Instance = _fileService;

        if (initialSize == 0)
            initialSize = properties.LibraryFile.OriginalSize;

        properties.LibraryFile.Status = FileStatus.Processing;

        properties.LibraryFile.ProcessingStarted = DateTime.UtcNow;
        // LibraryFileService.Update(LibraryFile).Wait();
        properties.Config = args.Config;
        properties.ConfigDirectory = args.ConfigDirectory;

        var info = new FlowExecutorInfo
        {
            LibraryFile = properties.LibraryFile,
            NodeUid = node.Uid,
            NodeName = node.Name,
            IsRemote = properties.IsRemote,
            TotalParts = properties.StartingFlow.Parts.Count,
            CurrentPart = 0,
            CurrentPartPercent = 0,
            CurrentPartName = string.Empty,
            StartedAt = DateTime.UtcNow,
            WorkingFile = workingFile,
            IsDirectory = properties.IsDirectory,
            LibraryPath = libPath, 
            InitialSize = initialSize,
            AdditionalInfos = new ()
        };

        if (properties.IsDirectory == false)
        {
            // FF-1563: Set original size of file as it processes
            properties.LibraryFile.OriginalSize = info.InitialSize;
        }
        
        properties.Logger.ILog("Start Working File: " + info.WorkingFile);
        properties.LibraryFile.OriginalSize = info.InitialSize;
        properties.Logger.ILog("Initial Size: " + info.InitialSize);
        properties.Logger.ILog("File Service: "  + _fileService.GetType().Name);
        

        var runner = new Runner(this, properties.StartingFlow, node, args.WorkingDirectory);
        return runner.Run(properties.Logger);
    }

    internal void LogInfo(string message)
        => properties.Logger.ILog(message);
}