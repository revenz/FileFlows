namespace FileFlows.Shared.Models;

/// <summary>
/// File Overview Data
/// </summary>
public class FileOverviewData
{
    /// <summary>
    /// Gets or sets the data for the last 24 hours.
    /// </summary>
    [JsonPropertyName("l1")]
    public Dictionary<DateTime, DashboardFileData> Last24Hours { get; init; }
    /// <summary>
    /// Gets or sets the data for the last 7 days.
    /// </summary>
    [JsonPropertyName("l2")]
    public Dictionary<DateTime, DashboardFileData> Last7Days{ get; init; }
    /// <summary>
    /// Gets or sets the data for the last 31 days.
    /// </summary>
    [JsonPropertyName("l3")]
    public Dictionary<DateTime, DashboardFileData> Last31Days{ get; init; }
}

/// <summary>
/// Represents file data statistics such as file count and file size.
/// </summary>
public record DashboardFileData
{
    /// <summary>
    /// Gets or sets the number of files processed.
    /// </summary>
    [JsonPropertyName("c")]
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the total storage saved
    /// </summary>
    [JsonPropertyName("s")]
    public long StorageSaved { get; set; }
    
    /// <summary>
    /// Gets or sets the final storage
    /// </summary>
    [JsonPropertyName("f")]
    public long FinalStorage { get; set; }
    /// <summary>
    /// Gets or sets the original storage
    /// </summary>
    [JsonPropertyName("o")]
    public long OriginalStorage { get; set; }
}