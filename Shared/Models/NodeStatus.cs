namespace FileFlows.Shared.Models;

/// <summary>
/// The Node status summary
/// </summary>
public class NodeStatusSummary : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the name of the item
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets if this node is out of schedule
    /// </summary>
    public bool OutOfSchedule { get; set; }
    /// <summary>
    /// Gets or sets when the node will be in schedule in UTC
    /// </summary>
    public DateTime? ScheduleResumesAtUtc { get; set; }
    
    /// <summary>
    /// Gets or sets the number of flow runners
    /// </summary>
    public int FlowRunners { get; set; }

    /// <summary>
    /// Gets or sets the type of operating system this node is running on
    /// </summary>
    public OperatingSystemType OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the architecture type
    /// </summary>
    public ArchitectureType Architecture { get; set; }

    /// <summary>
    /// Gets or sets the FileFlows version of this node
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets if this node is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the address this node is located at, hostname or ip address
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the priority of the processing node
    /// Higher the value, the higher the priority 
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the current status of the node
    /// </summary>
    public ProcessingNodeStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the hardware information
    /// </summary>
    public HardwareInfo? HardwareInfo { get; set; }
}