using System.Net.Http.Headers;
using System.Text;
using FileFlows.ServerShared.FileServices;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Controller responsible for managing file-related operations.
/// </summary>
[Route("/remote/file-server")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class FileServerController : Controller
{
    /// <summary>
    /// Buffer size used for reading files during file operations.
    /// </summary>
    private const int BufferSize = 8192;

    /// <summary>
    /// The instance of the local file service
    /// </summary>
    private readonly LocalFileService _localFileService;

    private readonly StringLogger lfsLogger;

    /// <summary>
    /// The logger used for the file server
    /// </summary>
    private static readonly Logger Logger;

    private static string[] AllowedPaths = [];

    /// <summary>
    /// Static consturctor
    /// </summary>
    static FileServerController()
    {
        Logger = new Logger();
        Logger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "FileServer", false));
    }

    /// <summary>
    /// Constructs the controller
    /// </summary>
    public FileServerController(SettingsService settingsService)
    {
        var settings = settingsService.Get().Result;
        var allowedPaths = ServiceLoader.Load<LibraryService>().GetAllAsync().Result.Select(x => x.Path)
            .Union(settings.FileServerAllowedPaths ?? new string[] { })
            .Union([DirectoryHelper.ManualLibrary])
            .Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Distinct()
            .ToArray();

        bool allowedPathsChanged = Shared.Helpers.ObjectHelper.AreEqual(AllowedPaths, allowedPaths) == false;
        if(allowedPathsChanged)
            AllowedPaths = allowedPaths;

        lfsLogger = new StringLogger();
        _localFileService = new LocalFileService(settings.DontUseTempFilesWhenMovingOrCopying)
        {
            AllowedPaths = allowedPaths,
            CheckProtectivePaths = true,
            PermissionsFile = settings.FileServerFilePermissions is < 1 or > 777 ? Globals.DefaultPermissionsFile : settings.FileServerFilePermissions,
            PermissionsFolder = settings.FileServerFolderPermissions is < 1 or > 777 ? Globals.DefaultPermissionsFolder : settings.FileServerFolderPermissions,
            Logger = lfsLogger
        };
        
        if(allowedPathsChanged)
        {
            Logger.ILog("Allowed File Server Paths: \n" + string.Join("\n", allowedPaths));
            Logger.ILog("FileService Directory Permissions: " + _localFileService.PermissionsFolder);
            Logger.ILog("FileService File Permissions: " + _localFileService.PermissionsFile);
        }

        FileHelper.Permissions = _localFileService.PermissionsFile.Value;
        FileHelper.PermissionsFolders = _localFileService.PermissionsFolder.Value;
    }

    /// <summary>
    /// Checks if the file server is enabled
    /// </summary>
    /// <returns></returns>
    private bool ValidateRequest(out string message)
    {
        message = string.Empty;
#if(DEBUG == false)
        if (HttpContext.Request.Headers.TryGetValue("x-executor", out var executorHeaderValue) == false)
        {
            message = "No executor identifier given.";
            Logger.ELog(message);
            return false;
        }

        // Try parsing the header value to a GUID
        if (Guid.TryParse(executorHeaderValue, out Guid executorUid) == false)
        {
            message = "Invalid executor identifier given.";
            Logger.ELog(message);
            return false;
        }

        if (FlowRunnerService.Executors.ContainsKey(executorUid) == false)
        {
            message = "Unknown executor identifier given.";
            Logger.ELog(message);
            return false;
        }
#endif

        return GetIsFileServiceEnabled();
    }

    /// <summary>
    /// Gets if the file service is enabled
    /// </summary>
    /// <returns>true if its enabled, otherwise false</returns>
    private bool GetIsFileServiceEnabled()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileServer) == false)
            return false;
        
        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        return settings.FileServerDisabled == false;
    }

    /// <summary>
    /// Retrieves a list of files based on the specified parameters.
    /// </summary>
    /// <param name="request">Request parameters for listing files.</param>
    /// <returns>An IActionResult containing a list of files.</returns>
    [HttpPost("directory/files")]
    public IActionResult DirectoryFiles([FromBody] DirectoryFilesRequest request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.GetFiles(request.Path, request.SearchPattern, request.Recursive);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a list of directories at the specified path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>An IActionResult containing a list of directories.</returns>
    [HttpPost("list-directories")]
    public IActionResult ListDirectories([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.GetDirectories(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Checks if a directory exists at the given path.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>An IActionResult indicating if the directory exists.</returns>
    [HttpPost("directory/exists")]
    public IActionResult DirectoryExists([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryExists(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Checks if a directory is empty at the given path.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>An IActionResult indicating if the directory is empty.</returns>
    [HttpPost("directory/empty")]
    public IActionResult DirectoryEmpty([FromBody] DirectoryEmptyModel model)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryEmpty(model.Path, model.IncludePatterns);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the size of a directory
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>the size of the directory</returns>
    [HttpPost("directory-size")]
    public IActionResult DirectorySize([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectorySize(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Deletes a directory at the specified path.
    /// </summary>
    /// <param name="request">Request parameters for deleting a directory.</param>
    /// <returns>An IActionResult indicating the success of the delete operation.</returns>
    [HttpPost("directory/delete")]
    public IActionResult DirectoryDelete([FromBody] DirectoryOperationRequest request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryDelete(request.Path, request.Recursive);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        
        
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Delete Directory: " + log);
        return Ok(log);
    }

    /// <summary>
    /// Moves a directory from one location to another.
    /// </summary>
    /// <param name="request">Request parameters for moving a directory.</param>
    /// <returns>An IActionResult indicating the success of the move operation.</returns>
    [HttpPost("directory/move")]
    public IActionResult DirectoryMove([FromBody] DestinationRequest request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryMove(request.Path, request.Destination);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Move Directory: " + log);
        return Ok(log);
    }

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    /// <returns>An IActionResult indicating the success of directory creation.</returns>
    [HttpPost("directory/create")]
    public IActionResult DirectoryCreate([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryCreate(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Create Directory: " + log);
        return Ok(log);
    }

    /// <summary>
    /// Retrieves the creation time of a directory in UTC.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>An IActionResult containing the directory's creation time (UTC).</returns>
    [HttpPost("directory/creation-time-utc")]
    public IActionResult GetDirectoryCreationTimeUtc([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryCreationTimeUtc(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the last write time of a directory in UTC.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>An IActionResult containing the directory's last write time (UTC).</returns>
    [HttpPost("directory/last-write-time-utc")]
    public IActionResult GetDirectoryLastWriteTimeUtc([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.DirectoryLastWriteTimeUtc(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }


    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult indicating whether the file exists.</returns>
    [HttpPost("file/exists")]
    public IActionResult FileExists([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileExists(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        Logger.ILog($"File Exists '{path.Path}': {result.Value}");
        return Ok(result.Value);
    }

    /// <summary>
    /// Updates the last access and modification time of a file to the current time.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult indicating the success of the operation.</returns>
    [HttpPost("touch")]
    public IActionResult TouchFile([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.Touch(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        Logger.ILog($"Touched: '{path.Path}'");
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves information about a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult containing file information.</returns>
    [HttpPost("file/info")]
    public IActionResult GetFileInfo([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileInfo(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult indicating the success of file deletion.</returns>
    [HttpPost("file/delete")]
    public IActionResult DeleteFile([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileDelete(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        if (result.Value == false)
            return StatusCode(500, "Failed to delete");
        
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Deleted File: " + log);
        return Ok(log);
    }

    /// <summary>
    /// Checks if a path can be written to
    /// </summary>
    /// <param name="path">the path to check</param>
    /// <returns>if it can be written to</returns>
    [HttpPost("can-write-to")]
    public IActionResult CanWriteTo([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        string _path = path.Path ?? string.Empty;
        Logger.ILog("Check can write to path: " + _path);
        if(string.IsNullOrWhiteSpace(_path))
            return StatusCode(503, "Bade Data: Path not set");
        bool isProtected = _localFileService.IsProtectedPath(ref _path);
        return Ok(isProtected ? "false" : "true");
    }

    /// <summary>
    /// Retrieves the size of a file in bytes.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult containing the file size.</returns>
    [HttpPost("file/size")]
    public IActionResult GetFileSize([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileSize(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the creation time of a file in UTC.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult containing the file's creation time (UTC).</returns>
    [HttpPost("file/creation-time-utc")]
    public IActionResult GetFileCreationTimeUtc([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileCreationTimeUtc(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the last write time of a file in UTC.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An IActionResult containing the file's last write time (UTC).</returns>
    [HttpPost("file/last-write-time-utc")]
    public IActionResult GetFileLastWriteTimeUtc([FromBody] PathRequest path)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileLastWriteTimeUtc(path);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Moves a file from one location to another.
    /// </summary>
    /// <param name="request">The move file request.</param>
    /// <returns>An IActionResult indicating the success of the file move operation.</returns>
    [HttpPost("file/move")]
    public IActionResult MoveFile([FromBody] FileTransferRequest request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileMove(request.Path, request.Destination, request.Overwrite);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        if (result.Value == false)
            return StatusCode(500, "Failed to move file");
        
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Move File: " + log);
        return Ok(log);
    }

    /// <summary>
    /// Copies a file from one location to another.
    /// </summary>
    /// <param name="request">The copy file request.</param>
    /// <returns>An IActionResult indicating the success of the file copy operation.</returns>
    [HttpPost("file/copy")]
    public IActionResult CopyFile([FromBody] FileTransferRequest request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileCopy(request.Path, request.Destination, request.Overwrite);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        if (result.Value == false)
            return StatusCode(500, "Failed to copy file");
        
        string log = lfsLogger.ToString();
        Logger.ILog("Remote Copy File: " + log);
        return Ok(log);
    }

    [HttpPost("file/append-text")]
    public IActionResult AppendTextToFile([FromBody] TextDataModel request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.FileAppendAllText(request.Path, request.Text);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        Logger.ILog($"Appended '{request.Path}': {request.Text}");
        return Ok(result.Value);
    }

    [HttpPost("file/set-creation-time-utc")]
    public IActionResult SetFileCreationTimeUtc([FromBody] DateDataModel request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.SetCreationTimeUtc(request.Path, request.Date);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        Logger.ILog($"Set File Creation Time '{request.Path}': {request.Date}");
        return Ok(result.Value);
    }

    [HttpPost("file/set-last-write-time-utc")]
    public IActionResult SetFileLastWriteTimeUtc([FromBody] DateDataModel request)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
        var result = _localFileService.SetLastWriteTimeUtc(request.Path, request.Date);
        if (result.IsFailed)
            return StatusCode(500, result.Error);
        Logger.ILog($"Set File Write Time '{request.Path}': {request.Date}");
        return Ok(result.Value);
    }

    // /// <summary>
    // /// Retrieves the file content associated with the specified unique identifier (UID).
    // /// </summary>
    // /// <param name="uid">The unique identifier (UID) associated with the file.</param>
    // /// <returns>The file content as an asynchronous action result.</returns>
    // [HttpGet("{uid}/hash")]
    // public async Task<IActionResult> GetHash([FromRoute] Guid uid)
    // {
    //     if (ValidateRequest(out string message) == false)
    //         return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
    //
    //     string filePath = await GetPath(uid);
    //
    //     if (string.IsNullOrWhiteSpace(filePath) || System.IO.File.Exists(filePath) == false)
    //         return NotFound(); // Handle file not found scenario
    //
    //     try
    //     {
    //         // Calculate file hash for the entire file if no range specified
    //         var hash = await FileHasher.Hash(filePath);
    //         Logger.ILog("File: " + filePath + "\nHash: " + hash);
    //         return Content(hash);
    //     }
    //     catch(Exception ex)
    //     {
    //         Logger.ELog($"An error occurred: {ex.Message}");
    //         return StatusCode(500);
    //     }
    // }

    /// <summary>
    /// Retrieves the file content associated with the specified unique identifier (UID).
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The file content as an asynchronous action result.</returns>
    [HttpPost("download")]
    public async Task<IActionResult> FileDownload([FromBody] PathRequest path)
    {
        if (GetIsFileServiceEnabled() == false)
            return StatusCode(503, "File service is currently disabled.");

        if (string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path) == false)
            return NotFound("Unable to find file: " + path); // Handle file not found scenario

        try
        {
            Logger.ILog("File Download Request: " + path.Path);
            var fileInfo = new FileInfo(path);
            var totalLength = fileInfo.Length;

            if (Request.Headers.ContainsKey("Range") &&
                RangeHeaderValue.TryParse(Request.Headers["Range"], out var rangeHeader))
            {
                var range = rangeHeader.Ranges.FirstOrDefault();
                var startIndex = range?.From ?? 0;
                var endIndex = range?.To ?? totalLength - 1;
                var contentRange = new ContentRangeHeaderValue(startIndex, endIndex, totalLength);
                Response.Headers.Append("Accept-Ranges", "bytes");
                Response.Headers.Append("Content-Range", contentRange.ToString());

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                           BufferSize, FileOptions.Asynchronous))
                {
                    stream.Seek(startIndex, SeekOrigin.Begin);

                    Response.Headers.Append("Content-Length", (endIndex - startIndex + 1).ToString());

                    var partialStream = new StreamContent(stream, BufferSize);
                    return new FileStreamResult(await partialStream.ReadAsStreamAsync(), "application/octet-stream")
                    {
                        FileDownloadName = fileInfo.Name
                    };
                }
            }

            Response.Headers.Append("Accept-Ranges", "bytes");

            return PhysicalFile(path, "application/octet-stream", fileInfo.Name);
        }
        catch (Exception ex)
        {
            // Log the exception
            Logger.ELog($"An error occurred: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Saves the received file data to the specified path.
    /// </summary>
    /// <param name="path">The path where the file will be saved.</param>
    /// <param name="hash">The hash of the file data.</param>
    /// <returns>An asynchronous action result indicating the success or failure of the save operation.</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromQuery] string path, [FromQuery] string hash)
    {
        if (ValidateRequest(out string message) == false)
            return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");

        Logger.ILog("FileServer: Save: " + path);

        StringBuilder log = new StringBuilder(); 
        try
        {
            await using var stream = Request.Body;

            if (stream == null || stream.CanRead == false)
            {
                Logger.WLog("FileServer: No File specified for upload.");
                return StatusCode(503, "No File specified for upload.");
            }

            var tempFileResult = await SaveToTempLocation(path, log, stream);
            if (tempFileResult.Failed(out string error))
            {
                log.AppendLine(error);
                Logger.ELog($"FileServer: Save: '{path}' (FAILED){Environment.NewLine}{log}");
                return StatusCode(500, log.ToString());
            }

            var tempFile = tempFileResult.Value;

            // move from temp after its uploaded
            log.AppendLine("Renaming temporary filename: " + tempFile);
            var tempFileInfo = new FileInfo(tempFile);
            log.AppendLine("Temporary file exists: " + tempFileInfo.Exists);
            if (tempFileInfo.Exists == false)
            {
                Logger.ELog($"FileServer: Save: '{path}' (FAILED){Environment.NewLine}{log}");
                return StatusCode(503, log.ToString());
            }

            var fileInfo = new FileInfo(path);

            log.AppendLine("Moving temp directory to final location: " + fileInfo.DirectoryName);
            if (_localFileService.FileMove(tempFile, path, true).Failed(out error))
            {
                log.AppendLine("Failed to move file: " + error);
                Logger.ELog($"FileServer: Save: '{path}' (FAILED){Environment.NewLine}{log}");
                return StatusCode(500, log.ToString());
            }

            var tempDir = FileHelper.GetDirectory(tempFile);
            if (Directory.Exists(tempDir))
            {
                log.AppendLine("Deleting temporary folder: " + tempDir);
                Directory.Delete(tempDir,true);
            }

            log.AppendLine("FileServer: Uploaded successfully: " + path);
            Logger.ILog($"FileServer: Save: '{path}' (OK){Environment.NewLine}{log}");
            return Ok(log.ToString());
        }
        catch (Exception ex)
        {
            // Log the exception
            log.AppendLine($"FileServer: An error occurred: {ex.Message}" + Environment.NewLine + ex.StackTrace);
            Logger.ELog($"FileServer: An error occurred: {ex.Message}" + Environment.NewLine + ex.StackTrace);
            return StatusCode(500, log.ToString());
        }
    }

    /// <summary>
    /// Saves the file to a temporary file/folder
    /// </summary>
    /// <param name="path">the actual destination path</param>
    /// <param name="log">the logger</param>
    /// <param name="stream">the file stream</param>
    /// <returns>the temp path</returns>
    private async Task<Result<string>> SaveToTempLocation(string path, StringBuilder log, Stream stream)
    {
        log.AppendLine("Path: " + path);
        var directory = FileHelper.GetDirectory(path);
        var directoryName = FileHelper.GetDirectoryName(path);
        string parent = FileHelper.GetDirectory(directory);
        var fileInfo = new FileInfo(path);
        DirectoryInfo tempDirLocation;
        if (string.IsNullOrWhiteSpace(parent) == false)
        {
            tempDirLocation = new DirectoryInfo(Path.Combine(parent,
                "_TEMP_" + directoryName + "_" + Guid.NewGuid()));
        }
        else
        {
            tempDirLocation = new DirectoryInfo(directory + "_" + Guid.NewGuid());
        }
        log.AppendLine("tempDirLocation: " + tempDirLocation);

        string dirPath = tempDirLocation.FullName;
        var result = _localFileService.DirectoryCreate(tempDirLocation.FullName);
        if (result.Failed(out string error))
            return Result<string>.Fail("Failed creating directory: " + error);

        string outFile = Path.Combine(dirPath, fileInfo.Name + ".fftemp");
        log.AppendLine("Writing file to temporary filename: " + outFile);

        await using (var fileStream = new FileStream(outFile, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        return outFile;
    }

    // /// <summary>
    // /// Validates the integrity of the file by comparing the provided hash with the computed hash of the file data.
    // /// </summary>
    // /// <param name="data">The byte array representing the file data.</param>
    // /// <param name="expectedFileHash">The base64-encoded SHA256 hash expected to match the computed hash of the file data.</param>
    // /// <returns>True if the computed hash matches the expected hash, indicating file integrity; otherwise, false.</returns>
    // private bool ValidateFileHash(byte[] data, string expectedFileHash)
    // {
    //     using var sha256 = SHA256.Create();
    //     var actualHash = sha256.ComputeHash(data);
    //     var base64ActualHash = Convert.ToBase64String(actualHash);
    //
    //     return base64ActualHash.Equals(expectedFileHash);
    // }

    // /// <summary>
    // /// Deletes a remote file or folder
    // /// </summary>
    // /// <param name="model">the model to delete</param>
    // /// <returns>An asynchronous action result indicating the success or failure of the save operation.</returns>
    // [HttpPost("delete")]
    // public async Task<IActionResult> DeleteRemote([FromBody] DeleteRemoteModel model)
    // {
    //     if (ValidateRequest(out string message) == false)
    //         return StatusCode(503, message?.EmptyAsNull() ?? "File service is currently disabled.");
    //
    //     string path = model.Path;
    //     
    //     if(string.IsNullOrWhiteSpace(path))
    //         return StatusCode(503, "No path specified to delete.");
    //
    //     var libraryPaths = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).Select(x => x.Path).ToArray();
    //
    //     try
    //     {
    //         var fileInfo = new FileInfo(path);
    //         if (fileInfo.Exists)
    //         {
    //             bool isPathAllowed = libraryPaths.Any(allowedPath => fileInfo.FullName.StartsWith(allowedPath));
    //             if (isPathAllowed == false)
    //                 return StatusCode(503, "Path does not belong to a configured library, cannot delete file.");
    //             
    //             fileInfo.Delete();
    //             return Ok();
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(503, ex.Message);
    //     }
    //
    //     try
    //     {
    //         var dirInfo = new DirectoryInfo(path);
    //         if (dirInfo.Exists)
    //         {
    //             string libPath = libraryPaths.FirstOrDefault(allowedPath => dirInfo.FullName.StartsWith(allowedPath));
    //             if (string.IsNullOrWhiteSpace(libPath))
    //                 return StatusCode(503, "Path does not belong to a configured library, cannot delete folder.");
    //
    //             if (model.IfEmpty)
    //             {
    //                 List<string> messages = new();
    //                 var deleted = RecursiveDelete(model.IncludePatterns, libPath, path, true, messages);
    //                 string log = string.Join("\n", messages);
    //                 return deleted ? Ok(log) : StatusCode(503, log);
    //             }
    //             
    //             dirInfo.Delete(true);
    //             return Ok();
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(503, ex.Message);
    //     }
    //     
    //     return Ok();
    // }
    //
    //
    // public static bool RecursiveDelete(string[] IncludePatterns, string root, string path, bool deleteSubFolders, List<string> messages)
    // {
    //     Logger.ILog("Checking directory to delete: " + path);
    //     DirectoryInfo dir = new DirectoryInfo(path);
    //     if (string.Equals(dir.FullName, root, StringComparison.CurrentCultureIgnoreCase))
    //     {
    //         messages.Add("At root, stopping deleting: " + root);
    //         return true;
    //     }
    //     if (dir.FullName.Length <= root.Length)
    //     {
    //         messages.Add("At root2, stopping deleting: " + root);
    //         return true;
    //     }
    //     if (deleteSubFolders == false && dir.GetDirectories().Any())
    //     {
    //         messages.Add("Directory contains subfolders, cannot delete: " + dir.FullName);
    //         return false;
    //     }
    //
    //     var files = dir.Exists ? dir.GetFiles("*.*", SearchOption.AllDirectories) : new FileInfo[] { };
    //     if (IncludePatterns?.Any() == true)
    //     {
    //         var includedFiles = files.Where(x =>
    //         {
    //             foreach (var pattern in IncludePatterns)
    //             {
    //                 if (x.FullName.Contains(pattern))
    //                     return true;
    //                 try
    //                 {
    //                     if (System.Text.RegularExpressions.Regex.IsMatch(x.FullName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    //                         return true;
    //                 }
    //                 catch (Exception) { }
    //             }
    //             return false;
    //         }).ToList();
    //         if (includedFiles.Any())
    //         {
    //             messages.Add("Directory is not empty, cannot delete: " + dir.FullName + "\n" + 
    //                          string.Join("\n", includedFiles.Select(x => "- " + x).ToArray()));
    //             return false;
    //         }
    //     }
    //     else if (files.Length == 0)
    //     {
    //         messages.Add("Directory is not empty, cannot delete: " + dir.FullName);
    //         return false;
    //     }
    //
    //     messages.Add("Deleting directory: " + dir.FullName);
    //     try
    //     {
    //         dir.Delete(true);
    //     }
    //     catch (Exception ex)
    //     {
    //         messages.Add("Failed to delete directory: " + ex.Message);
    //         return dir.Exists == false; // silently fail
    //     }
    //
    //     return RecursiveDelete(IncludePatterns, root, dir.Parent.FullName, false, messages);
    // }

    /// <summary>
    /// Model used for deleting a remote path
    /// </summary>
    public class DeleteRemoteModel
    {
        /// <summary>
        /// the path to the item to delete
        /// </summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// if the path is a directory, only delete the directory if it is empty
        /// </summary>
        public bool IfEmpty { get; init; }

        /// <summary>
        /// the patterns to include for files to determine if the directory is empty
        /// eg [mkv, avi, mpg, etc] will treat the directory as empty if none of those files are found
        /// </summary>
        public string[] IncludePatterns { get; init; } = [];
    }


    // Request models for different operations
    /// <summary>
    /// Request parameters for directory operations.
    /// </summary>
    public class DirectoryOperationRequest
    {
        /// <summary>
        /// The directory path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the operation should be recursive.
        /// </summary>
        public bool Recursive { get; set; }
    }

    /// <summary>
    /// Request parameters for moving a directory.
    /// </summary>
    public class PathRequest
    {

        protected string FixPath(string value)
        {
            return value;
            // return value?.Replace("/", System.IO.Path.PathSeparator.ToString())
            //                        ?.Replace(@"\\", System.IO.Path.PathSeparator.ToString());
        }

        private string _Path = string.Empty;

        /// <summary>
        /// The directory path.
        /// </summary>
        public string Path
        {
            get => _Path;
            set => _Path = FixPath((value));
        }

        /// <summary>
        /// Converts the PathRequest to a string
        /// </summary>
        /// <param name="value">the value</param>
        /// <returns>the Path string</returns>
        public static implicit operator string(PathRequest value)
            => value.Path;
    }

    /// <summary>
    /// Request parameters for moving a directory.
    /// </summary>
    public class DestinationRequest : PathRequest
    {
        private string _Destination = string.Empty;

        /// <summary>
        /// The destination path.
        /// </summary>
        public string Destination
        {
            get => _Destination;
            set => _Destination = FixPath((value));
        }
    }


    /// <summary>
    /// Request parameters for listing files.
    /// </summary>
    public class DirectoryFilesRequest : PathRequest
    {

        /// <summary>
        /// The search pattern for filtering files.
        /// </summary>
        public string SearchPattern { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the search should be recursive.
        /// </summary>
        public bool Recursive { get; set; }
    }

    /// <summary>
    /// Represents a file transfer request containing source and destination paths.
    /// </summary>
    public class FileTransferRequest : DestinationRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the file if the destination already exists.
        /// </summary>
        public bool Overwrite { get; set; }
    }

    /// <summary>
    /// Model for appending text to a file.
    /// </summary>
    public class TextDataModel : PathRequest
    {
        /// <summary>
        /// Text to append.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for setting file creation or last write time.
    /// </summary>
    public class DateDataModel : PathRequest
    {
        /// <summary>
        /// Date to set (Creation or Last Write).
        /// </summary>
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Model for setting file creation or last write time.
    /// </summary>
    public class DirectoryEmptyModel : PathRequest
    {
        /// <summary>
        /// Gets or sets patterns to match the files against
        /// </summary>
        public string[]? IncludePatterns { get; set; }
    }
}