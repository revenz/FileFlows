using Microsoft.AspNetCore.Authorization;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for the dashboard
/// </summary>
[Route("/api/dashboard")]
[FileFlowsAuthorize]
public class DashboardController : BaseController
{
    
    /// <summary>
    /// Gets the file overview
    /// </summary>
    /// <returns></returns>
    [HttpGet("file-overview")]
    public FileOverviewData GetFileOverview()
        => ServiceLoader.Load<DashboardFileOverviewService>().GetData();

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the processing node</returns>
    [HttpGet("node-summary")]
    public async Task<List<NodeStatusSummary>> GetNodeSummary()
        => await ServiceLoader.Load<NodeService>().GetStatusSummaries();

    /// <summary>
    /// Gets the executors info minified
    /// </summary>
    /// <returns>the executors info minified</returns>
    [HttpGet("executors-info-minified")]
    public async Task<List<FlowExecutorInfoMinified>> GetExecutorsInfoMinified()
        => (await ServiceLoader.Load<FlowRunnerService>().GetExecutors()).Values.ToList();
    
    /// <summary>
    /// Gets any updates
    /// </summary>
    /// <returns>the updates</returns>
    [HttpGet("updates")]
    public UpdateInfo GetUpates()
        => ServiceLoader.Load<UpdateService>().Info;

    /// <summary>
    /// Gets a node icon
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <returns>the icon</returns>
    [HttpGet("node/{uid}/icon")]
    [AllowAnonymous]
    public async Task<IActionResult> NodeIcon(Guid uid)
    {
        var node = await ServiceLoader.Load<NodeService>().GetByUidAsync(uid);
        if (string.IsNullOrWhiteSpace(node.Icon) || node.Icon.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) == false)
            return NotFound();
        try
        {
            // Remove the "data:" prefix and decode the base64 data
            var base64Data = node.Icon.Substring(node.Icon.IndexOf(',', StringComparison.Ordinal) + 1);
            var imageData = Convert.FromBase64String(base64Data);

            // Determine the MIME type from the data URL (e.g., "data:image/png;base64,")
            var mimeType = node.Icon[5..node.Icon.IndexOf(';', StringComparison.Ordinal)];
        
            // Return the image data as a file with the appropriate MIME type
            return File(imageData, mimeType);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 image data.");
        }
    }
}