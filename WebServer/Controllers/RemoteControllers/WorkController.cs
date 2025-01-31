using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Worker controller
/// </summary>
[Route("/remote/work")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class WorkController : Controller
{
    /// <summary>
    /// Start work, tells the server work has started on a flow runner
    /// </summary>
    /// <param name="info">The info about the work starting</param>
    /// <returns>the updated info</returns>
    [HttpPost("start")]
    public Task<FlowExecutorInfo?> StartWork([FromBody] FlowExecutorInfo info)
        => ServiceLoader.Load<FlowRunnerService>().Start(info);

    /// <summary>
    /// Finish work, tells the server work has finished on a flow runner
    /// </summary>
    /// <param name="info">the flow executor info</param>
    [HttpPost("finish")]
    public Task FinishWork([FromBody] FlowExecutorInfo info)
        => ServiceLoader.Load<FlowRunnerService>().Finish(info);
    
    /// <summary>
    /// Update work, tells the server about updated work on a flow runner
    /// </summary>
    /// <param name="info">The updated work information</param>
    [HttpPost("update")]
    public Task UpdateWork([FromBody] FlowExecutorInfo info)
        => ServiceLoader.Load<FlowRunnerService>().Update(info);

    /// <summary>
    /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
    /// This is called when a node first starts.
    /// </summary>
    /// <param name="nodeUid">The UID of the processing node</param>
    /// <returns>an awaited task</returns>
    [HttpPost("clear/{nodeUid}")]
    public Task Clear([FromRoute] Guid nodeUid)
        => ServiceLoader.Load<FlowRunnerService>().Clear(nodeUid);
    
}