using FileFlows.ServerShared.Interfaces;

namespace FileFlows.RemoteServices;

/// <summary>
/// Service for sending a notification to the server
/// </summary>
public class RemoteDistributedCacheService : RemoteService, IDistributedCacheService
{

    /// <inheritdoc />
    public async Task<string?> GetJsonAsync(string key)
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/remote/cache/" + key);
            if (result is { Success: true, Data: not null })
                return result.Data;
            return default;
        }
        catch (Exception ex)
        {
            // ignored
            Logger.Instance?.ELog($"Failed to get '{key}' from cache: " + ex.Message);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task StoreJsonAsync(string key, string json, TimeSpan? expiration = null)
    {
        try
        {
            _ = await HttpHelper.Post($"{ServiceBaseUrl}/remote/cache", new
            {
                Key = key,
                Json = json,
                Expiration = expiration
            });
        }
        catch (Exception ex)
        {
            // ignored
            Logger.Instance?.ELog($"Failed to store '{key}' int cache: " + ex.Message);
        }
    }
}
