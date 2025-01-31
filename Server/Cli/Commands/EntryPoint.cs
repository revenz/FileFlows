namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Sets the entry point of the application
/// </summary>
public class EntryPoint : Command
{
    /// <inheritdoc />
    public override string Switch => "entry-point";

    /// <inheritdoc />
    public override string Description => "Sets the entry point of the application.";

    /// <inheritdoc />
    public override bool PrintToConsole => false;

    /// <summary>
    /// Gets or sets the base directory
    /// </summary>
    [CommandLineArg("" , "The entry point of the application")]
    public string Location { get; set; }
    
    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(Location) == false)
            Application.EntryPoint = Location;
        return false;
    }
}