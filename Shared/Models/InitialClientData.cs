namespace FileFlows.Shared.Models;

/// <summary>
/// The data that is sent to connecting clients (browsers) when first loaded
/// This populates all the cached data for them, so future pages do no need to make heaps of requests
/// </summary>
public class InitialClientData
{
    /// <summary>
    /// Gets or sets a basic list of libraries
    /// </summary>
    [JsonPropertyName("l")]
    public Dictionary<Guid, string> LibraryList { get; set; }
    
    /// <summary>
    /// Gets or sets a basic list of flows
    /// </summary>
    [JsonPropertyName("f")]
    public Dictionary<Guid, string> FlowList { get; set; }
    
    /// <summary>
    /// Gets or sets the users profile
    /// </summary>
    [JsonPropertyName("p")]
    public Profile Profile { get; set; }
    
    /// <summary>
    /// Gets or sets the node status summaries
    /// </summary>
    [JsonPropertyName("n")]
    public List<NodeStatusSummary> NodeStatusSummaries { get; set; }
    
    /// <summary>
    /// Gets or sets the tags in the system
    /// </summary>
    [JsonPropertyName("t")]
    public List<Tag> Tags { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the current file over data
    /// </summary>>
    [JsonPropertyName("fo")]
    public FileOverviewData CurrentFileOverData { get; set; }
    
    /// <summary>
    /// Gets or sets the current system info
    /// </summary>
    [JsonPropertyName("si")]
    public SystemInfo CurrentSystemInfo { get; set; }
    
    /// <summary>
    /// Gets or set the current update info
    /// </summary>
    [JsonPropertyName("ui")]
    public UpdateInfo CurrentUpdatesInfo { get; set; }
    
    /// <summary>
    /// Gets or sets the current executor info
    /// </summary>
    [JsonPropertyName("ei")]
    public List<FlowExecutorInfoMinified> CurrentExecutorInfoMinified { get; set; }

    /// <summary>
    /// Gets or sets the json for the language
    /// </summary>
    [JsonPropertyName("in")]
    public string LanguageJson { get; set; }
}