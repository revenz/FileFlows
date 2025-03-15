namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for files
/// </summary>
public class FileHandler
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
    }
}