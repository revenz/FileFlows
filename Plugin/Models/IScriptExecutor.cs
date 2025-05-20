namespace FileFlows.Plugin.Models;

/// <summary>
/// Interface for executing a script
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Executes a script
    /// </summary>
    /// <param name="args">the arguments of the script</param>
    /// <returns>the output node</returns>
    Result<int> Execute(ScriptExecutionArgs args);
}

/// <summary>
/// Arguments for script execution
/// </summary>
public class ScriptExecutionArgs
{
    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; }
    
    /// <summary>
    /// Gets or sets the language of the script being executed
    /// </summary>
    public ScriptLanguage Language { get; set; }
    
    /// <summary>
    /// Gets or sets the temp path to run this script if it needs to be run as a file
    /// </summary>
    public string TempPath { get; set; }

    /// <summary>
    /// Gets or sets the type of script being executed
    /// </summary>
    public ScriptType ScriptType { get; set; }

    /// <summary>
    /// Gets or sets teh NodeParameters
    /// </summary>
    public NodeParameters Args { get; set; }
    
    /// <summary>
    /// Gets or sets the logger to use
    /// </summary>
    public ILogger Logger { get; set; }
    
    /// <summary>
    /// Delegate for handling notifications.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message of the notification. This parameter can be null.</param>
    public delegate void NotificationDelegate(object severity, string title, string? message);

    /// <summary>
    /// Gets or sets the callback for notifications.
    /// </summary>
    public NotificationDelegate NotificationCallback { get; set; }

    /// <summary>
    /// Gets a collection of additional arguments to be passed to the javascript executor
    /// </summary>
    public Dictionary<string, object> AdditionalArguments { get; set; }
}