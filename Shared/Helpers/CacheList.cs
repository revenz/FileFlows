namespace FileFlows.Shared.Helpers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

/// <summary>
/// A generic cache list that stores keys for a specified duration.
/// Expired keys are automatically removed when checked.
/// </summary>
/// <typeparam name="TKey">The type of the key, typically a GUID.</typeparam>
public class CacheList<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, DateTime> _cache = new();
    private readonly TimeSpan _expiration;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheList{TKey}"/> class.
    /// </summary>
    /// <param name="expiration">The time duration after which an item expires.</param>
    /// <param name="cleanupInterval">The interval at which expired items are removed.</param>
    public CacheList(TimeSpan expiration, TimeSpan? cleanupInterval = null)
    {
        _expiration = expiration;
        _cleanupTimer = new Timer(RemoveExpiredItems, null, cleanupInterval ?? TimeSpan.FromMinutes(1), cleanupInterval ?? TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Adds a key to the cache list, resetting its expiration time.
    /// </summary>
    /// <param name="key">The key to add.</param>
    public void Add(TKey key)
    {
        _cache[key] = DateTime.UtcNow.Add(_expiration);
    }

    /// <summary>
    /// Checks if the key is in the cache and not expired.
    /// If expired, it is removed automatically.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists and is not expired, otherwise false.</returns>
    public bool Contains(TKey key)
    {
        if (_cache.TryGetValue(key, out DateTime expirationTime))
        {
            if (expirationTime > DateTime.UtcNow)
                return true;

            // Expired, remove it
            _cache.TryRemove(key, out _);
        }
        return false;
    }

    /// <summary>
    /// Removes an item from the cache manually.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the item was removed, otherwise false.</returns>
    public bool Remove(TKey key)
    {
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Removes expired items from the cache.
    /// This is called automatically at the cleanup interval.
    /// </summary>
    private void RemoveExpiredItems(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var key in _cache.Keys.ToList())
        {
            if (_cache.TryGetValue(key, out DateTime expirationTime) && expirationTime <= now)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
