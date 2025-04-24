using System.IO.Pipes;
using System.Text.Json;
using FileFlows.NodeClient.Handlers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.NodeClient;

/// <summary>
/// Represents a JSON-RPC server that communicates with a child process using named pipes.
/// </summary>
public class JsonRpcServer : IDisposable
{
    /// <summary>
    /// Gets the pipe name used for communication.
    /// </summary>
    public string PipeName { get; init; }

    internal readonly RunnerParameters runnerParameters;
    private CancellationTokenSource cts;
    private Task serverTask;
    private SemaphoreSlim writeLock = new(1, 1); // Semaphore for writing messages

    internal Action<string> _logMessage;

    internal LibraryFile _libraryFile;
    internal Client _client;

    private readonly RpcRegister _rpcRegister = new();

    private readonly LibraryFileHandler _libraryFileHandler;
    private readonly RunnerInfoHandler _runnerInfoHandler;
    private readonly BasicHandler _basicHandler;
    private readonly StatisticsHandler _statisticsHandler;
    private readonly CacheHandler _cacheHandler;
    private StreamWriter? writer;

    private readonly string LogPrefix;

    /// <summary>
    /// The server pipe used for communication with the client.
    /// </summary>
    private NamedPipeServerStream server;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcServer"/> class.
    /// </summary>
    /// <param name="client">The client associated with the server.</param>
    /// <param name="runnerParameters">The parameters for the runner.</param>
    /// <param name="logMessage">Action for logging messages.</param>
    public JsonRpcServer(Client client, RunnerParameters runnerParameters, Action<string> logMessage)
    {
        LogPrefix = $"[{runnerParameters.Uid}] JsonRpcClient: "; 
        _client = client;
        this.cts = new CancellationTokenSource();
        this.runnerParameters = runnerParameters;
        _libraryFileHandler = new(this, _rpcRegister);
        _basicHandler = new(this, _rpcRegister);
        _runnerInfoHandler = new(client.Manager, this, _rpcRegister);
        _statisticsHandler = new (this, _rpcRegister);
        _cacheHandler = new (this, _rpcRegister);

        _logMessage = logMessage;
        this._libraryFile = runnerParameters.LibraryFile;
        this.PipeName = "runner-" + _libraryFile.Uid;
    }

    /// <summary>
    /// Starts the JSON-RPC server to listen for connections from a child process.
    /// </summary>
    public void Start()
    {
        serverTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                Logger.Instance.ILog($"{LogPrefix}Starting RPC Server \"{PipeName}\"");

                try
                {
                    server = new NamedPipeServerStream(
                                PipeName,
                                PipeDirection.InOut,
                                1, // Max allowed server instances
                                PipeTransmissionMode.Byte,  // Use Byte for raw data
                                PipeOptions.Asynchronous,
                                1024 * 1024,  // 1MB in-buffer size
                                1024 * 1024   // 1MB out-buffer size
                            );
                    Logger.Instance.ILog($"{LogPrefix}Waiting for child process...");

                    await server.WaitForConnectionAsync(cts.Token);
                    Logger.Instance.ILog($"{LogPrefix}Child connected.");

                    using var reader = new StreamReader(server);
                    await using var writer = new StreamWriter(server);
                    this.writer = writer;
                    writer.AutoFlush = true;

                    while (server.IsConnected && !cts.Token.IsCancellationRequested)
                    {
                        if (cts.Token.IsCancellationRequested)
                            break; // Ensure exit when stopping

                        var requestJson = await reader.ReadLineAsync(cts.Token);
                        if (requestJson == null) break;
                        
                        if (requestJson == "Hello from client!")
                            continue;

                        _ = Task.Run(async () =>
                        {
                            var request = JsonSerializer.Deserialize<RpcRequest>(requestJson);
                            if (request == null)
                            {
                                Logger.Instance.ILog($"{LogPrefix}Failed to deserialize: " + requestJson);
                                return;
                            }

                            string? responseJson;
                            if (await _client.Connection.AwaitConnection() == false)
                                responseJson = JsonSerializer.Serialize(new { request.Id, Error = "Not connected to server." });
                            else
                                responseJson = await _rpcRegister.HandleRequest(request);
                            
                            if (responseJson != null && writer != null)
                                await SendMessageToClient(writer, responseJson);
                        }, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Instance.ILog($"{LogPrefix}Server task canceled.");
                    break; // Ensure exit
                }
                catch (Exception ex)
                {
                    Logger.Instance.ILog($"{LogPrefix}Server exception: " + ex);
                }
                finally
                {
                    try
                    {
                        server?.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Instance.ILog($"{LogPrefix}Error disposing server: " + disposeEx);
                    }
                }
            }
        }, cts.Token);

    }

    /// <summary>
    /// Stops the JSON-RPC server and cancels any ongoing tasks.
    /// </summary>
    public void Stop()
    {
        if (cts == null) return;

        Logger.Instance.ILog($"{LogPrefix}Stopping server...");
        cts.Cancel();
    }

    /// <summary>
    /// Disposes of the server resources, stopping the server task if necessary.
    /// </summary>
    public void Dispose()
    {
        Logger.Instance.ILog($"{LogPrefix}Disposing");
        Stop();
        serverTask?.Wait();
        serverTask?.Dispose();
        writeLock?.Dispose();
        cts.Dispose();
        cts = null;
    }

    /// <summary>
    /// Gets the final processed library file.
    /// </summary>
    /// <returns>The processed library file.</returns>
    internal LibraryFile GetProcessedFile()
        => _libraryFile;

    /// <summary>
    /// Sends an abort message to the connected client.
    /// </summary>
    public void Abort()
    {
        var abortMessage = new { action = "Abort" };
        string jsonMessage = JsonSerializer.Serialize(abortMessage);
        if (server?.IsConnected == true && writer != null)
            SendMessageToClient(writer, jsonMessage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends a message to the connected client through the named pipe.
    /// Uses a semaphore to prevent concurrent writes.
    /// </summary>
    /// <param name="writer">The StreamWriter instance.</param>
    /// <param name="message">The message to be sent.</param>
    private async Task SendMessageToClient(StreamWriter writer, string message)
    {
        await writeLock.WaitAsync(); // Acquire semaphore lock
        if(writer == null)
            return;
        try
        {
            if (server?.IsConnected == true)
            {
                if (message.Contains("Unknown method", StringComparison.CurrentCultureIgnoreCase) == false)
                {   
                    int byteSize = System.Text.Encoding.UTF8.GetByteCount(message);
                    await writer.WriteLineAsync(message);
                }
            }
            else
            {
                Logger.Instance.ILog($"{LogPrefix}Server is not connected, unable to send message.");
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ILog($"{LogPrefix}Error sending message: " + ex);
        }
        finally
        {
            writeLock.Release(); // Release semaphore lock
        }
    }
}
