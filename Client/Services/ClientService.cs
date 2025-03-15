using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Service used for cached communication between the client and server
/// </summary>
public partial class ClientService
{
    /// <summary>
    /// The URI of the WebSocket server.
    /// </summary>
    private readonly string ServerUri;
    
    /// <summary>
    /// Represents the navigation manager used to retrieve the current URL.
    /// </summary>
    private readonly NavigationManager _navigationManager;

    /// <summary>
    /// The instance of <see cref="IMemoryCache"/> used for caching.
    /// </summary>
    private readonly IMemoryCache _cache;

    /// <summary>
    /// The instance of the cache service
    /// </summary>
    private readonly CacheService _cacheService;

    /// <summary>
    /// The javascript runtime
    /// </summary>
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Gets when this is paused until
    /// </summary>
    public DateTime? PausedUntil { get; private set; }

    /// <summary>
    /// Gets if this system is paused
    /// </summary>
    public bool IsPaused => PausedUntil != null && PausedUntil > DateTime.UtcNow;

    /// <summary>
    /// The paused timer that will trigger when the system is no longer paused
    /// </summary>
    private System.Timers.Timer PausedTimer;

    /// <summary>
    /// Initializes a new instance of the ClientService class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager instance.</param>
    /// <param name="memoryCache">The memory cache instance used for caching.</param>
    /// <param name="jsRuntime">The javascript runtime.</param>
    /// <param name="cacheService">The cache service instance.</param>
    public ClientService(NavigationManager navigationManager, IMemoryCache memoryCache, IJSRuntime jsRuntime, CacheService cacheService)
    {
        _jsRuntime = jsRuntime;
        _cacheService = cacheService;
        _navigationManager = navigationManager; 
        _cache = memoryCache;
        _ = InitializeSystemInfo();
        #if(DEBUG)
        //ServerUri = "ws://localhost:6868/client-service";
        ServerUri = "http://localhost:6868/client-service";
        #else
        ServerUri = $"{_navigationManager.BaseUri}client-service";
        #endif
        _ = StartAsync();
    }

    async Task InitializeSystemInfo()
    {
        try
        {
            var systemInfoResult = (await HttpHelper.Get<SystemInfo>("/api/system/info"));
            if (systemInfoResult.Success)
            {
                var sysInfo = systemInfoResult.Data;
                if (sysInfo.IsPaused)
                {
                    SetPausedFor(sysInfo.PausedUntil.Subtract(sysInfo.CurrentTime).TotalMinutes);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Gets the executor info
    /// </summary>
    /// <returns>the executor info</returns>
    public Task<List<FlowExecutorInfo>> GetExecutorInfo()
        => GetOrCreate("FlowExecutorInfo", async () =>
        {
            var response = await HttpHelper.Get<List<FlowExecutorInfo>>("/api/worker");
            if (response.Success == false)
                return new List<FlowExecutorInfo>();
            return response.Data!;
        }, absExpiration: 10);


    private TItem GetOrCreate<TItem>(object key, Func<TItem> createItem, int slidingExpiration = 5,
        int absExpiration = 30, bool force = false)
    {
        TItem? cacheEntry;
        if (force || _cache.TryGetValue(key, out cacheEntry) == false) // Look for cache key.
        {
            // Key not in cache, so get data.
            cacheEntry = createItem();

            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            cacheEntryOptions.SetPriority(CacheItemPriority.High);
            if (slidingExpiration > 0)
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration));
            if (absExpiration > 0)
                cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(absExpiration));

            // Save data in cache.
            _cache.Set(key, cacheEntry, cacheEntryOptions);
        }

        return cacheEntry;
    }

    /// <summary>
    /// Fires a javascript event
    /// </summary>
    /// <param name="eventName">the name of the event</param>
    /// <param name="data">the event data</param>
    private void FireJsEvent(string eventName, object data)
    {
        _jsRuntime.InvokeVoidAsync("clientServiceInstance.onEvent", eventName, data);
    }

    private void SetPausedFor(double minutes)
    {
        if (PausedTimer?.Enabled == true)
        {
            PausedTimer.Stop();
            PausedTimer.Elapsed -= PausedTimerOnElapsed!;
            PausedTimer.Dispose();
            PausedTimer = null;
        }
        if (minutes <= 0)
        {
            PausedUntil = DateTime.MinValue;
            SystemPausedUpdated(false);
            return;
        }

        if (Math.Abs(minutes - int.MaxValue) < 10)
            PausedUntil = DateTime.MaxValue;
        else
        {
            PausedUntil = DateTime.UtcNow.AddMinutes(minutes);
            PausedTimer = new Timer();
            PausedTimer.Interval = minutes * 1000;
            PausedTimer.Elapsed += PausedTimerOnElapsed!;
            PausedTimer.Start();
        }

        SystemPausedUpdated(true);
    }

    private void PausedTimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        SystemPausedUpdated(false);
    }
    //
    // /// <summary>
    // /// Gets the current update info
    // /// </summary>
    // /// <returns>the update info</returns>
    // public async Task<UpdateInfo?> GetCurrentUpdatesInfo()
    // {
    //     if (CurrentUpdatesInfo != null) return CurrentUpdatesInfo;
    //     var result = await HttpHelper.Get<UpdateInfo>("/api/dashboard/updates");
    //     if (result.Success)
    //         CurrentUpdatesInfo ??= result.Data;
    //     return CurrentUpdatesInfo;
    // }

