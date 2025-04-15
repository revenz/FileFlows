using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for sending a notification
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    Task Record(NotificationSeverity severity, string title, string? message = null);
    
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="identifier">unique identifier to limit how often these messages are shown</param>
    /// <param name="frequency">the frequency of how often the messages can be shown</param>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    Task Record(string identifier, TimeSpan frequency, NotificationSeverity severity, string title, string? message = null);
}