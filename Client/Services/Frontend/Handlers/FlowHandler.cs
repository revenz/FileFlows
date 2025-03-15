namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Flow Handler related data
/// </summary>
/// <param name="feService">the frontend service</param>
public class FlowHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets all the flow elements in the system
    /// </summary>
    public List<FlowElement> FlowElements { get; private set; } = [];
    /// <summary>
    /// Gets or sets a basic list of flows
    /// </summary>
    public Dictionary<Guid, string> FlowList { get; private set; }
    
    /// <summary>
    /// Event raised when the flows are updated
    /// </summary>
    public event Action<Dictionary<Guid, string>> FlowListUpdated; 
    
    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        FlowElements = data.FlowElements;
        FlowList = data.FlowList;
        
        feService.Registry.Register<Dictionary<Guid, string>>(nameof(FlowList), (ed) =>
        {
            FlowList = ed;
            FlowListUpdated?.Invoke(ed);
        });
    }
}