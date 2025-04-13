namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// Logging model
/// </summary>
public class LoggingModel
{
    /// <summary>
    /// Gets or sets if the Queue messages should be logged
    /// </summary>
    public bool LogQueueMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of log files to keep
    /// </summary>
    public int LogFileRetention { get; set; }
    
    /// <summary>
    /// Gets or sets if every request to the server should be logged
    /// </summary>
    public bool LogEveryRequest { get; set; }
}