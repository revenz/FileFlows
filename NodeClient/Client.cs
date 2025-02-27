using FileFlows.RemoteServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public class Client
{
    private readonly HubConnection _connection;
    private readonly string _hostname;
    private readonly RunnerManager _runnerManager;
    private readonly ConfigurationService _configurationService;
    private readonly ILogger _logger;
    private ProcessingNode? _node;
    private TaskCompletionSource<bool> _registrationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    private bool _isRegistered => _node != null;
    private Guid _nodeUid => _node?.Uid ?? Guid.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="serverUrl">The server URL.</param>
    /// <param name="hostname">The hostname of the node.</param>
    /// <param name="accessToken">The access token for authentication.</param>
    /// <param name="logger">The logger instance.</param>
    public Client(string serverUrl, string hostname, string accessToken, ILogger logger)
    {
        _configurationService = ServiceLoader.Load<ConfigurationService>();
        _hostname = hostname;
        _runnerManager = new();
        _logger = logger;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/node", options =>
            {
                if (!string.IsNullOrWhiteSpace(accessToken))
                    options.Headers.Add("Authorization", accessToken);
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

    private async Task RegisterNodeAsync()
    {
        try
        {
            _logger.ILog("Registering node...");
            _node = null;
            var result = await _connection.InvokeAsync<NodeRegisterResult>("RegisterNode", 
                _hostname, _configurationService.CurrentConfig?.Revision ?? 0);
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
                _logger.ILog($"Sending node status from node '{_node?.Name ?? Environment.MachineName}'...");
                if (_connection.State == HubConnectionState.Connected && _node != null)
                {
                    _logger.DLog("Sending node status...");
                    await _connection.SendAsync("UpdateNodeStatus", new
                    {
                        NodeUid = _nodeUid,
                        ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                        MaxRunners = _node.FlowRunners,
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
        if (_isRegistered)
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
        if (!_isRegistered)
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
}
