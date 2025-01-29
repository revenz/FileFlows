namespace FileFlows.ServerShared.Interfaces;

/// <summary>
/// Distributed cache service interface
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// Retrieves a cached item by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores an item in the cache with an optional expiration time.
    /// </summary>
    Task StoreAsync<T>(string key, T value, TimeSpan? expiration = null);
}