using System.Runtime.InteropServices;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public class Client : IDisposable
{
    private readonly HubConnection _connection;
    private readonly string _hostname;
    private readonly RunnerManager _runnerManager;
    private readonly ConfigurationService _configurationService;
    private readonly ILogger _logger;
    private ProcessingNode? _node;
    public Guid? NodeUid => _node?.Uid;
    private TaskCompletionSource<bool> _registrationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _disposed;
    private ClientParameters _parameters;
    
    public bool IsRegistered => _node != null;
    private Guid _nodeUid => _node?.Uid ?? Guid.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="parameters">The client parameters</param>
    /// <param name="logger">The logger instance.</param>
    public Client(ClientParameters parameters, ILogger logger)
    {
        _parameters = parameters;
        _configurationService = ServiceLoader.Load<ConfigurationService>();
        _hostname = parameters.Hostname;
        _runnerManager = new();
        _logger = logger;

        parameters.ServerUrl = parameters.ServerUrl.Replace("http:", "ws:").Replace("https:", "wss:").TrimEnd('/');

        _connection = new HubConnectionBuilder()
            .WithUrl($"{parameters.ServerUrl}/node", options =>
            {
                if (string.IsNullOrWhiteSpace(parameters.AccessToken) == false)
                    options.Headers.Add("Authorization", parameters.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<RunFileArguments, Task<bool>>("ClientProcessFile", HandleClientProcessFile);
        _connection.On<ProcessingNode>("NodeUpdated", UpdateNode);
        _connection.On<ConfigurationRevision>("ConfigUpdated", UpdateConfiguration);

        _connection.Reconnecting += error =>
        {
            _logger.WLog("Connection lost, attempting to reconnect...");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            _logger.ILog("Reconnected, registering node...");
            await RegisterNodeAsync();
        };

        _connection.Closed += error =>
        {
            _logger.WLog("Connection closed.");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };
    }

    private async Task DownloadConfiguration()
    {
        try
        {
            if (_node == null)
                return;

            _logger.ILog("Downloading configuration...");
            var cfg = await _connection.InvokeAsync<ConfigurationRevision>("GetConfiguration");
            if (cfg == null || _node == null)
                return;
            
            await _configurationService.SaveConfiguration(cfg, _node);
            _logger.ILog($"Node '{_node.Name}' Configuration updated to {cfg.Revision}");
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error downloading configuration: {ex.Message}");
        }
    }

    private async Task UpdateConfiguration(ConfigurationRevision obj)
    {
        if (_node == null)
            return;
        
        _logger.ILog("Updating configuration...");
        await _configurationService.SaveConfiguration(obj, _node);
    }

    private void UpdateNode(ProcessingNode obj)
    {
        _logger.ILog("Updating node information...");
        _node = obj;
        _registrationCompletion.TrySetResult(true);
    }

    /// <summary>
    /// Starts the connection and attempts to register the node once connected.
    /// </summary>
    public async Task StartAsync()
    {
        _logger.ILog("Starting client...");
        await _connection.StartAsync();
        await RegisterNodeAsync();
        _ = Task.Run(SendNodeStatusAsync);
    }

    /// <summary>
    /// Registers the node with the server
    /// </summary>
    private async Task RegisterNodeAsync()
    {
        try
        {
            _logger.ILog("Registering node...");
            _node = null;
            
            string path = DirectoryHelper.BaseDirectory;

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
            {
                new()
                {
                    Server = "ffmpeg",
                    Local =  Globals.IsDocker ? "/usr/local/bin/ffmpeg" :
                        windows ? Path.Combine(path, "Tools", "ffmpeg.exe") : "/usr/local/bin/ffmpeg"
                }
            };
            if (_parameters.EnvironmentalMappings?.Any() == true)
            {
                Logger.Instance.ILog("Environmental mappings found, adding those");
                mappings.AddRange(_parameters.EnvironmentalMappings);
            }

            string tempPath =  _parameters.ForcedTempPath?.EmptyAsNull() 
                               ?? (Globals.IsDocker ? "/temp" : 
                                   Path.Combine(DirectoryHelper.BaseDirectory, "Temp"));
        
            HardwareInfo? hardwareInfo = null;
            try
            {
                hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
                Logger.Instance?.ILog("Hardware Info: " + Environment.NewLine + hardwareInfo);
            }
            catch(Exception ex)
            {
                Logger.Instance?.ELog("Failed to get hardware info: " + ex.Message);
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
            };
            
            var result = await _connection.InvokeAsync<NodeRegisterResult>("RegisterNode", parameters);
            
            if (!result.Success)
                return;
            
            _node = result.Node;
            _logger.ILog("Node successfully registered.");
            _registrationCompletion.TrySetResult(true);
            
            if (result.CurrentConfigRevision != _configurationService.CurrentConfig?.Revision)
                _ = DownloadConfiguration();
        }
        catch (Exception ex)
        {
            _logger.ELog($"Node registration failed: {ex.Message}");
            _registrationCompletion.TrySetResult(false);
        }
    }

    private async Task SendNodeStatusAsync()
    {
        while (true)
        {
            try
            {
                if (_connection.State == HubConnectionState.Connected && _node != null)
                {
                    _logger.ILog($"Sending node status from node '{_node?.Name ?? Environment.MachineName}'...");
                    await _connection.SendAsync("UpdateNodeStatus", new
                    {
                        NodeUid = _nodeUid,
                        ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                        MaxRunners = _node!.FlowRunners,
                        ActiveRunners = _runnerManager.GetActiveRunnerUids()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.ELog($"Error sending node status: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    public async Task StopAsync()
    {
        if (IsRegistered)
        {
            try
            {
                _logger.ILog("Unregistering node...");
                await _connection.InvokeAsync("UnregisterNode", _nodeUid);
            }
            catch (Exception ex)
            {
                _logger.WLog($"Failed to unregister node: {ex.Message}");
            }
            _node = null;
        }
        await _connection.StopAsync();
    }

    private async Task EnsureRegisteredAsync()
    {
        if (!IsRegistered)
        {
            _logger.ILog("Waiting for registration...");
            await _registrationCompletion.Task;
        }
    }

    private async Task<bool> HandleClientProcessFile(RunFileArguments args)
    {
        await EnsureRegisteredAsync();
        _logger.ILog("Handling process file request...");

        try
        {
            if (_configurationService.CurrentConfig?.Revision < args.ConfigRevision)
            {
                var updateTask = _configurationService.UpdateConfiguration(args.ConfigRevision, _node);
                if (await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromSeconds(10))) != updateTask)
                {
                    _logger.WLog("Configuration update timed out.");
                    return false;
                }
                if (_configurationService.CurrentConfig?.Revision < args.ConfigRevision)
                    return false;
            }

            bool result = _runnerManager.TryStartRunner(args, _node, _configurationService.CurrentConfig!);
            _logger.ILog($"Process file result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error processing file: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger.ILog("Disposing client...");
        StopAsync().GetAwaiter().GetResult();
        _connection.DisposeAsync().GetAwaiter().GetResult();
    }
}

/// <summary>
/// The client paraemeters
/// </summary>
public class ClientParameters
{
    /// <summary>
    /// Gets or sets the Server URL
    /// </summary>
    public string ServerUrl { get; set; }
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string Hostname  { get; set; }
    /// <summary>
    /// Gets or sets the access token
    /// </summary>
    public string AccessToken { get; set; }
    
    /// <summary>
    /// Gets or sets a forced temporary path
    /// </summary>
    public string? ForcedTempPath { get; set; }

    /// <summary>
    /// Gets or sets mappings passed in via enviromental values
    /// </summary>
    public List<RegisterModelMapping>? EnvironmentalMappings { get; set; }
}
