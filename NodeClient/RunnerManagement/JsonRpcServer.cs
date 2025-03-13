using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using FileFlows.NodeClient.Handlers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Scriban.Parsing;

namespace FileFlows.NodeClient;

public class JsonRpcServer : IDisposable
{
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
    
    
    private readonly Dictionary<string, Func<object[], Task<object?>>> _handlers = new();
    
    
    public void Start()
    {
        serverTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"Starting RPC Server {this.PipeName}");
                NamedPipeServerStream? server = null; // Declare before try block

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
                    if (server != null)
                    {
                        try
                        {
                            server.Dispose(); // Ensure cleanup
                        }
                        catch (Exception disposeEx)
                        {
                            Console.WriteLine("Error disposing server: " + disposeEx);
                        }
                    }
                }
            }
        }, cts.Token);
    }


    public void Stop()
    {
        if (cts == null) return;

        Console.WriteLine("Stopping server...");
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    public void Dispose()
    {
        Stop();
        serverTask?.Wait();
        serverTask?.Dispose();
    }
    
    /// <summary>
    /// Gets the final processed file 
    /// </summary>
    /// <returns>the final processed file </returns>
    internal LibraryFile GetProcessedFile()
        => _libraryFile;
}