using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Broker service
/// </summary>
public interface IBrokerService
{
    /// <summary>
    /// Broadcasts an event with a topic and JSON data to all connected SSE clients.
    /// </summary>
    /// <param name="eventName">The event name or topic.</param>
    /// <param name="data">The event data to send.</param>
    /// <param name="requiredRole">Optional role required to get this event broadcast to the user</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BroadcastEvent(string eventName, object data, UserRole? requiredRole = null);

}