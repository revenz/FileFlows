using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

/// <summary>
/// Cache handler
/// </summary>
public class CacheHandler
{
    private JsonRpcServer rpcServer;
    private ClientConnection _connection;

    /// <summary>
    /// Constructs a new instance of the handler
    /// </summary>
    /// <param name="rpcServer">the RPC server</param>
    /// <param name="rpcRegister">the RPC register</param>
    public CacheHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _connection = rpcServer._client.Connection;
        rpcRegister.Register<string, string>(nameof(GetJsonAsync), GetJsonAsync);
        rpcRegister.Register<StoreJsonModel>(nameof(StoreJsonAsync), StoreJsonAsync);
    }

    /// <summary>
    /// Retrieves a cached item as a raw JSON string.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>A task representing the asynchronous operation, containing the JSON string if found; otherwise, <c>null</c>.</returns>
    public async Task<string> GetJsonAsync(string key)
    {
        if(await _connection.AwaitConnection() == false)
            return string.Empty;
        try
        {
            return await _connection.InvokeAsync<string>("Cache" + nameof(GetJsonAsync), key);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Stores a JSON string in the cache with an optional expiration time.
    /// </summary>
    /// <param name="model">the model of the data</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public void StoreJsonAsync(StoreJsonModel model)
    {
        try
        {
            if (_connection.AwaitConnection().GetAwaiter().GetResult() == false)
                return;
            _ = _connection.SendAsync("Cache" + nameof(StoreJsonAsync), model.Key, model.Json, model.Expiration);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Running total model
    /// </summary>
    /// <param name="Key">The cache key.</param>
    /// <param name="Json">The JSON string to store.</param>
    /// <param name="Expiration">The optional expiration time. If not provided, a default expiration is used.</param>
    public record StoreJsonModel(string Key, string Json, TimeSpan? Expiration);
}