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
    public List<Notification> Notifications { get; private set; }
    
    /// <summary>
    /// Event raised when notifications are updated
    /// </summary>
    public event Action<List<Notification>> OnNotificationsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        Notifications = data.Notifications;
        
        feService.Registry.Register<Notification>("Notification", (ed) =>
        {
            Notifications.Insert(0, ed);
            OnNotificationsUpdated?.Invoke(Notifications);
        });
    }
}