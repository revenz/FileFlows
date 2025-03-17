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
    public Dictionary<Guid, string> FlowList { get; private set; } = [];

    /// <summary>
    /// Gets or sets a basic list of flows
    /// </summary>
    public List<FlowListModel> Flows { get; private set; } = [];
    
    /// <summary>
    /// Event raised when the flows are updated
    /// </summary>
    public event Action<Dictionary<Guid, string>> FlowListUpdated;

    /// <summary>
    /// Event raised when the flows are updated
    /// </summary>
    public event Action<List<FlowListModel>> FlowsUpdated;
    
    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        FlowElements = data.FlowElements;
        Flows = data.Flows;
        FlowList = data.Flows.ToDictionary(x => x.Uid, x => x.Name);
        
        feService.Registry.Register<List<FlowListModel>>(nameof(Flows), (ed) =>
        {
            Flows = ed;
            FlowList = ed.ToDictionary(x => x.Uid, x => x.Name);
            FlowsUpdated?.Invoke(Flows);
            FlowListUpdated?.Invoke(FlowList);
        });
    }
}