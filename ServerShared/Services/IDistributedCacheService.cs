namespace FileFlows.ServerShared.Interfaces;

/// <summary>
/// Distributed cache service interface
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// Retrieves a cached item by key as its raw JSON
    /// </summary>
    Task<string?> GetJsonAsync(string key);

    /// <summary>
    /// Stores an item in the cache with an optional expiration time.
    /// </summary>
    Task StoreAsync(string key, object value, TimeSpan? expiration = null);
}