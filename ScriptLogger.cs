class ScriptLogger 
{
    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public static void ILog(params object[] args) => Log("INFO", args);
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public static void DLog(params object[] args)=> Log("DBUG", args);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public static void WLog(params object[] args)=> Log("WARN", args);

    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public static void ELog(params object[] args)=> Log("ERRR", args);
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public static void Log(string type, params object[] args)
    {
        string message = type + " -> " + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        Console.WriteLine(message);
    }
}