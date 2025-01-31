namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Command line argument that sets this is running inside docker
/// </summary>
public class Docker : Command
{
    /// <inheritdoc />
    public override string Switch => "docker";

    /// <inheritdoc />
    public override string Description => "Sets that this is running inside a docker container";

    /// <inheritdoc />
    public override bool PrintToConsole => false;

    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        Globals.IsDocker = true;
        return false;
    }
}