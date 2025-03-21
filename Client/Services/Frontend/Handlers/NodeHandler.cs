namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Node related data
/// </summary>
/// <param name="feService">the front end service</param>
public class NodeHandler(FrontendService feService)
{
    public List<NodeStatusSummary> NodeStatusSummaries { get; private set; } = [];
    /// <summary>
    /// Gets or sets a basic list of node
    /// </summary>
    public Dictionary<Guid, string> NodeList { get; private set; }
    
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
        string lblInternalNode = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        foreach (var node in data.NodeStatusSummaries)
        {
            if (node.Uid == CommonVariables.InternalNodeUid)
                node.Name = lblInternalNode;
        }

        NodeList = data.NodeStatusSummaries.ToDictionary(x => x.Uid, x => x.Name);
        NodeStatusSummaries = data.NodeStatusSummaries;
        feService.Registry.Register<List<NodeStatusSummary>>(nameof(NodeStatusSummary), (nss) =>
        {
            foreach (var node in nss)
            {
                if (node.Uid == CommonVariables.InternalNodeUid)
                    node.Name = lblInternalNode;
            }
            
            NodeStatusSummaries = nss;
            NodeList = nss.ToDictionary(x => x.Uid, x => x.Name);
            NodeStatusUpdated?.Invoke(nss);
        });
    }
}