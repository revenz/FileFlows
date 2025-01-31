// using FileFlows.WebServer.Controllers;
// using FileFlows.ServerShared.Services;
// using FileFlows.ServerShared.Workers;
// using FileFlows.Shared.Models;
//
// namespace FileFlows.Server.Workers;
//
// /// <summary>
// /// Worker to monitor libraries
// /// </summary>
// public class LibraryWorker : ServerWorker
// {
//     
//     private Dictionary<string, WatchedLibrary> WatchedLibraries = new ();
//
//     /// <summary>
//     /// Gets the instance of the library worker
//     /// </summary>
//     private static LibraryWorker Instance;
//
//     private readonly Logger Logger;
//
//     /// <summary>
//     /// Creates a new instance of the library worker
//     /// </summary>
//     public LibraryWorker() : base(ScheduleType.Minute, 1)
//     {
//         Instance = this;
//         Logger = new();
//         Logger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "Library", false));
//     }
//
//     public override void Start()
//     {
//         base.Start();
//         UpdateLibraries();
//     }
//
//     public override void Stop()
//     {
//         base.Stop();
//     }
//     
//     private DateTime LibrariesLastUpdated = DateTime.MinValue;
//
//     /// <summary>
//     /// Triggers a scan now
//     /// </summary>
//     public static void ScanNow() => Instance?.Trigger();
//
//     /// <summary>
//     /// Updates the libraries being watched
//     /// </summary>
//     public static void UpdateLibraries() => Instance?.UpdateLibrariesInstance();
//
//     /// <summary>
//     /// Gets a watched library instance for a library if one exists
//     /// </summary>
//     /// <param name="library">the library</param>
//     /// <returns>the watched library instance</returns>
//     public static WatchedLibrary? GetWatchedLibrary(Library library)
//     {
//         string key =  library.Uid + ":" + library.Path;
//         return Instance.WatchedLibraries.GetValueOrDefault(key);
//     }
//     
//
//     private void Watch(params Library[] libraries)
//     {
//         foreach(var lib in libraries)
//         {
//             string key = lib.Uid + ":" + lib.Path;
//             lock (WatchedLibraries)
//             {
//                 if (WatchedLibraries.ContainsKey(key))
//                     continue;
//                 WatchedLibraries.Add(key, new WatchedLibrary(Logger, lib));
//             }
//         }
//     }
//
//     private void Unwatch(params string[] keys)
//     {
//         foreach(string key in keys)
//         {
//             if (WatchedLibraries.TryGetValue(key, out var watcher) == false)
//                 continue;
//             watcher.Dispose();
//             WatchedLibraries.Remove(key);
//             watcher = null;
//         }
//     }
//
//     /// <summary>
//     /// Updates the libraries being watched
//     /// </summary>
//     private void UpdateLibrariesInstance()
//     {
//         Logger.DLog("LibraryWorker: Updating Libraries");
//         var libraries = new Services.LibraryService().GetAllAsync().Result
//             .Where(x => x.Enabled && x.Uid != CommonVariables.ManualLibraryUid).ToList();
//         var libraryUids = libraries.Select(x => x.Uid + ":" + x.Path).ToList();            
//
//         Watch(libraries.Where(x =>  WatchedLibraries.ContainsKey(x.Uid + ":" + x.Path) == false).ToArray());
//         Unwatch(WatchedLibraries.Keys.Where(x => libraryUids.Contains(x) == false).ToArray());
//
//         foreach (var libwatcher in WatchedLibraries.Values)
//         {   
//             var library = libraries.FirstOrDefault(x => libwatcher.Library.Uid == x.Uid);
//             if (library == null)
//                 continue;
//             libwatcher.UpdateLibrary(library);
//         }
//      
//         LibrariesLastUpdated = DateTime.UtcNow;
//     }
//
//     protected override void ExecuteActual(Settings settings)
//     {
//         if(LibrariesLastUpdated < DateTime.UtcNow.AddHours(-1))
//             UpdateLibrariesInstance();
//
//         foreach(var libwatcher in WatchedLibraries.Values)
//         {
//             var library = libwatcher.Library;
//             if (library.Enabled == false)
//                 continue; // dont scan a disabled library
//             if (library.FullScanIntervalMinutes == 0)
//                 library.FullScanIntervalMinutes = 60;
//             bool scan = library.Scan;
//             if (!scan && library.Uid == libwatcher.Library?.Uid && libwatcher.UseScanner)
//                 scan = true;
//             
//             if (libwatcher.ScanComplete == false)
//             {
//                 // hasn't been scanned yet, we scan when the app starts or library is first added
//             }
//             else if (scan == false)
//             {
//                 if (library.FullScanDisabled)
//                 {
//                     Logger.DLog($"LibraryWorker: Library '{library.Name}' full scan disabled");
//                     continue;
//                 }
//
//                 // need to check full scan interval
//                 if (library.LastScannedAgo.TotalMinutes < library.FullScanIntervalMinutes)
//                 {
//                     Logger.DLog($"LibraryWorker: Library '{library.Name}' was scanned recently {library.LastScannedAgo} (full scan interval {library.FullScanIntervalMinutes} minutes)");
//                     continue;
//                 }
//             }
//             else if (library.LastScannedAgo.TotalSeconds < library.ScanInterval)
//             {
//                 Logger.DLog($"LibraryWorker: Library '{library.Name}' was scanned recently {library.LastScannedAgo} ({(new TimeSpan(library.ScanInterval * TimeSpan.TicksPerSecond))}");
//                 continue;
//             }
//
//             Logger.DLog($"LibraryWorker: Library '{library.Name}' calling scan " +
//                                  $"(Scan complete: {libwatcher.ScanComplete}) " +
//                                  $"(Library Scan: {scan} " +
//                                  $"(last scanned: {library.LastScannedAgo}) " +
//                                  $"(Full Scan interval: {library.FullScanIntervalMinutes})");
//
//             Task.Run(() =>
//             {
//                 libwatcher.Scan();
//             });
//         }
//     }
// }
