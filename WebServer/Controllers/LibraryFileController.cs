using FileFlows.ServerShared.Models;
using FileFlows.Services.ServiceHelpers;
using Humanizer;
using Swashbuckle.AspNetCore.Annotations;
using LibraryFileService = FileFlows.Services.LibraryFileService;
using LibraryService = FileFlows.Services.LibraryService;
using NodeService = FileFlows.Services.NodeService;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Library files controller
/// </summary>
[Route("/api/library-file")]
[FileFlowsAuthorize(UserRole.Files)]
public class LibraryFileController : Controller 
{

    /// <summary>
    /// Lists all the library files, only intended for the UI
    /// </summary>
    /// <param name="status">The status to list</param>
    /// <param name="page">The page to get</param>
    /// <param name="pageSize">The number of items to fetch</param>
    /// <param name="filter">[Optional] filter text</param>
    /// <param name="node">[Optional] node to filter by</param>
    /// <param name="library">[Optional] library to filter by</param>
    /// <param name="flow">[Optional] flow to filter by</param>
    /// <param name="sortBy">[Optional] sort by method</param>
    /// <param name="tag">[Optional] tag to filter by</param>
    /// <returns>a slimmed down list of files with only needed information</returns>
    [HttpGet("list-all")]
    public async Task<LibraryFileDatalistModel> ListAll([FromQuery] int status, [FromQuery] int page = 0, 
        [FromQuery] int pageSize = 0, [FromQuery] string? filter = null, [FromQuery] Guid? node = null, 
        [FromQuery] Guid? library = null, [FromQuery] Guid? flow = null, [FromQuery] FilesSortBy? sortBy = null, [FromQuery] Guid? tag = null)
    {
        var service = ServiceLoader.Load<LibraryFileService>();
        var lfStatus = await service.GetStatus();
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        
        var allLibraries = (await ServiceLoader.Load<LibraryService>().GetAllAsync());
        
        var sysInfo = new LibraryFilterSystemInfo()
        {
            AllLibraries = allLibraries.ToDictionary(x => x.Uid, x => x),
            Executors = FlowRunnerService.Executors.Values.ToList(),
            LicensedForProcessingOrder = LicenseService.IsLicensed(LicenseFlags.ProcessingOrder)
        };
        var lfFilter = new LibraryFileFilter()
        {
            Status = (FileStatus)status,
            Skip = page * pageSize,
            Rows = pageSize,
            Filter = filter,
            NodeUid = node,
            LibraryUid = library,
            FlowUid = flow,
            TagUid = LicenseService.IsLicensed() ? tag : null,
            SortBy = sortBy,
            SysInfo = sysInfo,
        };
        
        List<LibraryFile> files = await service.GetAll(lfFilter);
        if (string.IsNullOrWhiteSpace(filter) == false || node != null || flow != null || library != null)
        {
            // need to get total number of items matching filter as well
            int total = await service.GetTotalMatchingItems(lfFilter);
            HttpContext?.Response?.Headers?.TryAdd("x-total-items", total.ToString());
        }

        var nodeNames = (await ServiceLoader.Load<NodeService>().GetAllAsync()).ToDictionary(x => x.Uid, x => x.Name);
        return new()
        {
            Status = lfStatus,
            LibraryFiles = LibaryFileListModelHelper.ConvertToListModel(files, (FileStatus)status, libraries, nodeNames)
        };
    }

    /// <summary>
    /// Basic node list
    /// In this controller in case the users only has access to files page
    /// </summary>
    /// <returns>node list</returns>
    [HttpGet("node-list")]
    public async Task<List<NodeInfo>> GetNodeList()
    {
        var nodes = await new NodeService().GetAllAsync();
        return nodes.Select(x => new NodeInfo()
        {
            Uid = x.Uid,
            Name = x.Name,
            OperatingSystem = x.OperatingSystem
        }).ToList();
    }

    /// <summary>
    /// Gets all library files in the system
    /// </summary>
    /// <param name="status">The status to get, if missing will return all library files</param>
    /// <param name="skip">The amount of items to skip</param>
    /// <param name="top">The amount of items to grab, 0 to grab all</param>
    /// <returns>A list of library files</returns>
    [HttpGet]
    public Task<List<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
        => ServiceLoader.Load<LibraryFileService>().GetAll(status, skip: skip, rows: top);

    /// <summary>
    /// Get next 10 upcoming files to process
    /// </summary>
    /// <returns>a list of upcoming files to process</returns>
    [HttpGet("upcoming")]
    [FileFlowsAuthorize]
    public async Task<IActionResult> Upcoming()
    {
        var data = await ServiceLoader.Load<LibraryFileService>().GetAll(FileStatus.Unprocessed, rows: 50);
        var results = data.Select(x => new
        {
            x.Uid,
            x.Name,
            x.LibraryName,
            DisplayName = ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(x.Name, x.RelativePath, x.LibraryName)?.EmptyAsNull() 
                          ?? x.RelativePath?.EmptyAsNull() ?? x.Name
        });
        return Ok(results);
    }

