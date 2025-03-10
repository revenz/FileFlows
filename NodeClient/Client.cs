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
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public partial class Client : IDisposable
{
    private readonly object _lock = new(); 
    private readonly RetryPolicyLoop _retryPolicyLoop; 
    private readonly ConfigurationService _configurationService;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts;
    
    private TaskCompletionSource<bool> _registrationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HubConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Connection updated event
    /// </summary>
    public delegate void ConnectionUpdated(ConnectionState state);
    
    /// <summary>
    /// Event fired when connection state changes
    /// </summary>
    public event ConnectionUpdated OnConnectionUpdated;

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
        _hostname = parameters.Hostname;
        _runnerManager = ServiceLoader.Load<RunnerManager>();
        _retryPolicyLoop = new RetryPolicyLoop(logger);
        _cts = new CancellationTokenSource();

        parameters.ServerUrl = parameters.ServerUrl.Replace("http:", "ws:").Replace("https:", "wss:").TrimEnd('/');

        _connection = new HubConnectionBuilder()
            .WithUrl($"{parameters.ServerUrl}/node", options =>
            {
                if (string.IsNullOrWhiteSpace(parameters.AccessToken) == false)
                    options.Headers.Add("Authorization", parameters.AccessToken);
            })
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect(_retryPolicyLoop)
            .Build();

        _connection.On<RunFileArguments, bool>("ClientProcessFile", HandleClientProcessFile);
        _connection.On<ProcessingNode>("NodeUpdated", UpdateNode);
        _connection.On<ConfigurationRevision>("ConfigUpdated", UpdateConfiguration);

        _connection.Reconnecting += error =>
        {
            _logger.WLog("Connection lost, attempting to reconnect...");
            OnConnectionUpdated?.Invoke(ConnectionState.Connecting);
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            _logger.ILog("Reconnected, registering node...");
            OnConnectionUpdated?.Invoke(ConnectionState.Connected);
            await RegisterNodeAsync();
            _retryPolicyLoop.ResetBackoff();
        };

        _connection.Closed += error =>
        {
            _logger.WLog("Connection closed.");
            OnConnectionUpdated?.Invoke(ConnectionState.Disconnected);
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _retryPolicyLoop.ResetBackoff();
            return Task.CompletedTask;
        };

        _ = TryStartConnectionAsync();
    }

    /// <summary>
    /// Attempts to start the connection and retry if it fails initially.
    /// </summary>
    private async Task TryStartConnectionAsync()
    {
        OnConnectionUpdated?.Invoke(ConnectionState.Connecting);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                lock (_lock)
                {
                    if (_connection.State != HubConnectionState.Disconnected)
                    {
                        _logger.WLog($"Connection is in state {_connection.State}, skipping StartAsync.");
                        return;
                    }
                }

                _logger.ILog("Attempting to start connection...");
                await _connection.StartAsync(_cts.Token);
                OnConnectionUpdated?.Invoke(ConnectionState.Connected);
                await RegisterNodeAsync();
                _ = Task.Run(SendNodeStatusAsync, _cts.Token);
                return; // Exit loop on successful connection
            }
            catch (Exception ex)
            {
                _logger.WLog($"Connection failed: {ex.Message}. Retrying in 5 seconds...");
                await Task.Delay(5000, _cts.Token);
            }
        }
    }

    /// <summary>
    /// Starts the connection and attempts to register the node once connected.
    /// </summary>
    public async Task StartAsync()
    {
        _logger.ILog($"Starting client... (Connection {_connection.State})");
        try
        {
            if(_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync(_cts.Token);
            _logger.ILog($"Starting client 2... (Connection {_connection.State})");
            if (_connection.State == HubConnectionState.Connected)
            {
                OnConnectionUpdated?.Invoke(ConnectionState.Connected);
                await RegisterNodeAsync();
            }

            _ = Task.Run(SendNodeStatusAsync, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.WLog($"Failed to start connection: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the connection and unregisters the node.
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (IsRegistered)
            {
                _logger.ILog("Unregistering node...");
                await _connection.InvokeAsync("UnregisterNode", _nodeUid);
            }
        }
        catch (Exception ex)
        {
            _logger.WLog($"Failed to unregister node: {ex.Message}");
        }

        try
        {
            _logger.ILog("Stopping connection...");
            await _connection.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.WLog($"Failed to stop connection: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures the node is registered before proceeding.
    /// </summary>
    private async Task EnsureRegisteredAsync()
    {
        if (!IsRegistered)
        {
            _logger.ILog("Waiting for registration...");
            await _registrationCompletion.Task;
        }
    }

    /// <summary>
    /// Disposes the client, ensuring all resources are properly cleaned up.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;
            
            _disposed = true;
            _logger.ILog("Disposing client...");

            try
            {
                _cts.Cancel();
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.WLog($"Exception during disposal: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
                _connection.DisposeAsync().GetAwaiter().GetResult();
            }

            // Unsubscribe event handlers to prevent memory leaks
            if (OnConnectionUpdated != null)
            {
                foreach (var listener in OnConnectionUpdated.GetInvocationList())
                {
                    OnConnectionUpdated -= (ConnectionUpdated)listener;
                }
            }
        }
    }
}
