namespace FileFlows.Client.Services.Frontend;

/// <summary>
/// Front end related data
/// </summary>
public class DashboardFrontend(FrontendService feService)
{
    public FileOverviewData CurrentFileOverData { get; private set; }
    public SystemInfo CurrentSystemInfo { get; private set; }
    public UpdateInfo CurrentUpdatesInfo { get; private set; }
    public List<FlowExecutorInfoMinified> CurrentExecutorInfoMinifed { get; private set; }
    public List<NodeStatusSummary> CurrentNodeStatusSummaries { get; private set; }
    public List<Tag> Tags { get; private set; } = new List<Tag>();
    

    public async Task Initialize()
    {
        CurrentFileOverData = await feService.GetInitialData<FileOverviewData>("dashboard/file-overview") ?? new ();
        CurrentSystemInfo  = await feService.GetInitialData<SystemInfo>("dashboard/info") ?? new ();
        CurrentUpdatesInfo  = await feService.GetInitialData<UpdateInfo>("dashboard/updates") ?? new ();
        CurrentExecutorInfoMinifed  = await feService.GetInitialData<List<FlowExecutorInfoMinified>>("dashboard/executors-info-minified") ?? new ();
        CurrentNodeStatusSummaries  = await feService.GetInitialData<List<NodeStatusSummary>>("dashboard/node-summary") ?? new ();
        Tags  = await feService.GetInitialData<List<Tag>>("tag") ?? new ();
        
        feService.Registry.Register<bool>("Paused", (bool paused) =>
        {
            Logger.Instance.ILog("Paused: " + paused);
        });
    }
}