    /// <summary>
    /// Gets the last 10 successfully processed files
    /// </summary>
    /// <returns>the last successfully processed files</returns>
    [HttpGet("recently-finished")]
    [FileFlowsAuthorize]
    public async Task<IActionResult> RecentlyFinished([FromQuery]bool? failedFiles = null)
    {
        var service = ServiceLoader.Load<LibraryFileService>();
        var libraries = await ServiceLoader.Load<LibraryService>().GetAllAsync();
        LibraryFile[] all;
        var processed = await service.GetAll(FileStatus.Processed, rows: 50, allLibraries: libraries);
        var failed = await service.GetAll(FileStatus.ProcessingFailed, rows: 50, allLibraries: libraries);
        var mapping = await service.GetAll(FileStatus.MappingIssue, rows: 50, allLibraries: libraries);
        var minDate = new DateTime(2020, 1, 1);
        if (failedFiles == false)
            all = processed.ToArray();
        else if (failedFiles == true)
            all = failed.Union(mapping).ToArray();
        else
        {
            all = processed.Union(failed).Union(mapping)
                .OrderByDescending(x => x.ProcessingEnded < minDate ? x.ProcessingStarted : x.ProcessingEnded)
                .ToArray();
            
        }

        if (all.Any() == false)
            return Ok(new object[] { });
        
        all = all.OrderByDescending(x => x.ProcessingEnded < minDate ? x.ProcessingStarted : x.ProcessingEnded)
            .ToArray();
        
        if (all.Length > 50)
            all = all.Take(50).ToArray();
        var data = all.Select(x =>
        {
            var date = x.ProcessingEnded.Year > 2000 ? x.ProcessingEnded : x.ProcessingStarted;
            var when = date.ToLocalTime().Humanize(false, DateTime.UtcNow);
            return new
            {
                x.Uid,
                DisplayName = 
                    Regex.IsMatch(x.OutputPath, @"^[\w\d]{2,}:") ? x.OutputPath : // special case for uploaded files e.g. nc: for next cloud
                    ServiceLoader.Load<FileDisplayNameService>().GetDisplayName(x.Name, x.RelativePath, x.LibraryName)?.EmptyAsNull() ?? 
                              x.RelativePath?.EmptyAsNull() ?? x.Name,
                x.RelativePath,
                x.ProcessingEnded,
                When = when,
                x.OriginalSize,
                x.FinalSize,
                Message = x.Status == FileStatus.ProcessingFailed ? x.FailureReason : null,
                Status = (int)x.Status
            };
        });
        return Ok(data);
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("status")]
    [FileFlowsAuthorize]
    public Task<List<LibraryStatus>> GetStatus()
        => ServiceLoader.Load<LibraryFileService>().GetStatus();


    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile?> Get(Guid uid)
    {
        // first see if the file is currently processing, if it is, return that in memory 
        var file = await ServiceLoader.Load<FlowRunnerService>().TryGetFile(uid) ?? 
                   await ServiceLoader.Load<LibraryFileService>().Get(uid);
        
        if(file != null && (file.Status == FileStatus.ProcessingFailed || file.Status == FileStatus.Processed))
        {
            if (LibraryFileLogHelper.HtmlLogExists(uid))
                return file;
            LibraryFileLogHelper.CreateHtmlOfLog(uid);
        }
        return file;
    }

    /// <summary>
    /// Downloads a  log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The download action result</returns>
    [HttpGet("{uid}/log/download")]
    public IActionResult GetLog([FromRoute] Guid uid)
    {     
        string log = LibraryFileLogHelper.GetLog(uid);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
        return File(data, "application/octet-stream", uid + ".log");
    }
    
    /// <summary>
    /// Get the log of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lines">Optional number of lines to fetch</param>
    /// <param name="html">if the log should be html if possible</param>
    /// <returns>The log of the library file</returns>
    [HttpGet("{uid}/log")]
    public string GetLog([FromRoute] Guid uid, [FromQuery] int lines, [FromQuery] bool html = true)
    {
        try
        {
            return html ? LibraryFileLogHelper.GetHtmlLog(uid, lines) : LibraryFileLogHelper.GetLog(uid);
        }
        catch (Exception ex)
        {
            return "Error opening log: " + ex.Message;
        }
    }

