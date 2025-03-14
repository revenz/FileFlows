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
    [HttpGet]
    public async Task Get()
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        using var writer = new StreamWriter(Response.Body);
        var _broker = ServiceLoader.Load<SseEventBroker>();
        var clientId = _broker.AddClient(writer);
        
        try
        {
            while (!HttpContext.RequestAborted.IsCancellationRequested)
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
}