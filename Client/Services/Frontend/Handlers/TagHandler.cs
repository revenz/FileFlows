namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for tags
/// </summary>
/// <param name="feService">the frontend service</param>
public class TagHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets a basic list of tags
    /// </summary>
    public Dictionary<Guid, string> TagList { get; private set; }
    /// <summary>
    /// Gets or sets a list of tags
    /// </summary>
    public List<Tag> Tags { get; private set; }
    
    /// <summary>
    /// Event raised when the tags is updated
    /// </summary>
    public event Action<List<Tag>> TagsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        TagList = data.Tags.ToDictionary(x => x.Uid, x => x.Name);
        Tags = data.Tags;
        
        feService.Registry.Register<List<Tag>>("TagsUpdated", (ed) =>
        {
            TagList = ed.ToDictionary(x => x.Uid, x => x.Name);
            Tags = ed;
            TagsUpdated?.Invoke(ed);
        });
    }
}