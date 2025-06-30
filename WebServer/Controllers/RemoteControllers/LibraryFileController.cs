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
}