using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

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
    public Task<IEnumerable<Notification>> Get()
        => ((NotificationService)ServiceLoader.Load<INotificationService>()).GetAll(markAsRead: true);
}