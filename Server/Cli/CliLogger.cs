using FileFlows.Plugin;

namespace FileFlows.Server.Cli;

/// <summary>
/// Logger for the command line
/// </summary>
public class CliLogger : ILogger
{
    /// <inheritdoc />
    public void ILog(params object[] args)
        => Log(LogType.Info, args);

    /// <inheritdoc />
    public void Raw(params object[] args)
        => Log(LogType.Raw, args);

    /// <inheritdoc />
    public void DLog(params object[] args)
        => Log(LogType.Debug, args);
    
    /// <inheritdoc />
    public void WLog(params object[] args)
        => Log(LogType.Warning, args);

    /// <inheritdoc />
    public void ELog(params object[] args)
        => Log(LogType.Error, args);

    /// <inheritdoc />
    public string GetTail(int length = 50)
        => string.Empty;
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log to record</param>
    /// <param name="args">the arguments of the message</param>
    private void Log(LogType type, params object[] args)
    {
        string prefix = type is LogType.Info or LogType.Debug or LogType.Raw ? string.Empty : type + ": ";
        
        string message = prefix + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            JsonSerializer.Serialize(x)));
        Console.WriteLine(message);
    }
}