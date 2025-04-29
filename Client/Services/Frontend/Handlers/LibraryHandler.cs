namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for libraries
/// </summary>
/// <param name="feService">the frontend service</param>
public class LibraryHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets a basic list of libraries
    /// </summary>
    public Dictionary<Guid, string> LibraryList { get; private set; }
    /// <summary>
    /// Gets or sets a list of libraries
    /// </summary>
    public List<LibraryListModel> Libraries { get; private set; }
    
    /// <summary>
    /// Event raised when the libraries is updated
    /// </summary>
    public event Action<List<LibraryListModel>> LibrariesUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        LibraryList = data.Libraries.ToDictionary(x => x.Uid, x => x.Name);
        Libraries = data.Libraries;
        
        feService.Registry.Register<List<LibraryListModel>>(nameof(LibraryList), (ed) =>
        {
            LibraryList = ed.ToDictionary(x => x.Uid, x => x.Name);
            Libraries = ed;
            LibrariesUpdated?.Invoke(ed);
        });
    }
}