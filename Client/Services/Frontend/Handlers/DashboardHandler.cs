namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Front end related data
/// </summary>
/// <param name="feService">the frontend service</param>
public class DashboardHandler(FrontendService feService)
{
    public FileOverviewData CurrentFileOverData { get; private set; }
    public SystemInfo CurrentSystemInfo { get; private set; }
    public UpdateInfo CurrentUpdatesInfo { get; private set; }
    /// <summary>
    /// Gets or sets the total storage saved data
    /// </summary>
    public List<StorageSavedData> StorageSavedTotalData { get; private set; }

    /// <summary>
    /// Gets or sets the total storage saved in a month
    /// </summary>
    public List<StorageSavedData> StorageSavedMonthData { get; private set; }
    
    /// <summary>
    /// Event raised when the system info has been updated
    /// </summary>
    public event Action<SystemInfo> SystemInfoUpdated;
    
    /// <summary>
    /// Event raised when the update info has been updated
    /// </summary>
    public event Action<UpdateInfo> UpdateInfoUpdated;
    
    /// <summary>
    /// Event raised when the file overview data is updated info has been updated
    /// </summary>
    public event Action<FileOverviewData> FileOverviewDataUpdated;

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial client data</param>
    public void Initialize(InitialClientData data)
    {
        CurrentFileOverData = data.CurrentFileOverData;
        CurrentSystemInfo = data.CurrentSystemInfo;
        CurrentUpdatesInfo = data.CurrentUpdatesInfo;
        StorageSavedMonthData = data.StorageSavedMonthData;
        StorageSavedTotalData = data.StorageSavedTotalData;
        
        feService.Registry.Register<bool>("Paused", (bool paused) =>
        {
            Logger.Instance.ILog("Paused: " + paused);
        });
        feService.Registry.Register<SystemInfo>(nameof(SystemInfo), (se) =>
        {
            CurrentSystemInfo = se;
            SystemInfoUpdated?.Invoke(se);
        });
        feService.Registry.Register<UpdateInfo>(nameof(UpdateInfo), (ed) =>
        {
            CurrentUpdatesInfo = ed;
            UpdateInfoUpdated?.Invoke(ed);
        });
        feService.Registry.Register<FileOverviewData>(nameof(FileOverviewData), (ed) =>
        {
            CurrentFileOverData = ed;
            FileOverviewDataUpdated?.Invoke(ed);
        });
    }
}