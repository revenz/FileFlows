namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for files
/// </summary>
/// <param name="feService">the frontend service</param>
public class FileHandler(FrontendService feService)
{
    /// <summary>
    /// Gets the upcoming files to process
    /// </summary>
    public List<LibraryFileMinimal> UpcomingFiles { get; private set; }
    /// <summary>
    /// Gets the most successfully processed files
    /// </summary>
    public List<LibraryFileMinimal> RecentlyFinished { get; private set; }
    /// <summary>
    /// Gets the most recent failed files
    /// </summary>
    public List<LibraryFileMinimal> FailedFiles { get; private set; }
    /// <summary>
    /// Gets or sets the top savings of all time
    /// </summary>
    public List<LibraryFileMinimal> TopSavingsAll { get; private set; }
    /// <summary>
    /// Gets or sets the top savings for the last 31 days
    /// </summary>
    public List<LibraryFileMinimal> TopSavings31Days { get; private set; }
    
    /// <summary>
    /// Gets or sets the file counts
    /// </summary>
    public List<LibraryStatus> LibraryFileCounts { get; set; }
    
    /// <summary>
    /// Event raised when the node status is updated
    /// </summary>
    public event Action<List<LibraryStatus>> LibraryFileCountsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        UpcomingFiles = data.UpcomingFiles;
        RecentlyFinished = data.RecentlyFinished;
        FailedFiles = data.FailedFiles;
        TopSavingsAll = data.TopSavingsAll;
        TopSavings31Days = data.TopSavings31Days;
        LibraryFileCounts = data.LibraryFileCounts;
        
        feService.Registry.Register<List<LibraryStatus>>(nameof(LibraryFileCounts), (ed) =>
        {
            Logger.Instance.ILog("LibraryFileCounts", LibraryFileCounts);
            LibraryFileCounts = ed;
            LibraryFileCountsUpdated?.Invoke(ed);
        });
    }
}