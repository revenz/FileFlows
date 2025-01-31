namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// Command line argument that sets this is running as a systemd service
/// </summary>
public class SystemdService : Command
{
    /// <inheritdoc />
    public override string Switch => "systemd-service";

    /// <inheritdoc />
    public override string Description => "Sets that this is running as a systemd service";

    /// <inheritdoc />
    public override bool PrintToConsole => false;

    /// <inheritdoc />
    public override bool Run(ILogger logger)
    {
        Globals.IsSystemd = true;
        return false;
    }
}