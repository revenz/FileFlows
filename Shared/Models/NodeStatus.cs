namespace FileFlows.Shared.Models;

/// <summary>
/// The Node status summary
/// </summary>
public class NodeStatusSummary : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    [JsonPropertyName("u")]
    public Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the name of the item
    /// </summary>
    [JsonPropertyName("n")]
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets if this node is out of schedule
    /// </summary>
    [JsonPropertyName("o")]
    public bool OutOfSchedule { get; set; }
    /// <summary>
    /// Gets or sets when the node will be in schedule in UTC
    /// </summary>
    [JsonPropertyName("s")]
    public DateTime? ScheduleResumesAtUtc { get; set; }
    
    /// <summary>
    /// Gets or sets the number of flow runners
    /// </summary>
    [JsonPropertyName("r")]
    public int FlowRunners { get; set; }

    /// <summary>
    /// Gets or sets the type of operating system this node is running on
    /// </summary>
    [JsonPropertyName("os")]
    public OperatingSystemType OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the architecture type
    /// </summary>
    [JsonPropertyName("a")]
    public ArchitectureType Architecture { get; set; }

    /// <summary>
    /// Gets or sets the FileFlows version of this node
    /// </summary>
    [JsonPropertyName("v")]
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    [JsonPropertyName("i")]
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets if this node is enabled
    /// </summary>
    [JsonPropertyName("e")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the address this node is located at, hostname or ip address
    /// </summary>
    [JsonPropertyName("ad")]
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the priority of the processing node
    /// Higher the value, the higher the priority 
    /// </summary>
    [JsonPropertyName("p")]
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the current status of the node
    /// </summary>
    [JsonPropertyName("z")]
    public ProcessingNodeStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the status message
    /// </summary>
    [JsonPropertyName("m")]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the hardware information
    /// </summary>
    [JsonPropertyName("h")]
    public HardwareInfo? HardwareInfo { get; set; }
}