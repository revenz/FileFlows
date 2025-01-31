namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Notification controller
/// </summary>
[Route("/remote/notification")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class NotificationController
{
    /// <summary>
    /// Records a notification
    /// </summary>
    /// <param name="notification">the notification being recorded</param>
    [HttpPost("record")]
    public async Task Record([FromBody] NotificationModel notification)
    {
        var service = ServiceLoader.Load<INotificationService>();
        await service.Record(notification.Severity, notification.Title, notification.Message);
    }

    /// <summary>
    /// Notification model
    /// </summary>
    public class NotificationModel
    {
        /// <summary>
        /// Gets the notification severity
        /// </summary>
        public NotificationSeverity Severity { get; init; }

        /// <summary>
        /// Gets the notification title
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets the notification message
        /// </summary>
        public string? Message { get; init; }
    }
}