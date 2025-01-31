namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller to download a node
/// </summary>
[Route("/download")]
[FileFlowsAuthorize]
public class DownloadController:Controller
{
    /// <summary>
    /// Downloads the node from the server
    /// </summary>
    /// <returns>the node download zip</returns>
    [HttpGet]
    public IActionResult Download()
    {
        string zipName = $"FileFlows-Node-{Globals.Version}.zip";
        string file = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "Nodes", zipName);
        if (System.IO.File.Exists(file) == false)
            return NotFound();

        return new FileStreamResult(FileOpenHelper.OpenRead_NoLocks(file), "application/octet-stream")
        {
            FileDownloadName = zipName
        };
    }
}