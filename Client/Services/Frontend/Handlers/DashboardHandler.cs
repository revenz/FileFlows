namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Front end related data
/// </summary>
public class DashboardHandler(FrontendService feService)
{
    public FileOverviewData CurrentFileOverData { get; private set; }
    public SystemInfo CurrentSystemInfo { get; private set; }
    public UpdateInfo CurrentUpdatesInfo { get; private set; }
    public List<FlowExecutorInfoMinified> CurrentExecutorInfoMinified { get; private set; }
    public List<Tag> Tags { get; private set; } = new List<Tag>();
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
    /// Event raised when the runner info has been updated
    /// </summary>
    public event Action<List<FlowExecutorInfoMinified>> RunnerInfoUpdated;
    
    /// <summary>
    /// Event raised when the update info has been updated
    /// </summary>
    public event Action<UpdateInfo> UpdateInfoUpdated;
    
    /// <summary>
    /// Event raised when the file overview data is updated info has been updated
    /// </summary>
    public event Action<FileOverviewData> FileOverviewDataUpdated;

    public void Initialize(InitialClientData data)
    {
        CurrentFileOverData = data.CurrentFileOverData;
        CurrentSystemInfo = data.CurrentSystemInfo;
        CurrentUpdatesInfo = data.CurrentUpdatesInfo;
        CurrentExecutorInfoMinified = data.CurrentExecutorInfoMinified;
        StorageSavedMonthData = data.StorageSavedMonthData;
        StorageSavedTotalData = data.StorageSavedTotalData;
        Tags = data.Tags;
        
        feService.Registry.Register<bool>("Paused", (bool paused) =>
        {
            Logger.Instance.ILog("Paused: " + paused);
        });
        feService.Registry.Register<SystemInfo>(nameof(SystemInfo), (se) =>
        {
            CurrentSystemInfo = se;
            SystemInfoUpdated?.Invoke(se);
        });
        feService.Registry.Register<List<FlowExecutorInfoMinified>>(nameof(FlowExecutorInfoMinified), (ed) =>
        {
            CurrentExecutorInfoMinified = ed;
            RunnerInfoUpdated?.Invoke(ed);
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