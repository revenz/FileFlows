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
    public List<LibraryFileMinimal> FileQueue { get; private set; } = [];
    /// <summary>
    /// Gets the most successfully processed files
    /// </summary>
    public List<LibraryFileMinimal> Successful { get; private set; }
    /// <summary>
    /// Gets the total successfully processed files
    /// </summary>
    public int SuccessfulTotal { get; private set; }
    /// <summary>
    /// Gets the failed files
    /// </summary>
    public List<LibraryFileMinimal> FailedFiles { get; private set; }
    /// <summary>
    /// Gets the total recent failed files
    /// </summary>
    public int FailedFilesTotal { get; private set; }
    /// <summary>
    /// Gets or sets the top savings of all time
    /// </summary>
    public List<LibraryFileMinimal> TopSavingsAll { get; private set; }
    /// <summary>
    /// Gets or sets the top savings for the last 31 days
    /// </summary>
    public List<LibraryFileMinimal> TopSavings31Days { get; private set; }
    /// <summary>
    /// Gets or sets the files that are on hold
    /// </summary>
    public List<LibraryFileMinimal> OnHold { get; private set; }
    /// <summary>
    /// Gets or sets the files that are out of schedule
    /// </summary>
    public List<LibraryFileMinimal> OutOfSchedule { get; private set; }
    /// <summary>
    /// Gets or sets the files that are disabled
    /// </summary>
    public List<LibraryFileMinimal> Disabled { get; private set; }
    
    /// <summary>
    /// Gets or sets the file counts
    /// </summary>
    public List<LibraryStatus> LibraryFileCounts { get; set; }
    
    /// <summary>
    /// Event raised when the node status is updated
    /// </summary>
    public event Action<List<LibraryStatus>> LibraryFileCountsUpdated; 
    
    /// <summary>
    /// Event raised when the file queue is updated
    /// </summary>
    public event Action<List<LibraryFileMinimal>> FileQueueUpdated; 
    
    /// <summary>
    /// Event raised when the on hold queue is updated
    /// </summary>
    public event Action<List<LibraryFileMinimal>> OnHoldUpdated; 
    
    /// <summary>
    /// Event raised when the on out of schedule queue is updated
    /// </summary>
    public event Action<List<LibraryFileMinimal>> OutOfScheduleUpdated; 
    
    /// <summary>
    /// Event raised when the disabled files are updated
    /// </summary>
    public event Action<List<LibraryFileMinimal>> DisabledUpdated; 
    
    /// <summary>
    /// Called when recently finished files have been updated
    /// </summary>
    public event Action<ListAndCount> SuccessfulUpdated;
    /// <summary>
    /// Called when failed files have been updated
    /// </summary>
    public event Action<ListAndCount> FailedFilesUpdated;

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        FileQueue = data.FileQueue;
        Successful = data.Successful;
        SuccessfulTotal = data.SuccessfulTotal;
        FailedFiles = data.FailedFiles;
        FailedFilesTotal = data.FailedFilesTotal;
        TopSavingsAll = data.TopSavingsAll;
        TopSavings31Days = data.TopSavings31Days;
        LibraryFileCounts = data.LibraryFileCounts;
        OnHold = data.OnHold;
        OutOfSchedule = data.OutOfScheduleFiles;
        Disabled = data.DisabledFiles;
        
        feService.Registry.Register<List<LibraryStatus>>(nameof(LibraryFileCounts), (ed) =>
        {
            LibraryFileCounts = ed;
            LibraryFileCountsUpdated?.Invoke(ed);
        });
        feService.Registry.Register<List<LibraryFileMinimal>>("FileQueue", (ed) =>
        {
            FileQueue = ed;
            FileQueueUpdated?.Invoke(ed);
        });

        // feService.Registry.Register<Guid[]>("FilesDeleted", (uids) =>
        // {
        //     if (FailedFiles.RemoveAll(x => uids.Contains(x.Uid)) > 0)
        //         FailedFilesUpdated?.Invoke(FailedFiles);
        //     if (Successful.RemoveAll(x => uids.Contains(x.Uid)) > 0)
        //         SuccessfulUpdated?.Invoke(Successful);
        //     if (FileQueue.RemoveAll(x => uids.Contains(x.Uid)) > 0)
        //         FileQueueUpdated?.Invoke(FileQueue);
        //     if (OnHold.RemoveAll(x => uids.Contains(x.Uid)) > 0)
        //         OnHoldUpdated?.Invoke(OnHold);
        // });
        feService.Registry.Register<List<LibraryFileMinimal>>("OnHold", (files) =>
        {
            OnHold = files;
            OnHoldUpdated?.Invoke(OnHold);
        });
        feService.Registry.Register<List<LibraryFileMinimal>>("OutOfSchedule", (files) =>
        {
            OutOfSchedule = files;
            OutOfScheduleUpdated?.Invoke(OutOfSchedule);
        });
        feService.Registry.Register<List<LibraryFileMinimal>>("DisabledFiles", (files) =>
        {
            Disabled = files;
            DisabledUpdated?.Invoke(Disabled);
        });
        feService.Registry.Register<ListAndCount>("ProcessedFiles", (lat) =>
        {
            Successful = lat.Files;
            SuccessfulTotal = lat.Total;
            SuccessfulUpdated?.Invoke(lat);
        });
        feService.Registry.Register<ListAndCount>("FailedFiles", (lat) =>
        {
            FailedFiles = lat.Files;
            FailedFilesTotal = lat.Total;
            FailedFilesUpdated?.Invoke(lat);
        });
    }

    /// <summary>
    /// List of files and their counts
    /// </summary>
    public class ListAndCount
    {
        /// <summary>
        /// Gets or sets the total files
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// Gets or sets the files
        /// </summary>
        public List<LibraryFileMinimal> Files { get; set; }
    }
}