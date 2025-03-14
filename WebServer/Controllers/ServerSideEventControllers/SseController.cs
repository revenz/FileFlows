namespace FileFlows.WebServer.Controllers.ServerSideEventControllers;

/// <summary>
/// Controller for Server Side Events
/// </summary>
[Route("/api/sse")]
[ApiExplorerSettings(IgnoreApi = true)]
[FileFlowsAuthorize]
public class SseController : Controller
{ 
    /// <summary>
    /// Gets a SSE connection
    /// </summary>
    /// <param name="initialData">if the initial data should be sent</param>
    [HttpGet]
    public async Task Get([FromQuery] bool initialData = false)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        using var writer = new StreamWriter(Response.Body);
        var _broker = ServiceLoader.Load<SseEventBroker>();
        var clientId = _broker.AddClient(writer);
        
        try
        {
            if (initialData)
            { 
                var data = await GetInitialData(HttpContext);
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    return; // in case it took too long and the request was closed while waiting
                
                await writer.WriteLineAsync("data: id:" + JsonSerializer.Serialize(data) + "\n");
                await writer.FlushAsync();
                await Task.Delay(5000, HttpContext.RequestAborted);
            }

            while (HttpContext.RequestAborted.IsCancellationRequested == false)
            {
                await writer.WriteLineAsync(":heartbeat\n");
                await writer.FlushAsync();
                await Task.Delay(5000, HttpContext.RequestAborted);
            }
        }
        catch
        {
            // Client disconnected
        }
        finally
        {
            _broker.RemoveClient(clientId);
        }
    }

    /// <summary>
    /// Gets the initial data to send to the client
    /// </summary>
    private async Task<InitialClientData> GetInitialData(HttpContext context)
    {
        var flowsTask = ServiceLoader.Load<FlowService>().GetAllAsync();
        var librariesTask = ServiceLoader.Load<LibraryService>().GetAllAsync();
        var executorsTask = ServiceLoader.Load<FlowRunnerService>().GetExecutors();
        var nodeStatusesTask = ServiceLoader.Load<NodeService>().GetStatusSummaries();
        var tagsTask = ServiceLoader.Load<TagService>().GetAllAsync();
        var profileTask = context.GetProfile(); 

        await Task.WhenAll(flowsTask, librariesTask, executorsTask, nodeStatusesTask, tagsTask, profileTask);

        var profile = profileTask.Result;
        if (profile == null)
            throw new Exception("User is not logged in");
        
        var lang = await ServiceLoader.Load<LanguageService>().GetLanguageJson(profile.Language);
        
        return new InitialClientData
        {
            Profile = profile,
            LanguageJson = lang,
            CurrentSystemInfo = ServiceLoader.Load<DashboardService>().GetSystemInfo(),
            CurrentFileOverData = ServiceLoader.Load<DashboardFileOverviewService>().GetData(),
            CurrentUpdatesInfo = ServiceLoader.Load<UpdateService>().Info,
            FlowList = flowsTask.Result.ToDictionary(x => x.Uid, x => x.Name),
            LibraryList = librariesTask.Result.ToDictionary(x => x.Uid, x => x.Name),
            CurrentExecutorInfoMinified = executorsTask.Result.Values.ToList(),
            NodeStatusSummaries = nodeStatusesTask.Result,
            Tags = (tagsTask.Result ?? Enumerable.Empty<Tag>())
                .OrderBy(x => x.Name.ToLowerInvariant())
                .ToList()
        };
    }
}