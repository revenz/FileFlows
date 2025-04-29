namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Plugin related data
/// </summary>
/// <param name="feService">the front end service</param>
public class PluginHandler(FrontendService feService)
{
    public List<PluginInfoModel> Plugins { get; private set; } = [];
    
    /// <summary>
    /// Event raised when the node status is updated
    /// </summary>
    public event Action<List<PluginInfoModel>> PluginsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Plugins = data.Plugins;
        feService.Registry.Register<List<PluginInfoModel>>("Plugins", (ed) =>
        {
            Plugins = ed;
            PluginsUpdated?.Invoke(ed);
        });
    }
}