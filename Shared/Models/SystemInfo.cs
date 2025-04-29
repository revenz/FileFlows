namespace FileFlows.Shared.Models;

/// <summary>
/// Gets the system information about FileFlows
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets the amount of memory used by FileFlows
    /// </summary>
    [JsonPropertyName("mu")]
    public long[] MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets the how much CPU is used by FileFlows
    /// </summary>
    [JsonPropertyName("cu")]
    public float[] CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets if the system is paused
    /// </summary>
    [JsonPropertyName("p")]
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets when the system is paused until
    /// </summary>
    [JsonPropertyName("pu")]
    public DateTime PausedUntil { get; set; }

    /// <summary>
    /// Gets the current time on the server
    /// </summary>
    [JsonPropertyName("ct")]
    public DateTime CurrentTime => DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the status of the system
    /// </summary>
    [JsonPropertyName("ns")]
    public List<NodeStatus> NodeStatuses { get; set; }
    
}

/// <summary>
/// Node statuse
/// </summary>
public class NodeStatus
{
    /// <summary>
    /// Gets or sets the UID of the node
    /// </summary>
    [JsonPropertyName("u")]
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets or sets the name of the node
    /// </summary>
    [JsonPropertyName("n")]
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets if this node is out of schedule
    /// </summary>
    [JsonPropertyName("oos")]
    public bool OutOfSchedule { get; set; }
    /// <summary>
    /// Gets or sets when the node will be in schedule in UTC
    /// </summary>
    [JsonPropertyName("sr")]
    public DateTime? ScheduleResumesAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the version of the node
    /// </summary>
    [JsonPropertyName("v")]
    public string Version { get; set; }
    /// <summary>
    /// Gets or sets if the node is enabled
    /// </summary>
    [JsonPropertyName("e")]
    public bool Enabled { get; set; }
}