using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Statistics controller
/// </summary>
[Route("/remote/statistic")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class StatisticController : Controller
{
    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    [HttpPost("record-running-total")]
    public Task RecordRunningTotals([FromQuery] string name, [FromQuery] string value)
        => new StatisticService().RecordRunningTotal(name, value);
    
    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    [HttpPost("record-average")]
    public Task RecordAverage([FromQuery] string name, [FromQuery] int value)
        => new StatisticService().RecordAverage(name, value);
}