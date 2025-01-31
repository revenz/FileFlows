using FileFlows.Common;
using FileFlows.Exceptions;
using FileFlows.Helpers;
using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.Plugin.Services;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Remote File Service that connects to the FileFlows server remotely
/// </summary>
public class RemoteFileService : IFileService
{
    /// <summary>
    /// Gets the path separator
    /// </summary>
    public char PathSeparator { get; init; }
    /// <summary>
    /// Gets or sets the replace variables delegate
    /// </summary>
    public ReplaceVariablesDelegate? ReplaceVariables { get; set; }
    /// <summary>
    /// Gets or sets the logger
    /// </summary>
    public ILogger? Logger { get; set; }

    private readonly Guid executorUid;
    private readonly string serverUrl;
    private readonly string tempPath;
    private readonly ILogger logger;
    private readonly LocalFileService _localFileService;
    /// <summary>
    /// The access  token
    /// </summary>
    private readonly string AccessToken;
    /// <summary>
    /// The remote node UID
    /// </summary>
    private readonly Guid RemoteNodeUid;

    /// <summary>
    /// Constructs a new remote file service instance
    /// </summary>
    /// <param name="executorUid">the UID of the executor</param>
    /// <param name="serverUrl">the URL to the FileFlows server</param>
    /// <param name="tempPath">the path to the temporary runner directory</param>
    /// <param name="logger">the logger</param>
    /// <param name="pathSeparator">the path separator</param>
    /// <param name="accessToken">the API token to use</param>
    /// <param name="remoteNodeUid">the UID of the remote node</param>
    /// <param name="dontUseTemporaryFilesForMoveCopy">If temporary files should not be used for move/copy</param>
    public RemoteFileService(Guid executorUid, string serverUrl, string tempPath, ILogger logger, char pathSeparator, string accessToken, Guid remoteNodeUid, bool dontUseTemporaryFilesForMoveCopy)
    {
        this.executorUid = executorUid;
        this.serverUrl = serverUrl;
        this.tempPath = tempPath;
        this.logger = logger;
        this.AccessToken = accessToken;
        this.RemoteNodeUid = remoteNodeUid;
        this.PathSeparator = pathSeparator;
        this._localFileService = new(dontUseTemporaryFilesForMoveCopy);
        HttpHelper.OnHttpRequestCreated = OnHttpRequestCreated;
    }

    /// <summary>
    /// Called when a http request is created so headers can be added
    /// </summary>
    /// <param name="request">the request</param>
    private void OnHttpRequestCreated(HttpRequestMessage request)
    {
        request.Headers.Add("x-executor", executorUid.ToString());
        if(string.IsNullOrWhiteSpace(AccessToken) == false)
            request.Headers.Add("x-token", AccessToken);
    }

    /// <summary>
    /// Gets the URL on the server
    /// </summary>
    /// <param name="route">the route</param>
    /// <returns>the full url to the server</returns>
    private string GetUrl(string route)
    {
        return serverUrl + "/remote/file-server/" + route;
    }

    /// <summary>
    /// Prepares a path replacing any variables
    /// </summary>
    /// <param name="path">the path</param>
    /// <returns>the corrected path</returns>
    private string PreparePath(ref string path)
    {
        if (ReplaceVariables != null)
            path = ReplaceVariables(path, true);
        return path;
    }

