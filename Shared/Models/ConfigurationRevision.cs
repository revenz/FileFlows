using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// The FileFlows configuration as a specific revision
/// </summary>
public class ConfigurationRevision
{
    /// <summary>
    /// Gets or sets the Revision
    /// </summary>
    public int Revision { get; set; }
    
    /// <summary>
    /// Gets the delay between requesting a new file if a file can be processed instantly
    /// </summary>
    public int DelayBetweenNextFile { get; set; }

    /// <summary>
    /// Gets or sets the maximum nodes that can be executed
    /// </summary>
    public int MaxNodes { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a license level
    /// </summary>
    public FileFlows.Common.LicenseLevel LicenseLevel { get; set; }
    
    /// <summary>
    /// Gets or sets if remote files are allowed
    /// </summary>
    public bool AllowRemote { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files should be kept if the flow fails
    /// </summary>
    public bool KeepFailedFlowTempFiles { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files should be used when moving or copying files
    /// </summary>
    public bool DontUseTempFilesWhenMovingOrCopying { get; set; }
    
    /// <summary>
    /// Gets or sets the resources 
    /// </summary>
    public List<Resource> Resources { get; set; }

    /// <summary>
    /// Gets or sets the system variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; }

    private Dictionary<Guid, string> _Tags = [];

    /// <summary>
    /// Gets or sets the tags
    /// </summary>
    public Dictionary<Guid, string> Tags
    {
        get => _Tags; 
        set => _Tags = value ?? [];
    } 
    
    /// <summary>
    /// Gets or sets the path to the manual library
    /// </summary>
    public string ManualLibraryPath { get; set; }

    /// <summary>
    /// Gets or sets the shared Scripts
    /// </summary>
    public List<Script> SharedScripts { get; set; }
    
    /// <summary>
    /// Gets or sets the flow Scripts
    /// </summary>
    public List<Script> FlowScripts { get; set; }
    
    /// <summary>
    /// Gets or sets the system Scripts
    /// </summary>
    public List<Script> SystemScripts { get; set; }

    /// <summary>
    /// Gets or sets all the flows in the system
    /// </summary>
    public List<Flow> Flows { get; set; }

    /// <summary>
    /// Gets or sets all the libraries in the system
    /// </summary>
    public List<Library> Libraries { get; set; }

    /// <summary>
    /// Gets or sets the plugin settings which is dictionary of the Plugin name and the settings JSON
    /// </summary>
    public Dictionary<string, string> PluginSettings { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of plugins in use
    /// </summary>
    public List<string> Plugins { get; set; }
    
    /// <summary>
    /// Gets or sets a list of plugin names in use
    /// </summary>
    public List<string> PluginNames { get; set; }
    
    /// <summary>
    /// Gets or sets a list of DockerMods in use
    /// </summary>
    public List<DockerMod> DockerMods { get; set; }
}