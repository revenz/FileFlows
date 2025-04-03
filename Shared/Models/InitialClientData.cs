namespace FileFlows.Shared.Models;

/// <summary>
/// The data that is sent to connecting clients (browsers) when first loaded
/// This populates all the cached data for them, so future pages do no need to make heaps of requests
/// </summary>
public class InitialClientData
{
    /// <summary>
    /// Gets or sets a list of libraries
    /// </summary>
    [JsonPropertyName("l")]
    public List<LibraryListModel> Libraries { get; set; }
    
    /// <summary>
    /// Gets or sets a basic list of flows
    /// </summary>
    [JsonPropertyName("f")]
    public List<FlowListModel> Flows { get; set; }
    
    /// <summary>
    /// Gets or sets all the Plugins the system
    /// </summary>
    [JsonPropertyName("u")]
    public List<PluginInfoModel> Plugins { get; set; }
    
    /// <summary>
    /// Gets or sets the users profile
    /// </summary>
    [JsonPropertyName("p")]
    public Profile Profile { get; set; }
    
    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    [JsonPropertyName("ps")]
    public int PageSize { get; set; }
    
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
    [JsonPropertyName("pf")]
    public List<ProcessingLibraryFile> Processing { get; set; }

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
    /// Gets or sets the file queue
    /// </summary>
    [JsonPropertyName("fq")]
    public List<LibraryFileMinimal> FileQueue { get; set; }
    
    /// <summary>
    /// Gets or sets the successfully processed files
    /// </summary>
    [JsonPropertyName("sf")]
    public List<LibraryFileMinimal> Successful { get; set; }
    
    /// <summary>
    /// Gets or sets the total successfully processed files
    /// </summary>
    [JsonPropertyName("sft")]
    public int SuccessfulTotal { get; set; }
    
    /// <summary>
    /// Gets or sets the most recent failed files
    /// </summary>
    [JsonPropertyName("ff")]
    public List<LibraryFileMinimal> FailedFiles { get; set; }
    
    /// <summary>
    /// Gets or sets the total failed files
    /// </summary>
    [JsonPropertyName("fft")]
    public int FailedFilesTotal { get; set; }

    /// <summary>
    /// Gets or sets the top savings of all time
    /// </summary>
    [JsonPropertyName("tsa")]
    public List<LibraryFileMinimal> TopSavingsAll { get; set; }

    /// <summary>
    /// Gets or sets the on hold files
    /// </summary>
    [JsonPropertyName("oh")]
    public List<LibraryFileMinimal> OnHold { get; set; }

    /// <summary>
    /// Gets or sets the on hold files
    /// </summary>
    [JsonPropertyName("df")]
    public List<LibraryFileMinimal> DisabledFiles { get; set; }

    /// <summary>
    /// Gets or sets the out of schedule files
    /// </summary>
    [JsonPropertyName("os")]
    public List<LibraryFileMinimal> OutOfScheduleFiles { get; set; }

    /// <summary>
    /// Gets or sets the top savings for the last 31 days
    /// </summary>
    [JsonPropertyName("ts31")]
    public List<LibraryFileMinimal> TopSavings31Days { get; set; }
    
    /// <summary>
    /// Gets or sets the file counts
    /// </summary>
    [JsonPropertyName("fc")]
    public List<LibraryStatus> LibraryFileCounts { get; set; }
    
    /// <summary>
    /// Gets or sets all the flow elements in the system
    /// </summary>
    [JsonPropertyName("fe")]
    public List<FlowElement> FlowElements { get; set; }
    
    /// <summary>
    /// Gets or sets all the scripts in the system
    /// </summary>
    [JsonPropertyName("sc")]
    public List<Script> Scripts { get; set; }
    
    /// <summary>
    /// Gets or sets all the variables in the system
    /// </summary>
    [JsonPropertyName("v")]
    public List<Variable> Variables { get; set; }
    
    /// <summary>
    /// Gets or sets all the DockerMods the system
    /// </summary>
    [JsonPropertyName("dm")]
    public List<DockerMod> DockerMods { get; set; }
    
    /// <summary>
    /// Gets or sets the report definitions
    /// </summary>
    [JsonPropertyName("rd")]
    public List<ReportDefinition> ReportDefinitions { get; set; }
    
    /// <summary>
    /// Gets or sets the scheduled reports
    /// </summary>
    [JsonPropertyName("sr")]
    public List<ScheduledReport> ScheduledReports { get; set; }
}