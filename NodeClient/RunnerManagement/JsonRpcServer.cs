using System.IO.Pipes;
using System.Text.Json;
using FileFlows.NodeClient.Handlers;
using FileFlows.ServerShared.Models;
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
    internal FlowExecutorInfo _flowExecutorInfo;

    private readonly RpcRegister _rpcRegister = new();

    private readonly LibraryFileHandler _libraryFileHandler;
    private readonly RunnerInfoHandler _runnerInfoHandler;
    private readonly BasicHandler _basicHandler;

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
        _client = client;
        this.cts = new CancellationTokenSource();
        this.runnerParameters = runnerParameters;
        _libraryFileHandler = new(this, _rpcRegister);
        _basicHandler = new(this, _rpcRegister);
        _runnerInfoHandler = new(client.Manager, this, _rpcRegister);

        _logMessage = logMessage;
        this._libraryFile = runnerParameters.LibraryFile;
        _flowExecutorInfo = new()
        {
            Uid = runnerParameters.LibraryFile.Uid,
            LibraryFileName = _libraryFile.Name,
            RelativeFile = _libraryFile.RelativePath,
            Library = _libraryFile.Library,
            InitialSize = _libraryFile.OriginalSize
        };
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
                Console.WriteLine($"Starting RPC Server {this.PipeName}");

                try
                {
                    server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
                    Console.WriteLine("Waiting for child process...");

                    await server.WaitForConnectionAsync(cts.Token);
                    Console.WriteLine("Child connected.");

                    using var reader = new StreamReader(server);
                    using var writer = new StreamWriter(server) { AutoFlush = true };

                    while (server.IsConnected && !cts.Token.IsCancellationRequested)
                    {
                        if (cts.Token.IsCancellationRequested)
                            break; // Ensure exit when stopping

                        var requestJson = await reader.ReadLineAsync(cts.Token);
                        if (requestJson == null) break;

                        _ = Task.Run(async () =>
                        {
                            var responseJson = await _rpcRegister.HandleRequest(requestJson);
                            if (responseJson != null)
                                await SendMessageToClient(writer, responseJson);
                        }, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Server task canceled.");
                    break; // Ensure exit
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server exception: " + ex);
                }
                finally
                {
                    try
                    {
                        server?.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        Console.WriteLine("Error disposing server: " + disposeEx);
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

        Console.WriteLine("Stopping server...");
        cts.Cancel();
    }

    /// <summary>
    /// Disposes of the server resources, stopping the server task if necessary.
    /// </summary>
    public void Dispose()
    {
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

        _ = Task.Run(async () =>
        {
            if (server?.IsConnected == true)
            {
                using var writer = new StreamWriter(server) { AutoFlush = true };
                await SendMessageToClient(writer, jsonMessage);
            }
        });
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
        try
        {
            if (server?.IsConnected == true)
            {
                if(message.Contains("Unknown method", StringComparison.CurrentCultureIgnoreCase) == false)
                    await writer.WriteLineAsync(message);
                Console.WriteLine("JSON RPC Message sent to client: " + message);
            }
            else
            {
                Console.WriteLine("Server is not connected, unable to send message.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending message: " + ex);
        }
        finally
        {
            writeLock.Release(); // Release semaphore lock
        }
    }
}
