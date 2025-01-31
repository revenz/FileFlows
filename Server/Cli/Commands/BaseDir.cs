namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Shows the minimal GUI
/// </summary>
public class BaseDir : Command
{
    /// <inheritdoc />
    public override string Switch => "base-dir";

    /// <inheritdoc />
    public override string Description => "Sets a custom location where the base data and files will be stored.";
    
    /// <summary>
    /// Gets or sets the base directory
    /// </summary>
    [CommandLineArg("" , "The directory to use as the base directory")]
    public string Directory { get; set; }
    
    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(Directory) == false)
            DirectoryHelper.BaseDirectory = Directory;
        return false;
    }
}