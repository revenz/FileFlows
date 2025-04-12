namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for notifications
/// </summary>
[Route("/api/notification")]
[FileFlowsAuthorize(UserRole.Admin)]
public class NotificationController : Controller
{
    /// <summary>
    /// Gets all the notifications
    /// </summary>
    /// <returns>the notifications</returns>
    [HttpGet]
    public Task<List<Notification>> Get()
        => ((NotificationService)ServiceLoader.Load<INotificationService>()).GetAll(markAsRead: true);

    /// <summary>
    /// Removes a notification
    /// </summary>
    /// <param name="uid">the uid of the notification</param>
    [HttpDelete("{uid}")]
    public void Delete([FromRoute] Guid uid)
    {
        var service = (NotificationService)ServiceLoader.Load<INotificationService>();
        service.Delete(uid);
    }
    
    /// <summary>
    /// Dismisses all the notifications
    /// </summary>
    [HttpDelete("dismiss-all")]
    public void Delete()
    {
        var service = (NotificationService)ServiceLoader.Load<INotificationService>();
        service.DeleteAll();
    }

}