    /// <summary>
    /// A reference model of library files to move to the top of the processing queue
    /// </summary>
    /// <param name="model">The reference model of items in order to move</param>
    /// <returns>an awaited task</returns>
    [HttpPost("move-to-top")]
    public async Task MoveToTop([FromBody] ReferenceModel<Guid> model)
    {
        if (model == null || model.Uids?.Any() != true)
            return; // nothing to delete

        var list = model.Uids.ToArray();
        await ServiceLoader.Load<LibraryFileService>().MoveToTop(list);
    }

    /// <summary>
    /// Delete library files from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public Task Delete([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().Delete(model?.Uids ?? []);

    /// <summary>
    /// Delete library files from disk
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("delete-files")]
    public async Task<string> DeleteFiles([FromBody] ReferenceModel<Guid> model)
    {
        List<Guid> deleted = new();
        bool failed = false;
        foreach (var uid in model.Uids)
        {
            var lf = await Get(uid);
            if(lf == null)
                continue;
            if (System.IO.File.Exists(lf.Name) == false)
                continue;
            if (DeleteFile(lf.Name) == false)
            {
                failed = true;
                continue;
            }

            deleted.Add(lf.Uid);
        }

        if (deleted.Any())
            await ServiceLoader.Load<LibraryFileService>().Delete(deleted.ToArray());

        return failed ? Translater.Instant("ErrorMessages.NotAllFilesCouldBeDeleted") : string.Empty;

        bool DeleteFile(string file)
        {
            try
            {
                System.IO.File.Delete(file);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Failed to delete file: " + ex.Message);
                return false;
            }
        }
    }
    
    /// <summary>
    /// Reprocess library files
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>an awaited task</returns>
    [HttpPost("reprocess")]
    public Task Reprocess([FromBody] ReprocessModel model)
        => ServiceLoader.Load<LibraryFileService>().Reprocess(model, onlySetProcessInfo: false);
    
    /// <summary>
    /// Set the process options for library files
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>an awaited task</returns>
    [HttpPost("set-process-options")]
    public Task SetProcessOptions([FromBody] ReprocessModel model)
        => ServiceLoader.Load<LibraryFileService>().Reprocess(model, onlySetProcessInfo: true);
    
   

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="model">A reference model containing UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    [HttpPost("unhold")]
    public Task Unhold([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().Unhold(model?.Uids ?? []);

    /// <summary>
    /// Toggles force processing 
    /// </summary>
    /// <param name="model">A reference model containing UIDs to toggle force on</param>
    /// <returns>an awaited task</returns>
    [HttpPost("toggle-force")]
    public Task ToggleForce([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().ToggleForce(model?.Uids ??[]);
    

    /// <summary>
    /// Force processing of files
    /// Used to force files that are currently out of schedule to be processed
    /// </summary>
    /// <param name="model">the items to process</param>
    /// <returns>an awaited task</returns>
    [HttpPost("force-processing")]
    public Task ForceProcessing([FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().ForceProcessing(model?.Uids ?? []);


    /// <summary>
    /// Sets the status of files
    /// </summary>
    /// <param name="status">the status to set to</param>
    /// <param name="model">the items to set the status on</param>
    /// <returns>an awaited task</returns>
    [HttpPost("set-status/{status}")]
    public Task SetStatus([FromRoute] FileStatus status, [FromBody] ReferenceModel<Guid> model)
        => ServiceLoader.Load<LibraryFileService>().SetStatus(status, model?.Uids ?? []);


    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    [HttpPost("search")]
    [FileFlowsAuthorize] // needed for the dashboard
    public Task<List<LibraryFile>> Search([FromBody] LibraryFileSearchModel filter)
        => ServiceLoader.Load<LibraryFileService>().Search(filter);


    /// <summary>
    /// Get a specific library file using cache
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    internal Task<LibraryFile?> GetCached(Guid uid)
        => ServiceLoader.Load<LibraryFileService>().Get(uid);

    /// <summary>
    /// Downloads a library file
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <param name="test">[Optional] if the file should be tested to see if it still exists and can be downloaded</param>
    /// <returns>the download</returns>
    [HttpGet("download/{uid}")]
    public async Task<IActionResult> Download([FromRoute] Guid uid, [FromQuery] bool test = false)
    {
        var file = await ServiceLoader.Load<LibraryFileService>().Get(uid);
        if (file == null)
            return NotFound("File not found.");
        string filePath = file.Name;
        if (System.IO.File.Exists(filePath) == false)
        {
            filePath = file.OutputPath;
            if (string.IsNullOrEmpty(filePath) || System.IO.File.Exists(filePath) == false)
                return NotFound("File not found.");
        }

        if (test)
            return Ok();
        
        var fileInfo = new FileInfo(filePath);
        var stream = fileInfo.OpenRead();
        return File(stream, "application/octet-stream", fileInfo.Name);
    }

    // /// <summary>
    // /// Processes a file or adds it to the queue to add to the system
    // /// </summary>
    // /// <param name="filename">the filename of the file to process</param>
    // /// <param name="libraryUid">[Optional] the UID of the library the file is in, if not passed in then the first file with the name will be used</param>
    // /// <returns>the HTTP response, 200 for an ok, otherwise bad request</returns>
    // [HttpPost("process-file")]
    // public async Task<IActionResult> ProcessFile([FromQuery] string filename, [FromQuery] Guid? libraryUid)
    // {
    //     try
    //     {
    //         if (string.IsNullOrWhiteSpace(filename))
    //             return BadRequest("Filename not set");
    //
    //         var service = ServiceLoader.Load<LibraryFileService>();
    //         var file = await service.GetFileIfKnown(filename, libraryUid);
    //         if (file != null)
    //         {
    //             if ((int)file.Status < 2)
    //                 return Ok(); // already in the queue or processing
    //             await service.Reprocess(file.Uid);
    //             return Ok();
    //         }
    //
    //         // file not known, add to the queue
    //         var library = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).Where(x => x.Enabled)
    //             .FirstOrDefault(x => filename.StartsWith(x.Path));
    //         if (library == null)
    //             return BadRequest("No library found for file: " + filename);
    //         var watchedLibraray = LibraryWorker.GetWatchedLibrary(library);
    //         if (watchedLibraray == null)
    //             return BadRequest("Library is not currently watched");
    //
    //         watchedLibraray.QueueItem(filename);
    //         return Ok();
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex.Message);
    //     }
    // }


    /// <summary>
    /// Manually adds items for processing
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the response</returns>
    [HttpPost("manually-add")]
    public async Task<IActionResult> ManuallyAdd([FromBody] AddFileModel model)
    {
        if ((await ServiceLoader.Load<LibraryFileService>().ManuallyAdd(model)).Failed(out var error))
            return BadRequest(error);
        return Ok();
    }

    /// <summary>
    /// Uploads a file in chunks, saving with a temporary extension until the final chunk is received.
    /// On the last chunk, renames the file to the final name, adding a timestamp if necessary.
    /// </summary>
    /// <param name="file">The current file chunk being uploaded.</param>
    /// <param name="chunkNumber">The sequence number of the current chunk (starting from 0).</param>
    /// <param name="totalChunks">The total number of chunks for the file.</param>
    /// <param name="fileName">The original file name for the upload.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> indicating the status of the upload.
    /// Returns the final file path when the last chunk has been uploaded successfully.
    /// </returns>
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [SwaggerIgnore]
    [RequestFormLimits(BufferBodyLengthLimit = 14_737_418_240,
        MultipartBodyLengthLimit = 14_737_418_240)] // 10GiB limit
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] int chunkNumber,
        [FromForm] int totalChunks,
        [FromForm] string fileName)
    {
        if (string.IsNullOrWhiteSpace(DirectoryHelper.ManualLibrary))
            return BadRequest("Manual Library path not set");
        if (Directory.Exists(DirectoryHelper.ManualLibrary) == false)
            return BadRequest("Manual Library path does not exist");
        try
        {
            // Path for the temp file during upload
            string tempFilePath = Path.Combine(DirectoryHelper.ManualLibrary,
                Plugin.Helpers.FileHelper.GetSafeFileName(fileName) + ".temp");

            // Set the file mode based on the chunk number
            var fileMode = chunkNumber == 0 ? FileMode.Create : FileMode.Append;

            // Write the current chunk to the temporary file
            await using (var stream = new FileStream(tempFilePath, fileMode, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            // If it's the last chunk, rename the file to its final name
            if (chunkNumber == totalChunks - 1)
            {
                string finalFilePath = Path.Combine(DirectoryHelper.ManualLibrary,
                    Plugin.Helpers.FileHelper.GetSafeFileName(fileName));

                // Check if the final file already exists, and append timestamp if necessary
                while (System.IO.File.Exists(finalFilePath))
                {
                    finalFilePath = Plugin.Helpers.FileHelper.InsertBeforeExtension(
                        finalFilePath, DateTime.Now.ToString("_hhmmss"));
                }

                // Rename the temp file to the final file path
                System.IO.File.Move(tempFilePath, finalFilePath);
                Logger.Instance.ILog("File upload to: " + finalFilePath);

                return Ok(finalFilePath);
            }

            // Return success for intermediate chunks
            return Ok(tempFilePath);
        }
        catch (Exception ex)
        {
            // Log and return an error if an exception occurs during upload
            Logger.Instance.WLog("File upload failed: " + ex.Message);
            return BadRequest(ex.Message);
        }
    }

}
