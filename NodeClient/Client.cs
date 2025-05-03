using System.Runtime.InteropServices;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using FileFlows.Plugin;
using Microsoft.AspNetCore.SignalR.Client;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public class Client : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly ILogger _logger;
    
    /// <summary>
    /// If DockerMods are installing
    /// </summary>
    private bool _InstallingDockerMods;

    /// <summary>
    /// The name of the DockerMod being installed
    /// </summary>
    private string _InstallingDockerMod;

    /// <summary>
    /// The client connection to the server
    /// </summary>
    public ClientConnection Connection { get; init; }
    
    private readonly string _hostname;
    private readonly RunnerManager _runnerManager;

    /// <summary>
    /// Gets the runner manager
    /// </summary>
    public RunnerManager Manager => _runnerManager;

    /// <summary>
    /// Gets the node
    /// </summary>
    public ProcessingNode? Node => Connection.Node;

    private bool _nodeStatusStarted;

    private readonly ClientParameters _parameters;
    private bool _updatingConfiguration = false;

    private readonly SemaphoreSlim _configurationSemaphore = new SemaphoreSlim(1, 1);

    private CancellationTokenSource _delayCts = new();
    private CancellationTokenSource _logSyncCts = new();
    private bool Disposed = false;
    private bool firstConnection = true;
    private bool _installingDockerMods;


    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="parameters">The client parameters</param>
    /// <param name="logger">The logger instance.</param>
    public Client(ClientParameters parameters, ILogger logger)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _configurationService = ServiceLoader.Load<ConfigurationService>();
        _configurationService.LoadFromDisk();
        _hostname = parameters.Hostname;
        _runnerManager = ServiceLoader.Load<RunnerManager>();
        _runnerManager.RunnerUpdated += OnRunnerUpdated;
        _runnerManager.Logger = _logger;

        Connection = new(logger, parameters.ServerUrl, parameters.AccessToken, GetRegisterParameters);


        Connection.FileCheckHandler = HandleClientProcessFile;
        Connection.AbortFileHandler = AbortFile;
        Connection.ConfigurationUpdated += (revision) => _ = UpdateConfiguration(revision);
        Connection.Connected += ConnectionOnConnected;
        
        EventManager.Subscribe("InstallingDockerMods", (bool installing) =>
        {
            _InstallingDockerMods = installing;
            TriggerStatusUpdate();
        });
        EventManager.Subscribe("InstallingDockerMod", (string dockerMod) =>
        {
            _InstallingDockerMod = dockerMod;
            TriggerStatusUpdate();
        });
    }


    /// <summary>
    /// Connection established and node registered
    /// </summary>
    private void ConnectionOnConnected()
    {
        if (Connection.ServerVersion != Globals.Version)
        {
            _logger.ILog(
                $"Server version '{Connection.ServerVersion}' does not match node version '{Globals.Version}'");
            EventManager.Broadcast("NodeVersionMismatch", Connection.ServerVersion);
        }
        else if (Connection.Node!.Enabled &&
                 Connection.ServerConfigRevision != _configurationService.CurrentConfig?.Revision)
        {
            Task.Run(() => UpdateConfiguration(Connection.ServerConfigRevision));
        }
        else if (firstConnection && Globals.IsDocker)
        {
            _logger?.ILog("Installing DockerMods for first connection");
            InstallDockerMods();
        }


        firstConnection = false;
        SendNodeStatusAsync();

        TriggerLogSync();
    }


    /// <summary>
    /// Called when a runner is updated
    /// </summary>
    private void OnRunnerUpdated()
        => TriggerStatusUpdate();

    /// <summary>
    /// Attempts to start the connection and retry if it fails initially.
    /// </summary>
    public void Start()
        => Connection.Start();
    

    /// <summary>
    /// Stops the connection and unregisters the node.
    /// </summary>
    public async Task StopAsync()
        => await Connection.StopAsync();

    /// <summary>
    /// Disposes the client, ensuring all resources are properly cleaned up.
    /// </summary>
    public void Dispose()
    {
        Disposed = true;
        if (_delayCts != null)
        {
            _delayCts.Cancel();
            _delayCts.Dispose();
        }

        Connection.Dispose();
    }

    /// <summary>
    /// Called when the configuration has to be updated
    /// </summary>
    /// <param name="revision">the revision on the server</param>
    private async Task UpdateConfiguration(int revision)
    {
        if (Connection.Node?.Enabled != true)
            return;
        const string prefix = "UpdateConfiguration:";
        _logger.ILog($"{prefix} Update Configuration to '{revision}' requested");

        if (_configurationService.CurrentConfig?.Revision >= revision)
        {
            _logger.ILog(
                $"{prefix} Configuration already updated to '{_configurationService.CurrentConfig?.Revision}'");
            return; // already up to date
        }

        await UpdateConfiguration();
    }

    /// <summary>
    /// Called when the configuration has to be updated
    /// </summary>
    public async Task UpdateConfiguration()
    {
        if (_updatingConfiguration)
            return;

        const string prefix = "UpdateConfiguration:";
        _logger.ILog($"{prefix} Updating configuration");
        if (Connection.Node == null)
        {
            _logger.ILog($"{prefix} Updating configuration: Node is null");
            return;
        }

        if (await _configurationSemaphore.WaitAsync(1_000) == false)
        {
            _logger.ILog($"{prefix} Failed to acquire configuration update semaphore, already updating.");
            return;
        }

        try
        {
            _updatingConfiguration = true;
            TriggerStatusUpdate();
            _logger.ILog($"{prefix} Updating configuration...");
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var cfg = await Connection.InvokeAsync<ConfigurationRevision>("GetConfiguration");

            if (cfg != null && _configurationService.CurrentConfig?.Revision != cfg.Revision && Connection.Node is {} node)
            {
                await _configurationService.SaveConfiguration(cfg, node);
                _logger.ILog($"{prefix} Configuration updated");
            }
        }
        catch (Exception ex)
        {
            // Ignore
            _logger.WLog($"{prefix} Failed to update: {ex}");
        }
        finally
        {
            _updatingConfiguration = false;
            _configurationSemaphore.Release();
        }

        TriggerStatusUpdate();
    }

    /// <summary>
    /// Gets the register parameters
    /// </summary>
    /// <returns>the register parameters</returns>
    private NodeRegisterParameters GetRegisterParameters()
    {
        string path = DirectoryHelper.BaseDirectory;

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
        {
            new()
            {
                Server = "ffmpeg",
                Local = Globals.IsDocker ? "/usr/local/bin/ffmpeg" :
                    windows ? Path.Combine(path, "Tools", "ffmpeg.exe") : "/usr/local/bin/ffmpeg"
            }
        };
        if (_parameters.EnvironmentalMappings?.Any() == true)
        {
            _logger.ILog("Environmental mappings found, adding those");
            mappings.AddRange(_parameters.EnvironmentalMappings);
        }

        string tempPath = _parameters.ForcedTempPath?.EmptyAsNull()
                          ?? (Globals.IsDocker ? "/temp" : Path.Combine(DirectoryHelper.BaseDirectory, "Temp"));

        HardwareInfo? hardwareInfo = null;
        try
        {
            hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
            _logger.ILog("Hardware Info: " + Environment.NewLine + hardwareInfo);
        }
        catch (Exception ex)
        {
            _logger.ELog("Failed to get hardware info: " + ex.Message);
        }

        var parameters = new NodeRegisterParameters()
        {
            Hostname = _hostname,
            ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
            HardwareInfo = hardwareInfo,
            TempPath = tempPath,
            Version = Globals.Version,
            Mappings = mappings.Select(x => new KeyValuePair<string, string>(x.Server, x.Local)).ToList(),
            Architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm32 :
                RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? ArchitectureType.Arm64 :
                RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm64 :
                RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ArchitectureType.x64 :
                RuntimeInformation.ProcessArchitecture == Architecture.X86 ? ArchitectureType.x86 :
                IntPtr.Size == 8 ? ArchitectureType.x64 :
                IntPtr.Size == 4 ? ArchitectureType.x86 :
                ArchitectureType.Unknown,
            OperatingSystem = Globals.IsDocker ? OperatingSystemType.Docker :
                Globals.IsWindows ? OperatingSystemType.Windows :
                Globals.IsLinux ? OperatingSystemType.Linux :
                Globals.IsMac ? OperatingSystemType.Mac :
                Globals.IsFreeBsd ? OperatingSystemType.FreeBsd :
                OperatingSystemType.Unknown,
            ActiveRunners = _runnerManager?.ActiveRunners?.Keys?.ToArray() ?? []
        };
        return parameters;
    }

    /// <summary>
    /// Aborts a file
    /// </summary>
    /// <param name="uid">the UID of the file to abort</param>
    /// <returns>true if the runner was requested to cancel</returns>
    private async Task<bool> AbortFile(Guid uid)
        => await _runnerManager.AbortRunner(uid);

    /// <summary>
    /// Sends a update to the server about this node
    /// </summary>
    private void SendNodeStatusAsync()
    {
        if (_nodeStatusStarted)
            return;
        
        _nodeStatusStarted = true;
        _ = Task.Run(async () =>
        {
            while (Disposed == false)
            {
                try
                {
                    if (await Connection.AwaitConnection() == false )
                    {
                        _logger.WLog("Failed to await connection to send node status update");
                        await Task.Delay(TimeSpan.FromSeconds(5), _delayCts.Token);
                        continue;
                    }
                
                    if (Connection.Node is { } node == false)
                    {
                        _logger.WLog("Node was null, cannot send node status update");
                        await Task.Delay(TimeSpan.FromSeconds(5), _delayCts.Token);
                        continue;
                    }
                
                    var info = new
                    {
                        Name = node.Name?.EmptyAsNull() ?? _hostname,
                        NodeUid = node.Uid,
                        ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                        NodeVersion = Globals.Version,
                        UpdatingConfiguration = _updatingConfiguration,
                        InstallingDockerMods = _InstallingDockerMods,
                        InstallingDockerMod = _InstallingDockerMods ? _InstallingDockerMod?.EmptyAsNull() : null,
                        Runners = _runnerManager.ActiveRunners
                            .Where(x =>
                                x.Value.FinishedProcessing == false &&
                                // x.Value.IsRunning && 
                                x.Value.Info.LibraryFile != null)
                            .ToDictionary(x => x.Key, x => x.Value.Info),
                    };
                    var result = await Connection.InvokeAsync<NodeStatusUpdateResult>("UpdateNodeStatus", info);

                    if (result == NodeStatusUpdateResult.UpdateConfiguration)
                    {
                        _logger.ILog($"Configuration out of date for {(node.Name?.EmptyAsNull() ?? _hostname)}");
                        _ = UpdateConfiguration(); // do not await this, as this could cause the node to stop sending updates
                    }

                    _logger.ILog("Node Status Update Result: " + result);
                }
                catch (Exception ex)
                {
                    _logger.ELog($"Error sending node status: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), _delayCts.Token);
                }
                catch (Exception)
                {
                    // Task.Delay was interrupted, continue immediately
                }
            }
           
            _nodeStatusStarted = false; 
            _logger.ILog("Disposed, stop sending node status updates");
        });
        
        _ = SyncLog();
    }

    /// <summary>
    /// Syncs the node log with the server
    /// </summary>
    private async Task SyncLog()
    {
        TimeSpan delay = TimeSpan.FromSeconds(5);
        while (true)
        {
            try
            {
                if (Disposed)
                    return;
                
                await Task.Delay(delay, _logSyncCts.Token);
                
                if (Disposed)
                    return;

                var node = Connection.Node;
                if (node == null)
                {
                    delay = TimeSpan.FromSeconds(10);
                    continue;
                }

                if (node.Uid == CommonVariables.InternalNodeUid)
                    return; // we dont sync this log

                if (Logger.Instance.TryGetLogger(out FileLogger logger) == false)
                {
                    delay = TimeSpan.FromMinutes(5);
                    continue;
                }


                var file = logger.GetLogFilename();
                if (File.Exists(file) == false)
                {
                    delay = TimeSpan.FromMinutes(5);
                    continue;
                }

                var log = await File.ReadAllTextAsync(file);
                if (string.IsNullOrWhiteSpace(log))
                {
                    delay = TimeSpan.FromMinutes(5);
                    continue;
                }

                var compressed = Gzipper.CompressToBytes(log);

                if (await Connection.AwaitConnection() == false)
                {
                    delay = TimeSpan.FromMinutes(5);
                    continue;
                }

                await Connection.InvokeAsync("SyncLog", node.Uid, compressed);
                delay = TimeSpan.FromMinutes(60);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    /// <summary>
    /// Installs the DockerMods
    /// </summary>
    private void InstallDockerMods()
    {
        if (Globals.IsDocker == false || Node == null)
            return;
        _logger.ILog("Client.InstallDockerMods");
        _installingDockerMods = true;
        _ = Task.Run(async () =>
        {
            _logger.ILog("Client.InstallDockerMods executing");
            try
            {
                await _configurationService.InstallDockerMods(Node);
                _logger.ILog("Client.InstallDockerMods done");
            }
            catch (Exception ex)
            {
                _logger.ILog($"Client.InstallDockerMods error: {ex}");
            }

            _installingDockerMods = false;
        });

    }

    /// <summary>
    /// Triggers an immediate node status update.
    /// </summary>
    public void TriggerStatusUpdate()
    {
        try
        {
            _delayCts.Cancel(); // Cancels the current delay
            _delayCts.Dispose();
            _delayCts = new CancellationTokenSource(); // Reset for the next delay
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    /// Triggers an immediate node status update.
    /// </summary>
    private void TriggerLogSync()
    {
        try
        {
            _logSyncCts.Cancel(); // Cancels the current delay
            _logSyncCts.Dispose();
            _logSyncCts = new CancellationTokenSource(); // Reset for the next delay
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    private async Task<FileCheckResult> HandleClientProcessFile(RunFileArguments args)
    {
        if (_installingDockerMods)
            return FileCheckResult.CannotProcess;
        
        //_logger.ILog("Handling process file request...");
        try
        {
            //_logger.ILog($"Trying to start runner for: {args.LibraryFile.Name}");
            var result = await _runnerManager.TryStartRunner(this, args, 
                Connection.Node!, _configurationService.CurrentConfig!);
            //_logger.ILog($"Process file result: {result}");
            if (result == FileCheckResult.CanProcess)
                TriggerStatusUpdate();
            else
                _runnerManager.ActiveRunners.Remove(args.LibraryFile.Uid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error processing file: {ex.Message}");
            return FileCheckResult.UnknownError;
        }
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <returns>true if could start processing</returns>
    public async Task<bool> FileStartProcessing(LibraryFile libraryFile)
    {
        int count = 0;
        while (++count < 60)
        {
            try
            {
                var node = Connection.Node;
                if (node != null)
                {
                    return await Connection.InvokeAsync<bool>("FileStartProcessing", libraryFile, new ObjectReference()
                    {
                        Uid = node.Uid,
                        Name = node.Name,
                        Type = node.GetType().FullName!
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.WLog(
                    $"Failed to notify file '{libraryFile.RelativePath}' started processing[{count}]: {ex.Message}");
            }

            await Task.Delay(500);
        }

        return false;
    }

    /// <summary>
    /// Called when the file finishes processing
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <param name="log">the complete log of the file processing</param>
    public async Task FileFinishProcessing(LibraryFile libraryFile, string log)
    {
        int count = 0;
        while (++count < 10)
        {
            try
            {
                if (await Connection.AwaitConnection() == false)
                {
                    _logger.WLog("Failed to connect to notify file finished. Attempt: " + count);
                    continue;
                }

                var result = await Connection.InvokeAsync<bool>("FileFinishProcessing", libraryFile, log);
                if (result)
                    return;
            }
            catch (Exception ex)
            {
                _logger.WLog(
                    $"Failed to notify file '{libraryFile.RelativePath}' finished processing[{count}]: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Prepends the text to the log file on the server
    /// </summary>
    /// <param name="libFileUid">the UID of the file</param>
    /// <param name="lines">the lines of the log</param>
    /// <param name="overwrite">if the file should be overwritten or appended to</param>
    public async Task FileLogAppend(Guid libFileUid, string lines, bool overwrite = false)
    {
        try
        {
            await Connection.SendAsync(nameof(FileLogAppend), libFileUid, lines, overwrite);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Sends a log message to the server
    /// </summary>
    /// <param name="messages">the messages being logged</param>
    public async Task Log(string[] messages)
    {
        try
        {
            if(await Connection.AwaitConnection(0) == false)
                Console.WriteLine("Couldn't send log to server due to lost connection.");
            
            await Connection.SendAsync(nameof(Log),
                Connection.Node?.Address?.EmptyAsNull() ??
                Connection.Node?.Name?.EmptyAsNull() ?? Environment.MachineName, messages);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

}
