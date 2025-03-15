// using System.Timers;
// using Microsoft.AspNetCore.Components;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.JSInterop;
//
// namespace FileFlows.Client.Services;
//
// /// <summary>
// /// Service used for cached communication between the client and server
// /// </summary>
// public partial class ClientService
// {
//     /// <summary>
//     /// The URI of the WebSocket server.
//     /// </summary>
//     private readonly string ServerUri;
//     
//     /// <summary>
//     /// Represents the navigation manager used to retrieve the current URL.
//     /// </summary>
//     private readonly NavigationManager _navigationManager;
//
//     /// <summary>
//     /// The instance of <see cref="IMemoryCache"/> used for caching.
//     /// </summary>
//     private readonly IMemoryCache _cache;
//
//     /// <summary>
//     /// The instance of the cache service
//     /// </summary>
//     private readonly CacheService _cacheService;
//
//     /// <summary>
//     /// The javascript runtime
//     /// </summary>
//     private readonly IJSRuntime _jsRuntime;
//
//     /// <summary>
//     /// Gets when this is paused until
//     /// </summary>
//     public DateTime? PausedUntil { get; private set; }
//
//     /// <summary>
//     /// Gets if this system is paused
//     /// </summary>
//     public bool IsPaused => PausedUntil != null && PausedUntil > DateTime.UtcNow;
//
//     /// <summary>
//     /// The paused timer that will trigger when the system is no longer paused
//     /// </summary>
//     private System.Timers.Timer PausedTimer;
//
//     /// <summary>
//     /// Initializes a new instance of the ClientService class.
//     /// </summary>
//     /// <param name="navigationManager">The navigation manager instance.</param>
//     /// <param name="memoryCache">The memory cache instance used for caching.</param>
//     /// <param name="jsRuntime">The javascript runtime.</param>
//     /// <param name="cacheService">The cache service instance.</param>
//     public ClientService(NavigationManager navigationManager, IMemoryCache memoryCache, IJSRuntime jsRuntime, CacheService cacheService)
//     {
//         _jsRuntime = jsRuntime;
//         _cacheService = cacheService;
//         _navigationManager = navigationManager; 
//         _cache = memoryCache;
//         _ = InitializeSystemInfo();
//         #if(DEBUG)
//         //ServerUri = "ws://localhost:6868/client-service";
//         ServerUri = "http://localhost:6868/client-service";
//         #else
//         ServerUri = $"{_navigationManager.BaseUri}client-service";
//         #endif
//         _ = StartAsync();
//     }
//
//     async Task InitializeSystemInfo()
//     {
//         try
//         {
//             var systemInfoResult = (await HttpHelper.Get<SystemInfo>("/api/system/info"));
//             if (systemInfoResult.Success)
//             {
//                 var sysInfo = systemInfoResult.Data;
//                 if (sysInfo.IsPaused)
//                 {
//                     SetPausedFor(sysInfo.PausedUntil.Subtract(sysInfo.CurrentTime).TotalMinutes);
//                 }
//             }
//         }
//         catch (Exception)
//         {
//         }
//     }
//
//     /// <summary>
//     /// Fires a javascript event
//     /// </summary>
//     /// <param name="eventName">the name of the event</param>
//     /// <param name="data">the event data</param>
//     private void FireJsEvent(string eventName, object data)
//     {
//         _jsRuntime.InvokeVoidAsync("clientServiceInstance.onEvent", eventName, data);
//     }
//
//     private void SetPausedFor(double minutes)
//     {
//         if (PausedTimer?.Enabled == true)
//         {
//             PausedTimer.Stop();
//             PausedTimer.Elapsed -= PausedTimerOnElapsed!;
//             PausedTimer.Dispose();
//             PausedTimer = null;
//         }
//         if (minutes <= 0)
//         {
//             PausedUntil = DateTime.MinValue;
//             SystemPausedUpdated(false);
//             return;
//         }
//
//         if (Math.Abs(minutes - int.MaxValue) < 10)
//             PausedUntil = DateTime.MaxValue;
//         else
//         {
//             PausedUntil = DateTime.UtcNow.AddMinutes(minutes);
//             PausedTimer = new Timer();
//             PausedTimer.Interval = minutes * 1000;
//             PausedTimer.Elapsed += PausedTimerOnElapsed!;
//             PausedTimer.Start();
//         }
//
//         SystemPausedUpdated(true);
//     }
//
//     private void PausedTimerOnElapsed(object sender, ElapsedEventArgs e)
//     {
//         SystemPausedUpdated(false);
//     }
// }