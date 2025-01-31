namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Sets some custom environmental variables
/// </summary>
public class EnvironmentalVariables: Command
{
    /// <inheritdoc />
    public override string Switch => "env";

    /// <inheritdoc />
    public override string Description => "Sets custom environmental variables.";

    /// <inheritdoc />
    public override bool PrintToConsole => false;
    /// <summary>
    /// Gets or sets the base directory
    /// </summary>
    [CommandLineArg("" , "The Variables")]
    public string Variables { get; set; }
    
    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(Variables))
            return true;

        var parts = Variables.Split(';');
        foreach (var part in parts)
        {
            var envPart = part.Split('=');
            if(envPart.Length != 2)
                continue;
            if (envPart[0] == "AutoUpdateUrl")
                Globals.AutoUpdateUrl = envPart[1];
            else if (envPart[0] == "FFURL")
                Globals.AutoUpdateUrl = envPart[1];
        }
        return true;
    }
}