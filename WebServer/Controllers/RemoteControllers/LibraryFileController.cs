using FileFlows.ServerShared.Models;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// System remote controller
/// </summary>
[Route("/remote/library-file")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class LibraryFileController : Controller
{

    /// <summary>
    /// Manually adds items for processing
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the response</returns>
    [HttpPost("manually-add")]
    public async Task<IActionResult> ManuallyAdd([FromBody] AddFileModel model)
    {
        var result =(await ServiceLoader.Load<LibraryFileService>().ManuallyAdd(model));
        if(result.Failed(out var error))
            return BadRequest(error);
        return Ok(new { Files = result });
    }
    
    /// <summary>
    /// Get a specific library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the library file instance</returns>
    [HttpGet("{uid}")]
    public async Task<LibraryFile?> GetLibraryFile(Guid uid)
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
    /// Gets the next library file for processing, and puts it into progress
    /// </summary>
    /// <param name="args">The arguments for the call</param>
    /// <returns>the next library file to process</returns>
    [HttpPost("next-file")]
    public Task<NextLibraryFileResult> GetNextLibraryFile([FromBody] NextLibraryFileArgs args)
    {
        // don't add any logic here to clear the file etc.  
        // the internal processing node bypasses this call and call the service directly (as does debug testing)
        // only remote processing nodes make this call
        var service = ServiceLoader.Load<LibraryFileService>();
        return service.GetNext(args.NodeName, args.NodeUid, args.NodeVersion, args.WorkerUid);
    }

    /// <summary>
    /// Tells the server not to check this node for number of seconds when checking for load balancing as it will
    /// be unavailable for this amount of time
    /// </summary>
    /// <param name="nodeUid">the UID of the node</param>
    /// <param name="forSeconds">the time in seconds</param>
    [HttpPost("node-cannot-run/{nodeUid}/{forSeconds}")]
    public void NodeCannotRun([FromRoute] Guid nodeUid, [FromRoute] int forSeconds)
    {
        var service = ServiceLoader.Load<LibraryFileService>();
        service.NodeCannotRun(nodeUid, forSeconds);
    } 

    /// <summary>
    /// Saves the full log for a library file
    /// Call this after processing has completed for a library file
    /// </summary>
    /// <param name="uid">The uid of the library file</param>
    /// <param name="log">the log</param>
    /// <returns>true if successfully saved log</returns>
    [HttpPut("{uid}/full-log")]
    public Task<bool> SaveFullLog([FromRoute] Guid uid, [FromBody] string log)
        => ServiceLoader.Load<LibraryFileService>().SaveFullLog(uid, log);
    
    /// <summary>
    /// Checks if a library file exists on the server
    /// </summary>
    /// <param name="uid">The Uid of the library file to check</param>
    /// <returns>true if exists, otherwise false</returns>
    [HttpGet("exists-on-server/{uid}")]
    public Task<bool> ExistsOnServer([FromRoute] Guid uid)
        => ServiceLoader.Load<LibraryFileService>().ExistsOnServer(uid);
    
    
    /// <summary>
    /// Delete a library files from disk and the database
    /// </summary>
    /// <param name="uid">the UID of the file to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public async Task<string> DeleteFile([FromRoute] Guid uid)
    {
        bool failed = false;
        var lf = await GetLibraryFile(uid);
        if (lf == null)
            return string.Empty;
        
        if (System.IO.File.Exists(lf.Name))
        {
            if (DeleteFileInner(lf.Name) == false)
            {
                failed = true;
            }
        }

        await ServiceLoader.Load<LibraryFileService>().Delete(uid);

        return failed ? Translater.Instant("ErrorMessages.NotAllFilesCouldBeDeleted") : string.Empty;

        bool DeleteFileInner(string file)
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
}