using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Remote flow controllers
/// </summary>
[Route("/remote/flow")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class FlowController : Controller
{
    /// <summary>
    /// Gets the flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <returns>the flow</returns>
    [HttpGet("{uid}")]
    public Task<Flow?> GetFlow([FromRoute] Guid uid)
        => ServiceLoader.Load<FlowService>().GetByUidAsync(uid);
    
    /// <summary>
    /// Basic flow list
    /// </summary>
    /// <returns>flow list</returns>
    [HttpGet("basic-list")]
    public async Task<Dictionary<Guid, string>> GetFlowList([FromQuery] FlowType? type = FlowType.Standard)
    {
        IEnumerable<Flow> items = await new FlowService().GetAllAsync();
        if (type != null)
            items = items.Where(x => x.Type == type.Value);
        return items.OrderBy(x => x.Name.ToLowerInvariant()).ToDictionary(x => x.Uid, x => x.Name);
    }
    
    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    [HttpGet("failure-flow-by-library/{libraryUid}")]
    public Task<Flow?> GetFailureFlow([FromRoute] Guid libraryUid)
        => ServiceLoader.Load<FlowService>().GetFailureFlow(libraryUid);
}