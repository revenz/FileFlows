namespace FileFlows.ServerShared.Models;

/// <summary>
/// Parameters passed to flow runner
/// </summary>
public class RunnerParameters
{
    /// <summary>
    /// Gets or sets the UID of the runner
    /// </summary>
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets or sets the UID of the node
    /// </summary>
    public Guid NodeUid { get; set; }
    /// <summary>
    /// Gets or sets the UID of the node to include in the remote calls
    /// </summary>
    public Guid RemoteNodeUid { get; set; }
    /// <summary>
    /// Gets or sets the UID of the library file
    /// </summary>
    public Guid LibraryFile { get; set; }
    /// <summary>
    /// Gets or sets the temporary path
    /// </summary>
    public string TempPath { get; set; } = null!;
    /// <summary>
    /// Gets or sets the configuration path
    /// </summary>
    public string ConfigPath { get; set; } = null!;
    /// <summary>
    /// Gets or sets the base URL for the FileFlows server
    /// </summary>
    public string BaseUrl { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Access token 
    /// </summary>
    public string AccessToken { get; set; } = null!;
    /// <summary>
    /// Gets or sets if running inside docker 
    /// </summary>
    public bool IsDocker { get; set; }
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string? Hostname { get; set; }
    /// <summary>
    /// Gets or sets if is the internal processing node 
    /// </summary>
    public bool IsInternalServerNode { get; set; }
    
    #if(DEBUG)
    /// <summary>
    /// Gets or sets a forced name for the runner temp path, this is used in debug so we dont have an issue
    /// loading the DLLs mulitple times.
    /// </summary>
    public string? RunnerTempPath { get; set; }
#endif
}