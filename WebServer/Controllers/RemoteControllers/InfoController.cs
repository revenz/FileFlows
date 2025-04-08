using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Basic info controller
/// </summary>
[Route("/remote/info")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class InfoController : Controller
{
    /// <summary>
    /// Gets the shrinkage groups
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    [HttpGet("shrinkage-groups")]
    public List<StorageSavedData> GetShrinkageGroups()
    {
        var groups = new StatisticService().GetStorageSaved();
        return groups;
    }
    
    /// <summary>
    /// Get the current status
    /// </summary>
    /// <returns>the current status</returns>
    [HttpGet("status")]
    public Task<StatusModel> Get()
        => new StatusService().Get();
    
    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>True if there is an update</returns>
    [HttpGet("update-available")]
    public Task<object> UpdateAvailable()
        => new StatusService().UpdateAvailable();

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    [HttpGet("library-status")]
    public Task<List<LibraryStatus>> GetStatus()
        => ServiceLoader.Load<LibraryFileService>().GetStatus();
}