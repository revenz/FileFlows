using System.Text.Json;

namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Cache Helper
/// </summary>
/// <param name="logger">the logger to use</param>
/// <param name="GetJsonFunc">the Get Json function</param>
/// <param name="SetJsonFunc">the Set JSON function</param>
public class CacheHelper(
    ILogger logger,
    Func<string, string?>? GetJsonFunc , 
    Action<string, string, TimeSpan?>? SetJsonFunc)
{
    /// <summary>
    /// Gets a strong type from the cache if available 
    /// </summary>
    /// <param name="key">the key for the item in the cache</param>
    /// <returns>The object if found, otherwise null</returns>
    public T? GetObject<T>(string key)
    {
        try
        {
            var json = GetJsonFunc(key);
            if (string.IsNullOrWhiteSpace(json))
                return default;
            logger?.ILog($"Got JSON '{key}' from cache: " + json);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            logger?.ILog($"Got exception '{key}' from cache: " + ex.Message);
            return default;
        }
    }

    /// <summary>
    /// Get a JSON string that is stored in the cache
    /// </summary>
    /// <param name="key">the key for the item in the cache</param>
    /// <returns>The JSON string if found, otherwise null</returns>
    public string GetJson(string key)
        => GetJsonFunc?.Invoke(key) ?? null;

    /// <summary>
    /// Stores an item in the cache
    /// </summary>
    /// <param name="key">the key for the item in the cache</param>
    /// <param name="value">the value being stored</param>
    /// <param name="minutes">the number of minutes an item is being stored for</param>
    public void SetObject(string key, object value, int minutes = 0)
    {
        if (value == null)
            return;
        try
        {
            var json = JsonSerializer.Serialize(value);
            SetJsonFunc(key, json, minutes > 0 ? TimeSpan.FromMinutes(minutes) : null);
        }
        catch (Exception)
        {
            // Ignore
        }
    }
    /// <summary>
    /// Stores an json in the cache
    /// </summary>
    /// <param name="key">the key for the item in the cache</param>
    /// <param json="json">the value json stored</param>
    /// <param name="minutes">the number of minutes an item is being stored for</param>
    public void SetJson(string key, string json, int minutes = 0)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        try
        {
            SetJsonFunc(key, json, minutes > 0 ? TimeSpan.FromMinutes(minutes) : null);
        }
        catch (Exception)
        {
            // Ignore
        }
    }
}