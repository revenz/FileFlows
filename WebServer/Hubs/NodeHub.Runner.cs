using FileFlows.LibraryUtils;
using FileFlows.Services.FileProcessing;
using NodeService = FileFlows.Services.NodeService;
using ServiceLoader = FileFlows.Services.ServiceLoader;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Runner methods from the node
/// </summary>
public partial class NodeHub
{
    /// <summary>
    /// Tells the server to ignore the specified path when scanning
    /// </summary>
    /// <param name="path">the Path to ignore</param>
    public void LibraryIgnorePath(string path)
    {
        try
        {
            Logger.Instance.ILog("Ignoring Path from library scanning: " + path);
            WatchedLibraryNew.IgnorePath(path);
        }
        catch(Exception ex)
        {
            Logger.Instance.ELog("Failed to ignore path from library scanning: " + path + " => " + ex.Message);
            // Ignored
        }
    }

    /// <summary>
    /// Tests if a file exists on the server
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="isDirectory">if it is a directory</param>
    /// <returns>true if exists, otherwise false</returns>
    public async Task<bool> ExistsOnServer(string path, bool isDirectory)
    {
        try
        {
            return await ServiceLoader.Load<LibraryFileService>().ExistsOnServer(path, isDirectory);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to exist on server: " + path + " => " + ex);
            throw;
        }
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="libraryFile">the library file </param>
    /// <returns>true that it was received</returns>
    public async Task<bool> FileStartProcessing(LibraryFile libraryFile)
    {
        _logger.ILog($"NodeHub.FileStartProcessing: {libraryFile.Name}");
        var sorter = ServiceLoader.Load<FileSorterService>();

        // the library file passed in isnt the cached instance, so get one from tdb
        var file = sorter.GetFile(libraryFile.Uid);
        if (file == null)
        {
            _logger.ILog("FileStartProcessing: file not found: " + libraryFile.Name);
            return false; // shouldn't happen
        }

        file.Status = FileStatus.Processing;
        file.Node = libraryFile.Node;
        file.Flow = libraryFile.Flow;
        file.ProcessingStarted = DateTime.UtcNow;
        
        sorter.StartProcessing(file);
        
        var lfService = ServiceLoader.Load<LibraryFileService>();
        await lfService.Update(file);
        return true;
    }

    /// <summary>
    /// Called when the file finishes processing
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <param name="log">the complete log of the file processing</param>
    /// <returns>true that it was received</returns>
    public async Task<bool> FileFinishProcessing(LibraryFile libraryFile, string log)
    {
        _logger.ILog($"NodeHub.FileFinishProcessing: {libraryFile.Name}");
        var libraryFileService = ServiceLoader.Load<LibraryFileService>();
        await libraryFileService.FinishProcessing(libraryFile, log);
        
        var nodeService = ServiceLoader.Load<NodeService>();
        nodeService.FinishProcessing(libraryFile);
        var sorter = ServiceLoader.Load<FileSorterService>();
        sorter.FinishProcessing(libraryFile);

        return true;
    }
    
    /// <summary>
    /// Prepends the text to the log file on the server
    /// </summary>
    /// <param name="libFileUid">the UID of the file</param>
    /// <param name="lines">the lines of the log</param>
    /// <param name="overwrite">if the file should be overwritten or appended to</param>
    public async Task FileLogAppend(Guid libFileUid, string lines, bool overwrite)
        => await LibraryFileLogHelper.AppendToLog(libFileUid, lines, overwrite);
    
    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="binaryData">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    public async Task SetThumbnail(Guid libraryFileUid, byte[] binaryData)
        => await ServiceLoader.Load<LibraryFileService>().SetThumbnail(libraryFileUid, binaryData);
    
    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public async Task RecordRunningTotal(string name, string value)
        => await ServiceLoader.Load<StatisticService>().RecordRunningTotal(name, value);
    
    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public async Task RecordAverage(string name, int value)
        => await ServiceLoader.Load<StatisticService>().RecordAverage(name, value);

    /// <summary>
    /// Sends an email to the provided recipients
    /// </summary>
    /// <param name="to">a list of email addresses</param>
    /// <param name="subject">the subject of the email</param>
    /// <param name="body">the plain text body of the email</param>
    public async Task<string> SendEmail(string[] to, string subject, string body)
    {
        try
        {
            var result = await ServiceLoader.Load<IEmailService>().Send(to, subject, body);
            if (result.Failed(out var error))
                return error;
            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }


    /// <summary>
    /// Retrieves a cached item as a raw JSON string.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>A task representing the asynchronous operation, containing the JSON string if found; otherwise, <c>null</c>.</returns>
    public async Task<string?> CacheGetJsonAsync(string key)
        => await ServiceLoader.Load<DistributedCacheService>().GetJsonAsync(key);
    
    /// <summary>
    /// Stores a JSON string in the cache with an optional expiration time.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="json">The JSON string to store.</param>
    /// <param name="expiration">The optional expiration time. If not provided, a default expiration is used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CacheStoreJsonAsync(string key, string json, TimeSpan? expiration)
        => await ServiceLoader.Load<DistributedCacheService>().StoreJsonAsync(key, json, expiration);
    
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    public async Task RecordNotification(NotificationSeverity severity, string title, string? message = null)
        => await ServiceLoader.Load<INotificationService>().Record(severity, title, message);
    
}