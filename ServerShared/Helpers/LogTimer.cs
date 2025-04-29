using System.Text;

namespace FileFlows.ServerShared.Helpers;


/// <summary>
/// A helper class to log messages with timestamps relative to when the timer started.
/// </summary>
public class LogTimer
{
    /// <summary>
    /// Stores the logged messages.
    /// </summary>
    private readonly StringBuilder _log = new();
    
    /// <summary>
    /// The start time of the log timer.
    /// </summary>
    private readonly DateTime _startTime = DateTime.Now;

    /// <summary>
    /// Logs a message with the time elapsed since the timer started.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        TimeSpan elapsed = DateTime.Now - _startTime;
        string formattedElapsed = elapsed.ToString(@"mm\:ss\.fff");
        _log.AppendLine($"[{formattedElapsed}] {message}");
    }

    /// <summary>
    /// Returns the log contents as a string.
    /// </summary>
    /// <returns>The log as a string.</returns>
    public override string ToString() => _log.ToString();
}
