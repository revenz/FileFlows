using System.IO.Pipes;
using FileFlows.FlowRunner.JsonRpc.Handlers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc;

/// <summary>
/// A JSON-RPC client that communicates with a named pipe server.
/// </summary>
public class JsonRpcClient
{
    private NamedPipeClientStream client;
    private StreamReader reader;
    private StreamWriter writer;
    private CancellationTokenSource cts = new();
    private Task listeningTask;
    public RunnerParameters Parameters { get; private set; }
    
    public BasicHandler BasicHandler {get;private set;}
    public LibraryFileHandler LibraryFileHandler {get;private set;}
    public RunnerInfoHandler RunnerInfoHandler {get;private set;}
    
    /// <summary>
    /// Gets or sets the library file being processed
    /// </summary>
    public LibraryFile LibraryFile { get; internal set; }
    
    /// <summary>
    /// Gets or sets the processing node this is executing on
    /// </summary>
    public ProcessingNode? Node { get; internal set; }

    /// <summary>
    /// Initializes the JSON-RPC client and connects to the specified named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to connect to.</param>
    public async Task<bool> Initialize(string pipeName)
    {
        try
        {
            client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            await client.ConnectAsync(10_000);

            reader = new StreamReader(client);
            writer = new StreamWriter(client) { AutoFlush = true };

            // Start listening for incoming server messages
            listeningTask = Task.Run(ListenForServerMessages, cts.Token);

            BasicHandler = new(this);
            Parameters = await BasicHandler.GetRunnerParameters();
            LibraryFileHandler = new(this);
            RunnerInfoHandler = new(this, Parameters.MaxFlowParts);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed connection to JSON RPC Client: " + ex);
            return false;
        }
    }

    /// <summary>
    /// Continuously listens for messages from the server.
    /// </summary>
    private async Task ListenForServerMessages()
    {
        try
        {
            while (!cts.Token.IsCancellationRequested && client.IsConnected)
            {
                var message = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                var rpcMessage = JsonSerializer.Deserialize<RpcMessage>(message);
                if (rpcMessage?.Method == "Abort")
                {
                    Logger.Instance.ILog("Received Abort request from server.");
                    HandleAbort();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error in server message listener: " + ex);
        }
    }

    /// <summary>
    /// Handles the "Abort" request from the server.
    /// </summary>
    private void HandleAbort()
    {
        Logger.Instance.WLog("Aborting client operations as requested by server.");
        cts.Cancel(); // Cancel ongoing tasks
        client?.Dispose(); // Close the connection
    }

    
    /// <summary>
    /// Sends a request to the server without expecting a response.
    /// </summary>
    internal async Task SendRequest(string method, params object[] parameters)
    {
        var request = JsonSerializer.Serialize(new { Method = method, Params = parameters });
        await writer.WriteLineAsync(request);
    }

    /// <summary>
    /// Sends a request to the server and waits for a response.
    /// </summary>
    internal async Task<T> SendRequest<T>(string method, params object[] parameters)
    {
        var request = JsonSerializer.Serialize(new { Method = method, Params = parameters });
        await writer.WriteLineAsync(request);

        var responseJson = await reader.ReadLineAsync();
        var response = JsonSerializer.Deserialize<RpcResponse<T>>(responseJson);
        return response.Result;
    }
}

/// <summary>
/// Represents a generic RPC message.
/// </summary>
class RpcMessage
{
    public string Method { get; set; }
}

/// <summary>
/// Represents a response from the RPC server.
/// </summary>
/// <typeparam name="T">The type of the result contained in the response.</typeparam>
class RpcResponse<T>
{
    /// <summary>
    /// Gets or sets the result of the RPC call.
    /// </summary>
    public T Result { get; set; }
}