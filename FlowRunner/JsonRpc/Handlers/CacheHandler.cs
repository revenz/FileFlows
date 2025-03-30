namespace FileFlows.FlowRunner.JsonRpc.Handlers;

/// <summary>
/// Cache handler
/// </summary>
/// <param name="client">the JSON RPC Client</param>
public class CacheHandler(JsonRpcClient client)
{
    /// <summary>
    /// Retrieves a cached item as a raw JSON string.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>A task representing the asynchronous operation, containing the JSON string if found; otherwise, <c>null</c>.</returns>
    public async Task<string?> GetJsonAsync(string key)
        => await client.SendRequest<string?>(nameof(GetJsonAsync), key);

    /// <summary>
    /// Stores a JSON string in the cache with an optional expiration time.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="json">The JSON string to store.</param>
    /// <param name="expiration">The optional expiration time. If not provided, a default expiration is used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StoreJsonAsync(string key, string json, TimeSpan? expiration = null)
        => await client.SendRequest(nameof(StoreJsonAsync), new { Key = key, Json = json, Expiration = expiration});
}