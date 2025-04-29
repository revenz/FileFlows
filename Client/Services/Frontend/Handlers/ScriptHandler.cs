namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Plugin related data
/// </summary>
/// <param name="feService">the front end service</param>
public class ScriptHandler(FrontendService feService)
{
    public List<Script> Scripts { get; private set; } = [];
    
    /// <summary>
    /// Event raised when the scripts are updated
    /// </summary>
    public event Action<List<Script>> ScriptsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Scripts = data.Scripts;
        feService.Registry.Register<List<Script>>("Scripts", (ed) =>
        {
            Scripts = ed;
            ScriptsUpdated?.Invoke(ed);
        });
    }
}