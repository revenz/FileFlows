namespace FileFlows.Common;

/// <summary>
/// A logger that adds a common prefix to every log message
/// </summary>
/// <param name="logger">the base logger to log to</param>
/// <param name="prefix">the prefix</param>
public class PrefixedLogger(ILogger logger, string prefix) : ILogger
{
    /// <summary>
    /// Writes an information log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void ILog(params object[] args)
        => Log(LogType.Info, args);

    /// <inheritdoc />
    public void Raw(params object[] args)
        => Log(LogType.Raw, args);

    /// <summary>
    /// Writes an debug log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void DLog(params object[] args)
        => Log(LogType.Debug, args);

    /// <summary>
    /// Writes an warning log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void WLog(params object[] args)
        => Log(LogType.Warning, args);

    /// <summary>
    /// Writes an error log message
    /// </summary>
    /// <param name="args">the log parameters</param>
    public void ELog(params object[] args)
        => Log(LogType.Error, args);
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log to record</param>
    /// <param name="args">the arguments of the message</param>
    private void Log(LogType type, params object[] args)
    {
        string message = prefix + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        
        switch (type)
        {
            case LogType.Debug:
                logger.DLog(message);
                break;
            case LogType.Warning:
                logger.WLog(message);
                break;
            case LogType.Error:
                logger.ELog(message);
                break;
            case LogType.Raw:
                logger.Raw(message);
                break;
            case LogType.Info:
                logger.ILog(message);
                break;
        }
    }

    /// <inheritdoc />
    public string GetTail(int length = 50)
        => logger.GetTail(50);
}