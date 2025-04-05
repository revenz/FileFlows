using System.IO.Pipes;
using FileFlows.FlowRunner.JsonRpc.Handlers;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc;

/// <summary>
/// A JSON-RPC client that communicates with a named pipe server.
/// </summary>
public class JsonRpcClient : IDisposable
{
    private NamedPipeClientStream client;
    private StreamReader reader;
    private StreamWriter writer;
    private CancellationTokenSource cts = new();
    private Task listeningTask;
    private SemaphoreSlim streamLock = new (1, 1);

    // A dictionary to store TaskCompletionSource for each request
    private readonly Dictionary<int, TaskCompletionSource<string>> responseTasks = new();
    private int currentRequestId = 0;  // To generate unique IDs for each request

    /// <summary>
    /// Gets the runner parameters
    /// </summary>
    public RunnerParameters Parameters { get; private set; }
    /// <summary>
    /// Gets the basic/generel handler 
    /// </summary>
    public BasicHandler Basic { get; private set; }
    /// <summary>
    /// Gets the handler for library files
    /// </summary>
    public LibraryFileHandler LibraryFileHandler { get; private set; }
    /// <summary>
    /// Gets the handler for the runner info
    /// </summary>
    public RunnerInfoHandler RunnerInfo { get; private set; }
    /// <summary>
    /// Gets the handler for statistics
    /// </summary>
    public StatisticsHandler Statistics { get; private set; }
    /// <summary>
    /// Gets the handler for Cache
    /// </summary>
    public CacheHandler Cache { get; private set; }

    public LibraryFile LibraryFile { get; internal set; }
    public ProcessingNode? Node { get; internal set; }

    private bool _disposed = false;

    public async Task<bool> Initialize(string pipeName)
    {
        try
        {
            Program.Log("JsonRpcClient.Initialize[0]");
            client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            Program.Log("JsonRpcClient.Initialize[1]");
            await client.ConnectAsync(10_000);
            Program.Log("JsonRpcClient.Initialize[2]");

            reader = new StreamReader(client);
            Program.Log("JsonRpcClient.Initialize[3]");
            writer = new StreamWriter(client) { AutoFlush = true };
            Program.Log("JsonRpcClient.Initialize[4]");

            // Start listening for incoming server messages
            listeningTask = Task.Run(ListenForServerMessages, cts.Token);
            
            Program.Log("JsonRpcClient.Initialize[5]");

            Basic = new(this);
            
            Program.Log("JsonRpcClient.Initialize[6]");
            Parameters = await Basic.GetRunnerParameters();
            Program.Log("JsonRpcClient.Initialize[7]");
            LibraryFileHandler = new(this);
            Program.Log("JsonRpcClient.Initialize[8]");
            RunnerInfo = new(this, Parameters.MaxFlowParts);
            Program.Log("JsonRpcClient.Initialize[8]");
            Statistics = new(this);
            Program.Log("JsonRpcClient.Initialize[10]");
            Cache = new(this);
            Program.Log("JsonRpcClient.Initialize[11]");

            return true;
        }
        catch (Exception ex)
        {
            Program.Log($"JsonRpcClient.Initialize: Error: {ex}");
            Logger.Instance.ELog("Failed connection to JSON RPC Client: " + ex);
            return false;
        }
    }

