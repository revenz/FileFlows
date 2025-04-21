using System.Text.Json;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.NodeClient;

/// <summary>
/// SignalR Node Client for connecting to the server.
/// </summary>
public class ClientConnection : IDisposable
{
    private readonly object _lock = new();
    private readonly ILogger _logger;
    private CancellationTokenSource _cts;
    
    internal readonly HubConnection _connection;
    private bool _disposed;


    private SemaphoreSlim _registerLock = new(1, 1);
    /// <summary>
    /// Gets if the client is registered
    /// </summary>
    public bool IsRegistered { get; private set; }
    
    /// <summary>
    /// Gets the configuration revision on the server
    /// </summary>
    public int ServerConfigRevision { get; private set; }
    
    /// <summary>
    /// Gets the server version
    /// </summary>
    public string? ServerVersion { get; private set; }

    /// <summary>
    /// If its started or not
    /// </summary>
    private bool _started = false;
    
    /// <summary>
    /// Gets the connected node
    /// </summary>
    public ProcessingNode? Node { get; private set; }

    /// <summary>
    /// The function that gets the parameters used for registering
    /// </summary>
    private readonly Func<NodeRegisterParameters> _getRegisterParameters;
    
    
    
    /// <summary>
    /// Gets or sets the file check handler
    /// </summary>
    public Func<RunFileArguments, Task<FileCheckResult>>? FileCheckHandler { get; set; }
    /// <summary>
    /// Gets or set sthe abort file handler
    /// </summary>
    public Func<Guid, Task<bool>>? AbortFileHandler { get; set; }
    /// <summary>
    /// Event raised when the configuration is updated
    /// </summary>
    public event Action<int>? ConfigurationUpdated;
    /// <summary>
    /// Event raised when the connection is established and the node is registered
    /// </summary>
    public event Action? Connected;
    /// <summary>
    /// Event raised when the connection state changes
    /// </summary>
    public event Action<ConnectionState>? ConnectedUpdated;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="url">the URL of the server</param>
    /// <param name="accessToken">the access token used to connect</param>
    /// <param name="getRegisterParameters">the method to get the registration parameters</param>
    public ClientConnection(ILogger logger, string url, string? accessToken, Func<NodeRegisterParameters> getRegisterParameters)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var retryPolicyLoop = new RetryPolicyLoop(logger);
        _cts = new CancellationTokenSource();
        _getRegisterParameters = getRegisterParameters;

        url = url.TrimEnd('/');
        
        _connection = new HubConnectionBuilder()
            .WithUrl($"{url}/node", options =>
            {
                if (string.IsNullOrWhiteSpace(accessToken) == false)
                    options.Headers.Add("Authorization", accessToken);
            })
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithAutomaticReconnect(retryPolicyLoop)
            .Build();

        _connection.On<RunFileArguments, FileCheckResult>("ClientProcessFile",
            async (args) =>
            {
                if (FileCheckHandler != null)
                    return await FileCheckHandler.Invoke(args);
                return FileCheckResult.CannotProcess;
            });

        _connection.On<Guid, bool>("AbortFile", async (uid) =>
        {
            if(AbortFileHandler != null)
                return await AbortFileHandler.Invoke(uid);
            return false;
        });

        _connection.On<ProcessingNode>("NodeUpdated", node => Node = node);
        _connection.On<int>("ConfigUpdated", rev => ConfigurationUpdated?.Invoke(rev));
        
        _connection.Reconnecting += error =>
        {
            ConnectedUpdated?.Invoke(ConnectionState.Connecting);
            WLog("Connection lost, attempting to reconnect...");
            IsRegistered = false;
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            ConnectedUpdated?.Invoke(ConnectionState.Connected);
            ILog("Reconnected, registering node...");
            IsRegistered = false;
            retryPolicyLoop.ResetBackoff();
            
        };

