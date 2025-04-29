namespace FileFlows.Shared.Models;


/// <summary>
/// Represents a notification.
/// </summary>
public class Notification : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the notification
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the notification was generated.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the title of the notification.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the message content of the notification.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the notification.
    /// </summary>
    public NotificationSeverity Severity { get; set; } 
    
    /// <summary>
    /// Gets or sets if this has been read
    /// </summary>
    public bool Read { get; set; }
}


/// <summary>
/// Represents the severity level of a notification.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informational notification.
    /// </summary>
    Information = 0,
    
    /// <summary>
    /// Success notification
    /// </summary>
    Success = 1,

    /// <summary>
    /// Warning notification.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error notification.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical notification.
    /// </summary>
    Critical = 4
}
