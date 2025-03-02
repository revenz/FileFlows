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

/// <summary>
/// The parameters to send when registering a ndoe
/// </summary>
public class NodeRegisterParameters
{
    /// <summary>
    /// Gets the hostname
    /// </summary>
    public string Hostname { get; init; }
    /// <summary>
    /// Gets the config revision
    /// </summary>
    public int ConfigRevision { get; init; }
    /// <summary>
    /// Gets the hardware info
    /// </summary>
    public HardwareInfo? HardwareInfo { get; init; }
    /// <summary>
    /// Gets or sets the temp path
    /// </summary>
    public string TempPath { get; set; }
    /// <summary>
    /// Gets or sets any node mappings
    /// </summary>
    public List<KeyValuePair<string, string>> Mappings { get; init; } 
    /// <summary>
    /// Gets the version of the node
    /// </summary>
    public string Version { get; init; }
    /// <summary>
    /// Gets the operating system type
    /// </summary>
    public OperatingSystemType OperatingSystem { get; init; }
    /// <summary>
    /// Gets tthe nodes architecture
    /// </summary>
    public ArchitectureType Architecture { get; init; }
}