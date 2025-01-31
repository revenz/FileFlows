namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Shows the full GUI
/// </summary>
public class Gui : Command
{
    /// <inheritdoc />
    public override string Switch => "gui";

    /// <inheritdoc />
    public override string Description => "Shows the full standalone GUI application.";
    
    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        Application.ShowGui = true;
        return false;
    }
}