        _connection.Closed += error =>
        {
            ConnectedUpdated?.Invoke(ConnectionState.Disconnected);
            WLog("Connection closed.");
            IsRegistered = false;
            retryPolicyLoop.ResetBackoff();
            return Task.CompletedTask;
        };
    }


    /// <summary>
    /// Attempts to start the connection and retry if it fails initially.
    /// </summary>
    public void Start()
    {
        if (_started)
            return; // already started

        _started = true;
        ILog("Starting connection...");
        
        _ = Task.Run(EnsureConnected, _cts.Token);
    }

    /// <summary>
    /// Connects, registers and keeps the node connected and registered
    /// </summary>
    private async Task EnsureConnected()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    ILog("Attempting to start connection...");
                    await _connection.StartAsync(_cts.Token);
                    ILog("Connection started.");
                }

                if (_connection.State != HubConnectionState.Connected)
                {
                    WLog("Connection did not establish. Retrying in 5 seconds...");
                    await Task.Delay(5000, _cts.Token);
                    continue;
                }

                if (IsRegistered)
                {
                    ILog("Connected and registered.");
                    await Task.Delay(5000, _cts.Token);
                    continue;
                }

                // Always try to register the node after a successful connection.
                // This will ensure the node is re-registered after reconnection as well.
                if (await RegisterNodeAsync() == false)
                {
                    WLog("Node registration failed or not registered yet. Retrying in 5 seconds...");
                    await Task.Delay(5000, _cts.Token);
                    continue;
                }

                // If successfully connected and registered, invoke the connected event and return.
                _ = Task.Run(() => ConnectedUpdated?.Invoke(ConnectionState.Connected));
                _ = Task.Run(() => Connected?.Invoke());
                ILog("Node connected and registered.");
            }
            catch (OperationCanceledException)
            {
                // expected on Stop()
                WLog("EnsureConnected: Connection was canceled.");
                return;
            }
            catch (Exception ex)
            {
                if (ex is InvalidDataException && ex.InnerException is JsonException jsonEx && jsonEx.Message.Contains("'<'" ))
                {
                    WLog("Connection failed: Server returned invalid data (likely HTML instead of JSON). " +
                                 "Check if the Hub URL is correct and accessible. Retrying in 5 seconds...");
                }
                else
                {
                    WLog($"Connection failed: {ex.Message}. Retrying in 5 seconds...");
                }
                await Task.Delay(5000, _cts.Token);
            }
        }
    }


    /// <summary>
    /// Registers the node with the server
    /// </summary>
    private async Task<bool> RegisterNodeAsync()
    {
        if (await _registerLock.WaitAsync(TimeSpan.FromSeconds(20)) == false)
        {
            WLog("Failed to wait for semaphore to register node.");
            return false;
        }
        try
        {
            ILog("Registering node...");
            Node = null;

            if (_connection.State == HubConnectionState.Disconnected)
            {
                ILog("Connection is disconnected, cannot register");
                return false;
            }

            var parameters = _getRegisterParameters();

            ILog("About to call RegisterNode on server: " + JsonSerializer.Serialize(parameters));
            ILog("_connection: " + (_connection == null ? "is null" : "is not null"));
            var result = await _connection!.InvokeAsync<NodeRegisterResult>("RegisterNode", parameters);
            ILog("RegisterNode result: " + result.Success);

            if (result.Success == false)
            {
                WLog("Failed to register node");
                return false;
            }

            Node = result.Node;
            ILog("Node successfully registered.");

            ServerConfigRevision = result.CurrentConfigRevision;
            ServerVersion = result.ServerVersion;
            IsRegistered = true;

            return true;
        }
        catch (Exception ex)
        {
            ELog($"Node registration failed: {ex}");
            return false;
        }
        finally
        {
            _registerLock.Release();
        }
    }
    

    /// <summary>
    /// Stops the connection and unregisters the node.
    /// </summary>
    public async Task StopAsync()
    {
        ILog("Closing connection");
        if (_disposed)
            return;

        await _cts.CancelAsync(); 
        
        try
        {
            if (IsRegistered && Node != null) 
            {
                ILog("Unregistering node...");
                await _connection.InvokeAsync("UnregisterNode", Node.Uid);
            }
        }
        catch (Exception ex)
        {
            WLog($"Failed to unregister node: {ex.Message}");
        }

        try
        {
            ILog("Stopping connection...");
            await _connection.StopAsync();
        }
        catch (Exception ex)
        {
            WLog($"Failed to stop connection: {ex.Message}");
        }
    }


    /// <summary>
    /// Disposes the client, ensuring all resources are properly cleaned up.
    /// </summary>
    public void Dispose()
    {
        ILog("Disposing of connection");
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;
            
            _disposed = true;
            ILog("Disposing client...");

            try
            {
                _cts.Cancel();
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                WLog($"Exception during disposal: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
                _connection.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Awaits the connection 
    /// </summary>
    /// <param name="timeoutInSeconds">time in seconds to wait for connection</param>
    /// <returns>true if connected, otherwise false if timed out</returns>
    public async Task<bool> AwaitConnection(int timeoutInSeconds = 60)
    {
        if (_disposed)
        {
            ILog("Cannot await connection because the connection has been disposed.");
            return false;
        }

        var end = DateTime.Now.AddSeconds(timeoutInSeconds);
        bool delayed = false;

        while (DateTime.Now < end || delayed == false)
        {
            if(delayed)
                await Task.Delay(250);
            
            if (_connection.State == HubConnectionState.Connected && IsRegistered)
            {
                if(delayed)
                    ILog("Await Connection is connected");
                return true;
            }

            delayed = true;
        }

        WLog("Failed to await connection");
        return false;

    }
    
    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments.</param>
    public async Task InvokeAsync(string methodName, params object?[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            if (_disposed)
                return;

            if (await AwaitConnection(30) == false)
                continue;
            try
            {            
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                switch (args.Length)
                {
                    case 0: await _connection.InvokeAsync(methodName, cancellationToken: cts.Token); break;
                    case 1: await _connection.InvokeAsync(methodName, args[0], cancellationToken: cts.Token); break;
                    case 2: await _connection.InvokeAsync(methodName, args[0], args[1], cancellationToken: cts.Token); break;
                    case 3:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], cancellationToken: cts.Token); break;
                    case 4:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3], cancellationToken: cts.Token);
                        break;
                    case 5:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], cancellationToken: cts.Token); break;
                    case 6:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], cancellationToken: cts.Token); break;
                    case 7:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], cancellationToken: cts.Token); break;
                    case 8:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], cancellationToken: cts.Token); break;
                    case 9:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], args[8], cancellationToken: cts.Token); break;
                    case 10:
                        await _connection.InvokeAsync(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], args[8], args[9], cancellationToken: cts.Token); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(args), "Too many arguments provided.");
                }
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                if (_connection.State == HubConnectionState.Connected)
                    throw;
            }
        }

        ELog($"Failed to invoke method '{methodName}' on server as no connection could be established.");
        throw new Exception($"Failed to invoke method '{methodName}' on server as no connection established.");
    }
    
    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the server method.</typeparam>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <typeparamref name="TResult"/> for the hub method return value.
    /// </returns>
    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object?[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            if (_disposed)
                return default!;

            if (await AwaitConnection(30) == false)
                continue;
            try
            {
                TResult result;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                switch (args.Length)
                {
                    case 0: result = await _connection.InvokeAsync<TResult>(methodName, cancellationToken: cts.Token); break;
                    case 1: result = await _connection.InvokeAsync<TResult>(methodName, args[0], cancellationToken: cts.Token); break;
                    case 2: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], cancellationToken: cts.Token); break;
                    case 3:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], cancellationToken: cts.Token); break;
                    case 4:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], cancellationToken: cts.Token);
                        break;
                    case 5:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], cancellationToken: cts.Token); break;
                    case 6:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], cancellationToken: cts.Token); break;
                    case 7:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], cancellationToken: cts.Token); break;
                    case 8:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], cancellationToken: cts.Token); break;
                    case 9:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], args[8], cancellationToken: cts.Token); break;
                    case 10:
                        result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3],
                            args[4], args[5], args[6], args[7], args[8], args[9], cancellationToken: cts.Token); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(args), "Too many arguments provided.");
                }

                return result;
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                if (_connection.State == HubConnectionState.Connected)
                    throw;
            }
        }

        ELog($"Failed to invoke method '{methodName}' on server as no connection could be established.");
        throw new Exception($"Failed to invoke method '{methodName}' on server as no connection established.");
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public async Task SendAsync(string methodName, params object?[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            if (_disposed)
                return;

            if (await AwaitConnection(30) == false)
                continue;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                switch (args.Length)
                {
                    case 0: await _connection.SendAsync(methodName, cancellationToken: cts.Token); break;
                    case 1: await _connection.SendAsync(methodName, args[0], cancellationToken: cts.Token); break;
                    case 2: await _connection.SendAsync(methodName, args[0], args[1], cancellationToken: cts.Token); break;
                    case 3: await _connection.SendAsync(methodName, args[0], args[1], args[2], cancellationToken: cts.Token); break;
                    case 4: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], cancellationToken: cts.Token); break;
                    case 5: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], cancellationToken: cts.Token); break;
                    case 6:
                        await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4],
                            args[5], cancellationToken: cts.Token); break;
                    case 7:
                        await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5],
                            args[6], cancellationToken: cts.Token); break;
                    case 8:
                        await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5],
                            args[6], args[7], cancellationToken: cts.Token); break;
                    case 9:
                        await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5],
                            args[6], args[7], args[8], cancellationToken: cts.Token); break;
                    case 10:
                        await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5],
                            args[6], args[7], args[8], args[9], cancellationToken: cts.Token); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(args), "Too many arguments provided.");
                }

                return;
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                if (_connection.State == HubConnectionState.Connected)
                    throw;
            }
        }

        ELog($"Failed to sending method '{methodName}' on server as no connection could be established.");
        throw new Exception($"Failed to sending method '{methodName}' on server as no connection established.");
    }
    
    
    /// <summary>
    /// Information log
    /// </summary>
    /// <param name="message">the message</param>
    private void ILog(string message)
    {
        message = "ClientConnection: " + message;
        //Console.WriteLine(message);
        _logger.ILog(message);
    }

    /// <summary>
    /// Warning log
    /// </summary>
    /// <param name="message">the message</param>
    private void WLog(string message)
    {
        message = "ClientConnection: " + message;
        //Console.WriteLine(message);
        _logger.WLog(message);
    }
    
    /// <summary>
    /// Error log
    /// </summary>
    /// <param name="message">the message</param>
    private void ELog(string message)
    {
        message = "ClientConnection: " + message;
        //Console.WriteLine(message);
        _logger.ELog(message);
    }
}
