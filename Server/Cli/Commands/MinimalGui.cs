namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Set where the base data files will be read/saved to
/// </summary>
public class MinimalGui : Command
{
    /// <inheritdoc />
    public override string Switch => "minimal-gui";

    /// <inheritdoc />
    public override string Description => "Shows the minimal GUI";
    
    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        Application.ShowMinimalGui = true;
        return false;
    }

}