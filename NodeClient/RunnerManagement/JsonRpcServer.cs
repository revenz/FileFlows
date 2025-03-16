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
    
    #if(DEBUG)
    internal Action<string> _logMessage;
    #else
    private Action<string> _logMessage;
    #endif
    
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
        this.runnerParameters = runnerParameters;
        _libraryFileHandler = new(this, _rpcRegister);
        _basicHandler = new(this, _rpcRegister);
        _runnerInfoHandler = new(this, _rpcRegister);
        
        _logMessage = logMessage;
        _flowExecutorInfo = new()
        {
            Uid = runnerParameters.LibraryFile.Uid
        };
        this._libraryFile = runnerParameters.LibraryFile;
        this.PipeName = "runner-" + _libraryFile.Uid;
        this.cts = new CancellationTokenSource();
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
                    // Create the NamedPipeServerStream and assign it to the class-level 'server' variable
                    server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
                    Console.WriteLine("Waiting for child process...");

                    await server.WaitForConnectionAsync(cts.Token);
                    Console.WriteLine("Child connected.");

                    using var reader = new StreamReader(server);
                    using var writer = new StreamWriter(server) { AutoFlush = true };

                    while (server.IsConnected && !cts.Token.IsCancellationRequested)
                    {
                        var requestJson = await reader.ReadLineAsync();
                        if (requestJson == null) break;

                        var responseJson = await _rpcRegister.HandleRequest(requestJson);
                        await writer.WriteLineAsync(responseJson);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server exception: " + ex);
                }
                finally
                {
                    try
                    {
                        // Ensure cleanup of server after connection closes
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
        cts.Dispose();
        cts = null;
    }

    /// <summary>
    /// Disposes of the server resources, stopping the server task if necessary.
    /// </summary>
    public void Dispose()
    {
        Stop();
        serverTask?.Wait();
        serverTask?.Dispose();
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
        // Create a message to signal abort (in this case, sending JSON with an "Abort" key)
        var abortMessage = new { action = "Abort" };
        string jsonMessage = JsonSerializer.Serialize(abortMessage);

        // Send the abort message to the connected client
        SendMessageToClient(jsonMessage);
    }

    /// <summary>
    /// Sends a message to the connected client through the named pipe.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    private async void SendMessageToClient(string message)
    {
        try
        {
            // Ensure the server is connected before writing to it
            if (server != null && server.IsConnected)
            {
                // Write the abort message to the pipe
                using (var writer = new StreamWriter(server) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(message);
                    Console.WriteLine("Abort message sent.");
                }
            }
            else
            {
                Console.WriteLine("Server is not connected, unable to send abort message.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending abort message: " + ex);
        }
    }
}
