using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// The run file arguments
/// </summary>
public class RunFileArguments
{
    /// <summary>
    /// Gets or sets the library file
    /// </summary>
    public LibraryFile LibraryFile { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the UID of the flow to execute
    /// </summary>
    public Guid FlowUid { get; set; }
    
    /// <summary>
    /// Gets or sets if this node cna run a pre-execute check
    /// </summary>
    public bool CanRunPreExecuteCheck { get; set; }
    
    /// <summary>
    /// Gets or sets the configuration revision
    /// </summary>
    public int ConfigRevision { get; set; }
    
    /// <summary>
    /// Gets or sets if failed files should be kept
    /// </summary>
    public bool KeepFailedFiles { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum runners on this node
    /// </summary>
    public int MaxRunnersOnNode { get; set; }
}