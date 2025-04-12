using Markdig.Helpers;

namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Handler for Notifications
/// </summary>
/// <param name="feService">the frontend service</param>
public class NotificationHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets a list of notifications
    /// </summary>
    public List<Notification> Notifications { get; private set; } = [];
    
    /// <summary>
    /// Gets or sets a list of Toasts
    /// </summary>
    public List<Notification> Toasts { get; private set; } = [];

    /// <summary>
    /// Gets or sets a list of Toasts
    /// </summary>
    public List<Notification> All { get; private set; } = [];
    
    /// <summary>
    /// Event raised when notifications are updated
    /// </summary>
    public event Action? OnNotificationsUpdated; 
    
    /// <summary>
    /// Event raised when notification is received
    /// </summary>
    public event Action<Notification> OnNotification; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Notifications = data.Notifications;
        All = data.Notifications.ToList();
        
        feService.Registry.Register<Notification>("Notification", (ed) =>
        {
            OnNotification?.Invoke(ed);
            
            Notifications.Insert(0, ed);
            All.Insert(0, ed);
            OnNotificationsUpdated?.Invoke();
        });
    }
    
    /// <summary>
    /// Show an error message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public void ShowError(string message, int duration = 5_000)
        => Toast(NotificationSeverity.Error, message, duration);
    
    /// <summary>
    /// Show an information message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public void ShowInfo(string message, int duration = 5_000)
        => Toast(NotificationSeverity.Information, message, duration);
    
    /// <summary>
    /// Show an success message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public void ShowSuccess(string message, int duration = 5_000)
        => Toast(NotificationSeverity.Success, message, duration);

    /// <summary>
    /// Show an warning message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public void ShowWarning(string message, int duration = 5_000)
        => Toast(NotificationSeverity.Warning, message, duration);

    /// <summary>
    /// Shows the actual toast message
    /// </summary>
    /// <param name="level">the toast level</param>
    /// <param name="message">the message of the toast</param>
    /// <param name="duration">the duration to show the toast for</param>
    void Toast(NotificationSeverity level, string message, int duration)
    {
        message = Translater.TranslateIfNeeded(message);
        Notification toast = new ()
        {
            Uid = Guid.NewGuid(),
            Message = string.Empty, //message,
            Date = DateTime.UtcNow,
            Severity = level,
            Title = message
        };
        
        OnNotification?.Invoke(toast);
        
        All.Insert(0, toast);
        Toasts.Insert(0, toast);
        OnNotificationsUpdated?.Invoke();
    }

    /// <summary>
    /// Dismisses a notification
    /// </summary>
    /// <param name="notification">The notification to dismiss</param>
    public void Dismiss(Notification notification)
    {
        All.Remove(notification);
        Toasts.Remove(notification);
        if (Notifications.Remove(notification) == false)
            return;
        // have to tell the Server to clear this notification
        _ = HttpHelper.Delete("/api/notification/" + notification.Uid);
    }

    /// <summary>
    /// Dismisses all notifications
    /// </summary>
    public void DismissAll()
    {
        if (All.Count == 0 && Toasts.Count == 0 && Notifications.Count == 0)
            return;
        All.Clear();
        Toasts.Clear();
        if (Notifications.Count > 0)
        {
            // have to tell the Server to clear this notification
            _ = HttpHelper.Delete("/api/notification/dismiss-all");
        }
        OnNotificationsUpdated?.Invoke();
    }
}