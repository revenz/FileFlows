namespace FileFlows.RemoteServices;

/// <summary>
/// Service for sending a notification to the server
/// </summary>
public class NotificationService : RemoteService, INotificationService
{
    /// <inheritdoc />
    public async Task Record(NotificationSeverity severity, string title, string? message = null)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/notification/record", new
            {
                Severity = (int)severity,
                Title = title,
                Message = message
            });
        }
        catch (Exception ex)
        {
            // ignored
            Logger.Instance?.ELog("Failed to record notification: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Task Record(string identifier, TimeSpan frequency, NotificationSeverity severity, string title, string? message = null)
    {
        return Task.CompletedTask;
    }
}
