namespace FileFlows.Shared.Models.SignalAre;

/// <summary>
/// Result from a node registration
/// </summary>
public class NodeRegisterResult
{
    /// <summary>
    /// Gets or sets if its a success
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sest the node
    /// </summary>
    public ProcessingNode Node { get; set; }
    /// <summary>
    /// Gets or sets the connection ID
    /// </summary>
    public string ConnectionId { get; set; }
    /// <summary>
    /// Gets or sets the config revision
    /// </summary>
    public int CurrentConfigRevision { get; set; }
    
}