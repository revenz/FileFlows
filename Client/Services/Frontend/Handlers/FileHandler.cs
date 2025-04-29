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
    public List<LibraryFileMinimal> Unprocessed { get; private set; } = [];
    /// <summary>
    /// Gets the most successfully processed files
    /// </summary>
    public List<LibraryFileMinimal> Processed { get; private set; }
    /// <summary>
    /// Gets the processing files
    /// </summary>
    public List<ProcessingLibraryFile> Processing { get; private set; }
    /// <summary>
    /// Gets the total successfully processed files
    /// </summary>
    public int ProcessedTotal { get; private set; }
    /// <summary>
    /// Gets the total unprocessed files
    /// </summary>
    public int UnprocessedTotal { get; private set; }
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
    /// Event raised when the unprocessed list is updated
    /// </summary>
    public event Action<List<LibraryFileMinimal>, int> UnprocessedUpdated; 
    
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
    public event Action<ListAndCount<LibraryFileMinimal>> SuccessfulUpdated;
    /// <summary>
    /// Called when failed files have been updated
    /// </summary>
    public event Action<ListAndCount<LibraryFileMinimal>> FailedFilesUpdated;
    /// <summary>
    /// Called when processing files are updated
    /// </summary>
    public event Action<List<ProcessingLibraryFile>> ProcessingUpdated;

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Unprocessed = data.Unprocessed;
        UnprocessedTotal = data.UnprocessedTotal;
        Processed = data.Successful;
        ProcessedTotal = data.SuccessfulTotal;
        FailedFiles = data.FailedFiles;
        FailedFilesTotal = data.FailedFilesTotal;
        TopSavingsAll = data.TopSavingsAll;
        TopSavings31Days = data.TopSavings31Days;
        OnHold = data.OnHold;
        OutOfSchedule = data.OutOfScheduleFiles;
        Disabled = data.DisabledFiles;
        Processing = data.Processing;
        
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("Unprocessed", (ed) =>
        {
            Unprocessed = ed.Data;
            UnprocessedTotal = ed.Total;
            UnprocessedUpdated?.Invoke(ed.Data, UnprocessedTotal);
        });
        feService.Registry.Register<ListAndCount<ProcessingLibraryFile>>("Processing", (ed) =>
        {
            Processing = ed.Data;
            ProcessingUpdated?.Invoke(ed.Data);
        });
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("OnHold", (files) =>
        {
            OnHold = files.Data;
            OnHoldUpdated?.Invoke(OnHold);
        });
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("OutOfSchedule", (files) =>
        {
            OutOfSchedule = files.Data;
            OutOfScheduleUpdated?.Invoke(OutOfSchedule);
        });
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("Disabled", (files) =>
        {
            Disabled = files.Data;
            DisabledUpdated?.Invoke(Disabled);
        });
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("Processed", (lat) =>
        {
            Processed = lat.Data;
            ProcessedTotal = lat.Total;
            SuccessfulUpdated?.Invoke(lat);
        });
        feService.Registry.Register<ListAndCount<LibraryFileMinimal>>("ProcessingFailed", (lat) =>
        {
            FailedFiles = lat.Data;
            FailedFilesTotal = lat.Total;
            FailedFilesUpdated?.Invoke(lat);
        });
    }

    /// <summary>
    /// List of files and their counts
    /// </summary>
    public class ListAndCount<T>
    {
        /// <summary>
        /// Gets or sets the total files
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// Gets or sets the files
        /// </summary>
        public List<T> Data { get; set; }
    }
}