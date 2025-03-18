namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for DockerMods
/// </summary>
/// <param name="feService">the frontend service</param>
public class DockerModHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets a basic list of DockerMods
    /// </summary>
    public Dictionary<Guid, string> DockerModList { get; private set; }
    /// <summary>
    /// Gets or sets a list of DockerMods
    /// </summary>
    public List<DockerMod> DockerMods { get; private set; }
    
    /// <summary>
    /// Event raised when the DockerMods is updated
    /// </summary>
    public event Action<List<DockerMod>> DockerModsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        DockerModList = data.DockerMods.ToDictionary(x => x.Uid, x => x.Name);
        DockerMods = data.DockerMods;
        
        feService.Registry.Register<List<DockerMod>>("DockerModsUpdated", (ed) =>
        {
            DockerModList = ed.ToDictionary(x => x.Uid, x => x.Name);
            DockerMods = ed;
            DockerModsUpdated?.Invoke(ed);
        });
    }
}