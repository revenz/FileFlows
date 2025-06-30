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
}