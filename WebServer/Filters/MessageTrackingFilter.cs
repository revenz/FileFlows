using Microsoft.AspNetCore.SignalR;

namespace FileFlows.WebServer.Filters;

/// <summary>
/// A SignalR Hub filter that tracks the timestamp of the last method invocation received from each connected client.
/// </summary>
public class MessageTrackingFilter : IHubFilter
{
    private NodeService _nodeService;

    public MessageTrackingFilter()
    {
        _nodeService = ServiceLoader.Load<NodeService>();
    }

    /// <summary>
    /// Intercepts hub method invocations and updates the last received timestamp for the calling client.
    /// </summary>
    /// <param name="invocationContext">The context for the current method invocation.</param>
    /// <param name="next">The delegate to invoke the next component in the pipeline.</param>
    /// <returns>The result of the invoked method.</returns>
    public async ValueTask<object> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object>> next)
    {
        var connectionId = invocationContext.Context.ConnectionId;
        _nodeService.UpdateLastSeenByConnection(connectionId);
        return await next(invocationContext);
    }
}
