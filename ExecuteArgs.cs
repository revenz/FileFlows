/// <summary>
/// Arguments used to execute a process
/// </summary>
public class ExecuteArgs
{
    /// <summary>
    /// Gets or sets the command to execute
    /// </summary>
    public string Command { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments of the command
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments of the command as a list and will be correctly escaped
    /// </summary>
    public string[] ArgumentList { get; set; } = new string[] { };
    /// <summary>
    /// Gets or sets the timeout in seconds of the process
    /// </summary>
    public int Timeout { get; set; }
    

    /// <summary>
    /// When silent, nothing will be logged
    /// </summary>
    public bool Silent { get; set; }
    /// <summary>
    /// Gets or sets the working directory of the process
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}