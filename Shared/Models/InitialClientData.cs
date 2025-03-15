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
    
    /// <summary>
    /// Gets or sets the total storage saved data
    /// </summary>
    [JsonPropertyName("sst")]
    public List<StorageSavedData> StorageSavedTotalData { get; set; }

    /// <summary>
    /// Gets or sets the total storage saved in a month
    /// </summary>
    [JsonPropertyName("ssm")]
    public List<StorageSavedData> StorageSavedMonthData { get; set; }
    
    /// <summary>
    /// Gets or sets the upcoming files to process
    /// </summary>
    [JsonPropertyName("fu")]
    public List<LibraryFileMinimal> UpcomingFiles { get; set; }
    
    /// <summary>
    /// Gets or sets the most successfully processed files
    /// </summary>
    [JsonPropertyName("fs")]
    public List<LibraryFileMinimal> RecentlyFinished { get; set; }
    
    /// <summary>
    /// Gets or sets the most recent failed files
    /// </summary>
    [JsonPropertyName("ff")]
    public List<LibraryFileMinimal> FailedFiles { get; set; }

    /// <summary>
    /// Gets or sets the top savings of all time
    /// </summary>
    [JsonPropertyName("tsa")]
    public List<LibraryFileMinimal> TopSavingsAll { get; set; }

    /// <summary>
    /// Gets or sets the top savings for the last 31 days
    /// </summary>
    [JsonPropertyName("ts31")]
    public List<LibraryFileMinimal> TopSavings31Days { get; set; }
    
    /// <summary>
    /// Gets or sets the file counts
    /// </summary>
    [JsonPropertyName("fc")]
    public  List<LibraryStatus> LibraryFileCounts { get; set; }
    
    /// <summary>
    /// Gets or sets all the flow elements in the system
    /// </summary>
    [JsonPropertyName("fe")]
    public List<FlowElement> FlowElements { get; set; }
}