    private async Task ListenForServerMessages()
    {
        try
        {
            while (_disposed == false && !cts.Token.IsCancellationRequested && client.IsConnected)
            {
                var message = await reader.ReadLineAsync(cts.Token);
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                _ = Task.Run(() =>
                {
                    try
                    {
                        Program.Log("Json Message Received: " + message);
                        #if(DEBUG)
                        _ = Basic.LogMessage("Json Message Received: " + message);
                        #else
                        Console.WriteLine("Json Message Received: " + message);
                        #endif

                        var rpcMessage = JsonSerializer.Deserialize<RpcMessage>(message);
                        if (rpcMessage?.Method == "Abort")
                        {
                            Logger.Instance.ILog("Received Abort request from server.");
                            HandleAbort();
                        }

                        // Deserialize the response to get the correlation ID
                        var response = JsonSerializer.Deserialize<RpcResponse<object>>(message);
                        if (response != null && response.Id != null)
                        {
                            Program.Log("Json Message Received: response.Id: " + response.Id);
                            var requestId = (int)response.Id;
                            if (responseTasks.TryGetValue(requestId, out var tcs))
                            {
                                // Complete the task with the response
                                tcs.SetResult(message);
                                responseTasks.Remove(requestId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_disposed)
                            return;
                        Logger.Instance.ELog("Failed with JSON RPC message: " + ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            if (_disposed)
                return;
            
            Logger.Instance.ELog("Error in server message listener: " + ex);
        }
    }

    /// <summary>
    /// Sends a request to the server and waits for a response.
    /// </summary>
    internal async Task<T> SendRequest<T>(string method, params object[] parameters)
    {
        var requestId = Interlocked.Increment(ref currentRequestId);  // Unique request ID
        var request = new
        {
            Id = requestId,
            Method = method,
            Params = parameters
        };
        Program.Log($"Json Message SendRequest[{request.Id}]: {method}");

        // Create a TaskCompletionSource to await the response
        var tcs = new TaskCompletionSource<string>();
        responseTasks.Add(requestId, tcs);

        // Ensure no other task is reading or writing at the same time
        await streamLock.WaitAsync();
        try
        {
            // Write the request to the server
            var requestJson = JsonSerializer.Serialize(request);
            Program.Log($"Json Message SendRequest[{request.Id}]: {method}: sending");
            await writer.WriteLineAsync(requestJson);
            Program.Log($"Json Message SendRequest[{request.Id}]: {method}: sent");

            // Wait for the response and return the deserialized result
            var responseJson = await tcs.Task;
            Program.Log($"Json Message SendRequest[{request.Id}]: {method}: Got response: {responseJson}");
            var response = JsonSerializer.Deserialize<RpcResponse<T?>>(responseJson);
            if (string.IsNullOrWhiteSpace(response.Error) == false)
            {
                Program.Log($"Json Message SendRequest[{request.Id}]: {method}: Error: {response.Error}");
                throw new Exception(response.Error);
            }

            Program.Log($"Json Message SendRequest[{request.Id}]: {method}: success: {response.Result}");
            return response.Result;
        }
        finally
        {
            streamLock.Release();
        }
    }

    /// <summary>
    /// Sends a request to the server without expecting a response.
    /// </summary>
    internal async Task SendRequest(string method, params object[] parameters)
    {
        var request = new
        {
            Method = method,
            Params = parameters
        };

        // Ensure no other task is reading or writing at the same time
        await streamLock.WaitAsync();
        try
        {
            // Write the request to the server
            var requestJson = JsonSerializer.Serialize(request);
            await writer.WriteLineAsync(requestJson);
        }
        finally
        {
            streamLock.Release();
        }
    }
    
    /// <summary>
    /// The server requested this process be aborted
    /// </summary>
    private void HandleAbort()
    {
        Logger.Instance.WLog("Aborting client operations as requested by server.");
        Console.WriteLine("Aborting client operations as requested by server.");
        OnAbort?.Invoke();
    }

    public Action OnAbort { get; set; }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        foreach (var disposable in new IDisposable[]
                 {
                     client, reader, writer, cts, listeningTask, streamLock
                 })
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception)
            {
                // Ignored
            }
        }
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
    /// Gets or sets the ID of the request this response is associated with.
    /// </summary>
    public int? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the result of the RPC call.
    /// </summary>
    public T Result { get; set; }
    
    /// <summary>
    /// Gets or sets the error from the call
    /// </summary>
    public string? Error { get; set; }
}