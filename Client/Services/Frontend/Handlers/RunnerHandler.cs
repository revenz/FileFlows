namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Runner handler
/// </summary>
/// <param name="feService">the frontend service</param>
public class RunnerHandler(FrontendService feService)
{
    public List<FlowExecutorInfoMinified> Runners { get; private set; }
    
    /// <summary>
    /// Event raised when the runner info has been updated
    /// </summary>
    public event Action<List<FlowExecutorInfoMinified>> RunnerInfoUpdated;

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial client data</param>
    public void Initialize(InitialClientData data)
    {
        Runners = data.CurrentExecutorInfoMinified;
        feService.Registry.Register<List<FlowExecutorInfoMinified>>("Runners", (ed) =>
        {
            Runners = ed;
            RunnerInfoUpdated?.Invoke(ed);
        });
    }
}