namespace FileFlows.Shared.Models;

/// <summary>
/// The FileFlows repository
/// </summary>
public class FileFlowsRepository
{
    /// <summary>
    /// Gets or sets the shared scripts
    /// </summary>
    public List<RepositoryObject> SharedScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the system scripts
    /// </summary>
    public List<RepositoryObject> SystemScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the webhook scripts
    /// </summary>
    public List<RepositoryObject> WebhookScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the flow scripts
    /// </summary>
    public List<RepositoryObject> FlowScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the function scripts
    /// </summary>
    public List<RepositoryObject> FunctionScripts { get; set; } = new ();
    /// <summary>
    /// Gets or sets the sub flows
    /// </summary>
    public List<RepositoryObject> SubFlows { get; set; } = new ();
    /// <summary>
    /// Gets or sets the DockerMods
    /// </summary>
    public List<RepositoryObject> DockerMods { get; set; } = new ();
}

/// <summary>
/// A Remote script
/// </summary>
public class RepositoryObject
{
    /// <summary>
    /// Gets or sets the path of the script
    /// </summary>
    public string? Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows this object requires
    /// </summary>
    public Version? MinimumVersion { get; set; }
    
    /// <summary>
    /// Gets or sets an optional UID of the object
    /// </summary>
    public Guid? Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the author if available
    /// </summary>
    public string Author { get; set; }
    
    /// <summary>
    /// Gets or sets a list of sub flows this object depends on
    /// </summary>
    public List<Guid>? SubFlows { get; set; }
    
    /// <summary>
    /// Gets or sets an optional Icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets if this is a default item
    /// </summary>
    public bool? Default { get; set; }
}