namespace FileFlows.Shared;

/// <summary>
/// A logger used to write log messages 
/// </summary>
public class Logger : ILogger
{
    private static Logger _Instance;
    /// <summary>
    /// Gets or sets the instance of Logger
    /// </summary>
    public static Logger Instance
    {
        get
        {
            _Instance ??= new();
            return _Instance;
        }
    }


    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ILog(params object[] args) => Log(LogType.Info, args);

    /// <inheritdoc />
    public void Raw(params object[] args) => Log(LogType.Raw, args);
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void WLog(params object[] args) => Log(LogType.Warning, args);

    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ELog(params object[] args) => Log(LogType.Error, args);
    
    /// <summary>
    /// Gets the last number of log lines
    /// </summary>
    /// <param name="length">The maximum number of lines to grab</param>
    /// <returns>The last number of log lines</returns>
    public string GetTail(int length = 50) => "";

    private readonly List<ILogWriter> Writers = new ();

    /// <summary>
    /// Register a writer with the logger
    /// </summary>
    /// <param name="writer">the writer</param>
    public void RegisterWriter(ILogWriter writer)
    {
        if(Writers.Contains(writer) == false)
            Writers.Add(writer);
    }

    /// <summary>
    /// Tries to get a specific registered log writer
    /// </summary>
    /// <typeparam name="T">the type of log writer to get</typeparam>
    /// <returns>the instance if found, otherwise false</returns>
    public bool TryGetLogger<T>(out T writer) where T : class, ILogWriter
    {
        writer = Writers.FirstOrDefault(x => x is T) as T;
        return writer != null;
    }

    /// <summary>
    /// Logs a message to all writers
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the message arguments</param>
    private void Log(LogType type, params object[] args)
    {
        try
        {
            foreach (var writer in Writers)
            {
                if (writer.GetType().Name == "ClientConsoleLogger")
                    writer.Log(type, args).Wait();
                else
                {
                    try
                    {
                        writer.Log(type, args).Wait();
                    }
                    catch (Exception)
                    {
                         // Ignored
                    }
                }
            }
        }
        catch (Exception)
        {
            // so cant crash
        }
    }
}