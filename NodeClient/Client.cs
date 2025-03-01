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
using UglyToad.PdfPig.Graphics;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public partial class Client : IDisposable
{
    private HubConnection _connection;
    private readonly RetryPolicyLoop _retryPolicyLoop; 
    private readonly ConfigurationService _configurationService;
    private readonly ILogger _logger;
    private TaskCompletionSource<bool> _registrationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _disposed;
    private CancellationTokenSource _cts;  // Cancellation token source for graceful shutdown

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
        _parameters = parameters;
        _configurationService = ServiceLoader.Load<ConfigurationService>();
        _hostname = parameters.Hostname;
        _runnerManager = new();
        _logger = logger;
        _retryPolicyLoop = new RetryPolicyLoop(logger);  // Initialize retry policy
        _cts = new CancellationTokenSource();  // Initialize the cancellation token source

        parameters.ServerUrl = parameters.ServerUrl.Replace("http:", "ws:").Replace("https:", "wss:").TrimEnd('/');

        _connection = new HubConnectionBuilder()
            .WithUrl($"{parameters.ServerUrl}/node", options =>
            {
                if (string.IsNullOrWhiteSpace(parameters.AccessToken) == false)
                    options.Headers.Add("Authorization", parameters.AccessToken);
            })
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect(_retryPolicyLoop) // Custom retry policy
            .Build();

        _connection.On<RunFileArguments, Task<bool>>("ClientProcessFile", HandleClientProcessFile);
        _connection.On<ProcessingNode>("NodeUpdated", UpdateNode);
        _connection.On<ConfigurationRevision>("ConfigUpdated", UpdateConfiguration);

        _connection.Reconnecting += error =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Connecting);
            _logger.WLog("Connection lost, attempting to reconnect...");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Connected);
            _logger.ILog("Reconnected, registering node...");
            await RegisterNodeAsync();
            _retryPolicyLoop.ResetBackoff();  // Reset backoff after successful reconnection
        };

        _connection.Closed += error =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Disconnected);
            _logger.WLog("Connection closed.");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _retryPolicyLoop.ResetBackoff();  // Reset backoff on closure if needed
            return Task.CompletedTask;
        };

        // Try to start the connection immediately on construction, and handle the initial failure
        TryStartConnectionAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to start the connection and retry if the connection fails initially.
    /// </summary>
    private async Task TryStartConnectionAsync()
    {
        OnConnectionUpdated?.Invoke(ConnectionState.Connecting);
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                // The connection is in a disconnected state, retrying
                _logger.ILog("Attempting to start connection...");
                await _connection.StartAsync(_cts.Token);  // Pass cancellation token here
                OnConnectionUpdated?.Invoke(ConnectionState.Connected);
                await RegisterNodeAsync();
                _ = Task.Run(SendNodeStatusAsync); // Start sending node status once connected
            }
        }
        catch (ObjectDisposedException)
        {
            // If the connection has been disposed, handle re-creating the connection
            _logger.WLog("Connection object disposed. Recreating the connection...");
            CreateNewConnection();
            await TryStartConnectionAsync(); // Retry after re-creating the connection
        }
        catch (OperationCanceledException)
        {
            _logger.WLog("Connection attempt was canceled.");
        }
        catch (Exception ex)
        {
            _logger.WLog($"Connection failed to start: {ex.Message}. Retrying...");
            await Task.Delay(5000);  // Wait for a few seconds before retrying
            await TryStartConnectionAsync();  // Retry connecting
        }
    }

    private void CreateNewConnection()
    {
        _logger.ILog("Creating a new HubConnection...");

        // Recreate the connection object
        _connection.DisposeAsync().GetAwaiter().GetResult();

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_parameters.ServerUrl}/node", options =>
            {
                if (string.IsNullOrWhiteSpace(_parameters.AccessToken) == false)
                    options.Headers.Add("Authorization", _parameters.AccessToken);
            })
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect(_retryPolicyLoop) // Custom retry policy
            .Build();

        _connection.On<RunFileArguments, Task<bool>>("ClientProcessFile", HandleClientProcessFile);
        _connection.On<ProcessingNode>("NodeUpdated", UpdateNode);
        _connection.On<ConfigurationRevision>("ConfigUpdated", UpdateConfiguration);

        _connection.Reconnecting += error =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Connecting);
            _logger.WLog("Connection lost, attempting to reconnect...");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Connected);
            _logger.ILog("Reconnected, registering node...");
            await RegisterNodeAsync();
        };

        _connection.Closed += error =>
        {
            OnConnectionUpdated?.Invoke(ConnectionState.Disconnected);
            _logger.WLog("Connection closed.");
            _registrationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Starts the connection and attempts to register the node once connected.
    /// </summary>
    public async Task StartAsync()
    {
        _logger.ILog("Starting client...");
        await _connection.StartAsync(_cts.Token);  // Pass cancellation token here
        OnConnectionUpdated?.Invoke(ConnectionState.Connected);
        await RegisterNodeAsync();
        _ = Task.Run(SendNodeStatusAsync);
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

        await _connection.StopAsync();  // Stop connection asynchronously
    }

    private async Task EnsureRegisteredAsync()
    {
        if (!IsRegistered)
        {
            _logger.ILog("Waiting for registration...");
            await _registrationCompletion.Task;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Unsubscribe all listeners from the event
        if (OnConnectionUpdated != null)
        {
            foreach (var listener in OnConnectionUpdated.GetInvocationList())
            {
                OnConnectionUpdated -= (ConnectionUpdated)listener;
            }
        }
        _disposed = true;
        _logger.ILog("Disposing client...");

        // Cancel any ongoing connection tasks if disposing
        _cts.Cancel();
        StopAsync().GetAwaiter().GetResult();
        _connection.DisposeAsync().GetAwaiter().GetResult();
    }
}