    // /// <summary>
    // /// Gets the current update info
    // /// </summary>
    // /// <returns>the update info</returns>
    // public async Task<List<FlowExecutorInfoMinified>> GetCurrentExecutorInfoMinifed()
    // {
    //     if (CurrentExecutorInfoMinified != null) return CurrentExecutorInfoMinified;
    //     var result = await HttpHelper.Get<List<FlowExecutorInfoMinified>>("/api/dashboard/executors-info-minified");
    //     if (result.Success)
    //         CurrentExecutorInfoMinified ??= result.Data;
    //     return CurrentExecutorInfoMinified ?? [];
    // }
    // /// <summary>
    // /// Gets the current node status summaries
    // /// </summary>
    // /// <returns>the current node status summaries</returns>
    // public async Task<List<NodeStatusSummary>> GetCurrentNodeStatusSummaries()
    // {
    //     if (CurrentNodeStatusSummaries != null) return CurrentNodeStatusSummaries;
    //     var result = await HttpHelper.Get<List<NodeStatusSummary>>("/api/dashboard/node-summary");
    //     if (result.Success)
    //         CurrentNodeStatusSummaries ??= result.Data;
    //     return CurrentNodeStatusSummaries;
    // }

    // /// <summary>
    // /// Gets the current file overview data
    // /// </summary>
    // /// <returns>the current file overview data</returns>
    // public async Task<FileOverviewData?> GetCurrentFileOverData()
    // {
    //     if (CurrentFileOverData != null) return CurrentFileOverData;
    //     var result = await HttpHelper.Get<FileOverviewData>("/api/dashboard/file-overview");
    //     if (result.Success)
    //         CurrentFileOverData ??= result.Data;
    //     return CurrentFileOverData;
    // }


    /// <summary>
    /// Gets the tags in the system
    /// </summary>
    /// <returns>the tags</returns>
    public async Task<List<Tag>> GetTags()
    {
        if (Tags != null) return Tags;
        var result = await HttpHelper.Get<List<Tag>>("/api/tag");
        if (result.Success)
            Tags ??= result.Data;
        return Tags ?? [];
    }
    
    /// <summary>
    /// Gets the all library savings data
    /// </summary>
    /// <returns></returns>
    public async Task<List<StorageSavedData>> GetLibrarySavingsAllData()
        => await _cacheService.GetFromCache("LibraryFilesAllData", 5 * 60,
            async () => await GetLibraryFileData(0));

    /// <summary>
    /// Gets the month library savings data
    /// </summary>
    /// <returns></returns>
    public async Task<List<StorageSavedData>> GetLibrarySavingsMonthData()
        => await _cacheService.GetFromCache("LibraryFilesMonthData", 5 * 60,
                async () => await GetLibraryFileData(31));
    
    private async Task<List<StorageSavedData>> GetLibraryFileData(int days)
    {
        var result = await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw?days=" + days);
        return result.Success ? result.Data ?? [] : [];
    }

    /// <summary>
    /// Gets all the flow elements in the system
    /// </summary>
    /// <returns>all the flow elements in the system</returns>
    public async Task<List<FlowElement>> GetAllFlowElements()
        => await _cacheService.GetFromCache<List<FlowElement>>("AllFlowElements", 30,
            async () =>
            {
                var result = await HttpHelper.Get<List<FlowElement>>("/api/flow/elements");
                List<FlowElement> list = result.Success ? result.Data ?? [] : [];
                return list;
            });
}