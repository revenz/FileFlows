/// <summary>
/// Arguments for script execution
/// </summary>
public class ScriptExecutionArgs
{
    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets teh NodeParameters
    /// </summary>
    //public NodeParameters Args { get; set; }

    /// <summary>
    /// Gets a collection of additional arguments to be passed to the javascript executor
    /// </summary>
    public Dictionary<string, object> AdditionalArguments { get; set; } = new Dictionary<string, object>();

    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
}