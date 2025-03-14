namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Node related data
/// </summary>
/// <param name="feService">the front end service</param>
public class NodeHandler(FrontendService feService)
{
    public List<NodeStatusSummary> NodeStatusSummaries { get; private set; } = [];
    
    /// <summary>
    /// Event raised when the node status is updated
    /// </summary>
    public event Action<List<NodeStatusSummary>> NodeStatusUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        NodeStatusSummaries = data.NodeStatusSummaries;
        feService.Registry.Register<List<NodeStatusSummary>>(nameof(NodeStatusSummary), (nss) =>
        {
            NodeStatusSummaries = nss;
            NodeStatusUpdated?.Invoke(nss);
        });
    }
}