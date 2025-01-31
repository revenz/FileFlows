namespace FileFlows.Common;

/// <summary>
/// Logging interface used to print log messages
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void ILog(params object[] args);
    
    /// <summary>
    /// Logs a message with no prefix
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void Raw(params object[] args);
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void DLog(params object[] args);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void WLog(params object[] args);

    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void ELog(params object[] args);
    
    /// <summary>
    /// Gets the last number of log lines
    /// </summary>
    /// <param name="length">The maximum number of lines to grab</param>
    /// <returns>The last number of log lines</returns>
    string GetTail(int length = 50);
}

/// <summary>
/// The type of log message
/// </summary>
public enum LogType
{
    /// <summary>
    /// A error message
    /// </summary>
    Error, 
    /// <summary>
    /// a warning message
    /// </summary>
    Warning,
    /// <summary>
    /// A informational message
    /// </summary>
    Info,
    /// <summary>
    /// A debug message
    /// </summary>
    Debug,
    /// <summary>
    /// A raw message with no prefix
    /// </summary>
    Raw
}
