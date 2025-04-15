using System.Text.Json;
using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Service for interacting with local storage using JavaScript Interop.
/// </summary>
public class FFLocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private bool? _localStorageEnabled;
    private string _accessToken;

    /// <summary>
    /// Gets a value indicating whether local storage is enabled.
    /// </summary>
    public bool LocalStorageEnabled => _localStorageEnabled == true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFLocalStorageService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript Runtime service provided by Blazor.</param>
    public FFLocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Sets an item in the local storage with optional expiration.
    /// </summary>
    /// <param name="key">The key of the item to set.</param>
    /// <param name="value">The value of the item to set.</param>
    /// <param name="expiry">Optional expiration timespan.</param>
    public async Task SetItemAsync(string key, object value, TimeSpan? expiry = null)
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();

        if (_localStorageEnabled == true)
        {
            var item = new StorageItem<object>
            {
                Value = value,
                ExpiresAt = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null
            };
            string json = JsonSerializer.Serialize(item);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }
    }

    /// <summary>
    /// Gets an item from the local storage. Removes it if expired.
    /// </summary>
    /// <typeparam name="T">The type of the item to get.</typeparam>
    /// <param name="key">The key of the item to get.</param>
    /// <returns>The item or default if not found/expired.</returns>
    public async Task<T> GetItemAsync<T>(string key)
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();

        if (_localStorageEnabled == true)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                if (json == null)
                    return default;

                var item = JsonSerializer.Deserialize<StorageItem<T>>(json);
                if (item == null)
                    return default;

                if (item.ExpiresAt.HasValue && item.ExpiresAt.Value < DateTime.UtcNow)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                    return default;
                }

                return item.Value;
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// Checks if local storage is enabled.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckLocalStorageEnabled()
    {
        try
        {
            _localStorageEnabled = await _jsRuntime.InvokeAsync<bool>("localStorageEnabled");
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    /// Gets the access token
    /// </summary>
    /// <returns>the access token</returns>
    public async Task<string?> GetAccessToken()
    {
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if (_localStorageEnabled == true)
            return await GetItemAsync<string>("ACCESS_TOKEN");
        return _accessToken;
    }
    
    /// <summary>
    /// Sets the access token
    /// </summary>
    /// <param name="token">the token</param>
    public async Task SetAccessToken(string token)
    {
        _accessToken = token;
        
        if (_localStorageEnabled == null)
            await CheckLocalStorageEnabled();
        if (_localStorageEnabled == true)
            await SetItemAsync("ACCESS_TOKEN", token);
        await _jsRuntime.InvokeVoidAsync("ff.setAccessToken", token);
    }
    
    /// <summary>
    /// Represents a value stored in local storage with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the value being stored.</typeparam>
    private class StorageItem<T>
    {
        /// <summary>
        /// Gets or sets the value to be stored.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the item. If null, the item does not expire.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

}