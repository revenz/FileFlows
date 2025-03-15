namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Flow Handler related data
/// </summary>
public class FlowHandler()
{
    /// <summary>
    /// Gets or sets all the flow elements in the system
    /// </summary>
    public List<FlowElement> FlowElements { get; private set; } = [];
    
    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        FlowElements = data.FlowElements;
    }
}