    /// <inheritdoc />
    public Result<string[]> GetFiles(string path, string searchPattern = "", bool recursive = false)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.GetFiles(path, searchPattern, recursive);
        try
        {
            var result = HttpHelper.Post<string[]>(GetUrl("directory/files"), new
            {
                path,
                searchPattern,
                recursive
            }).Result;
            return result.Data ?? new string[] { };
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return new string[] { };
        }
    }

    /// <inheritdoc />
    public Result<string[]> GetDirectories(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.GetDirectories(path);
        try
        {
            var result = HttpHelper.Post<string[]>(GetUrl("list-directories"), new { path }).Result;
            return result.Data ?? new string[] { };
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return new string[] { };
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryExists(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryExists(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/exists"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryEmpty(string path, string[]? includePatterns = null)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryExists(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("directory/empty"), new { path, includePatterns }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryDelete(string path, bool recursive = false)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryDelete(path, recursive);
        try
        {
            var result = HttpHelper.Post<string>(GetUrl("directory/delete"), new
            {
                path,
                recursive
            }).Result;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to delete directory: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryMove(string path, string destination)
    {
        if (FileIsLocal(PreparePath(ref path)))
        {
            if(FileIsLocal(PreparePath(ref destination)))
                return _localFileService.DirectoryMove(path, destination);
            return Result<bool>.Fail("Cannot move temporary directory to remote host");
        }
        
        if(FileIsLocal(PreparePath(ref destination)) && FileIsLocal(PreparePath(ref path)) == false)
            return Result<bool>.Fail("Cannot move remote directory to local host");

        try
        {
            var result = HttpHelper.Post<string>(GetUrl("directory/move"), new
            {
                path,
                destination
            }).Result;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to move directory: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> DirectoryCreate(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryCreate(path);
        try
        {
            var result = HttpHelper.Post<string>(GetUrl("directory/create"), new { path }).Result;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to create directory: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> DirectoryCreationTimeUtc(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryCreationTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("directory/creation-time-utc"), new { path }).Result;
            if (result.Success == false)
                return Result<DateTime>.Fail(result.Body);
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> DirectoryLastWriteTimeUtc(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectoryLastWriteTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("directory/last-write-time-utc"), new { path }).Result;
            if (result.Success == false)
                return Result<DateTime>.Fail(result.Body);
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<DateTime>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileExists(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileExists(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/exists"), new { path }).Result;
            return result.Data == true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool FileIsLocal(string path)
    {
        if (path.StartsWith(tempPath))
            return true;
        return false;
    }

    /// <inheritdoc />
    public Result<string> GetLocalPath(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return path;
        string filename = Path.Combine(tempPath, FileHelper.GetShortFileName(path));
        if (File.Exists(filename))
            return filename;
        
        // check if its a directory
        if (DirectoryExists(path).Is(true))
            return Result<string>.Fail("Cannot map a remote folder");

        var result = new FileDownloader(logger, serverUrl, executorUid, AccessToken, RemoteNodeUid)
            .DownloadFile(path, filename).Result;
        if (result.IsFailed)
            return Result<string>.Fail(result.Error ?? "Failed to get local path");
        return filename;
    }

    /// <inheritdoc />
    public Result<bool> Touch(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.Touch(path);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("touch"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed touching file: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<long> DirectorySize(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.DirectorySize(path);
        try
        {
            var result = HttpHelper.Post<long>(GetUrl("directory-size"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            if(ex.Message.StartsWith("Cannot access protected path:"))
                return Result<long>.Fail(ex.Message);
            return Result<long>.Fail("Failed getting folder size: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<FileInformation> FileInfo(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileInfo(path);
        try
        {
            var result = HttpHelper.Post<FileInformation>(GetUrl("file/info"), new { path }).Result;
            return result.Data!;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<FileInformation>.Fail("Failed to get file information: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileDelete(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileDelete(path);
        try
        {
            var result = HttpHelper.Post<string>(GetUrl("file/delete"), new { path }).Result;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to delete file: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<long> FileSize(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileSize(path);
        try
        {
            var result = HttpHelper.Post<long>(GetUrl("file/size"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<long>.Fail("Failed to get file size: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> FileCreationTimeUtc(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileCreationTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("file/creation-time-utc"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<DateTime>.Fail("Failed to get file creation time (UTC): " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<DateTime> FileLastWriteTimeUtc(string path)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileLastWriteTimeUtc(path);
        try
        {
            var result = HttpHelper.Post<DateTime>(GetUrl("file/last-write-time-utc"), new { path }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<DateTime>.Fail("Failed to get file last write time (UTC): " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileMove(string path, string destination, bool overwrite = true)
    {
        if (FileIsLocal(PreparePath(ref path)))
        {
            logger.ILog("File is local: " + path);
            if (FileIsLocal(PreparePath(ref destination)))
            {
                logger.ILog("Moving to local destination: " + destination);
                return _localFileService.FileMove(path, destination, overwrite);
            }

            logger.ILog("Uploading to remote destination: " + destination);
            var result = new FileUploader(logger, serverUrl, executorUid, AccessToken, RemoteNodeUid)
                .UploadFile(path, destination).Result;
            if (result.Success == false)
                return Result<bool>.Fail(result.Error);

            logger.ILog("Upload was successful, deleting old file: " + path);
            FileDelete(path);
            return true;
        }
        
        try
        {
            logger.ILog("Moving file via RemoteFileService file/move: " + path);
            var result = HttpHelper.Post<string>(GetUrl("file/move"), new
            {
                path,
                destination,
                overwrite
            }).Result;
            if (result.Success == false)
                return false;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to move file: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileCopy(string path, string destination, bool overwrite = true)
    {
        if (FileIsLocal(PreparePath(ref path)))
        {
            if(FileIsLocal(PreparePath(ref destination)))
                return _localFileService.FileCopy(path, destination, overwrite);
            var result = new FileUploader(logger, serverUrl, executorUid, AccessToken, RemoteNodeUid)
                .UploadFile(path, destination).Result;
            if (result.Success == false)
                return Result<bool>.Fail(result.Error);
            return true;
        }

        if (FileIsLocal(PreparePath(ref destination)))
        {
            // download the file
            var result = new FileDownloader(logger, serverUrl, executorUid, AccessToken, RemoteNodeUid)
                .DownloadFile(path, destination).Result;
            if (result.IsFailed)
                return Result<bool>.Fail(result.Error ?? "Failed to copy file");
            return true;
            
        }

        try
        {
            var result = HttpHelper.Post<string>(GetUrl("file/copy"), new
            {
                path,
                destination,
                overwrite
            }).Result;
            if (result.Success == false)
                return false;
            if(result.Data != null)
                logger.ILog(result.Data);
            return true;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to copy file: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FileAppendAllText(string path, string text)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.FileAppendAllText(path, text);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/append-text"), new
            {
                path,
                text
            }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to append text to file: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> SetCreationTimeUtc(string path, DateTime date)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.SetCreationTimeUtc(path, date);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/set-creation-time-utc"), new
            {
                path,
                date
            }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to set file creation time (UTC): " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> SetLastWriteTimeUtc(string path, DateTime date)
    {
        if (FileIsLocal(PreparePath(ref path)))
            return _localFileService.SetLastWriteTimeUtc(path, date);
        try
        {
            var result = HttpHelper.Post<bool>(GetUrl("file/set-last-write-time-utc"), new
            {
                path,
                date
            }).Result;
            return result.Data;
        }
        catch (Exception ex) when (ex is not ProtectedPathException)
        {
            return Result<bool>.Fail("Failed to set file last write time (UTC): " + ex.Message);
        }
    }

}