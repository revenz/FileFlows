using FileFlows.NodeClient;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Node.Workers;

/// <summary>
/// Worker to automatically download node updates from the FileFlows server
/// </summary>
public class NodeUpdater:UpdaterWorker
{
    private static NodeUpdater? Instance;
    
    /// <summary>
    /// Constructs an instance of the node updater worker
    /// </summary>
    public NodeUpdater() : base("node-upgrade", ScheduleType.Daily, 3)
    {
        Instance = this;
        EventManager.Subscribe("NodeVersionMismatch", (string version) =>
        {
            Trigger();
        });

    }

    /// <summary>
    /// Checks for an update now
    /// </summary>
    internal static void CheckForUpdate()
    {
        if (Instance == null)
            return;
        Instance.Trigger();
    }


    /// <summary>
    /// Gets if an update can currently run
    /// </summary>
    /// <returns>true if the update can run, otherwise false</returns>
    protected override bool CanUpdate()
        => ServiceLoader.Load<RunnerManager>().HasActiveRunners == false;

    /// <summary>
    /// Quits the application so the update can be applied
    /// </summary>
    protected override void QuitApplication()
    {
        Logger.ILog($"{UpdaterName}: Quiting Application");
        // systemd needs an OK status not to auto restart, we dont want to auto restart that when upgrading
        Program.Quit(Globals.IsSystemd ? 0 : 99);
    }

    /// <inheritdoc />
    protected override void PreUpgradeArgumentsAdd(ProcessStartInfo startInfo)
    {
        Logger.ILog("Is MacOS: " + OperatingSystem.IsMacOS());
        bool hasEntryPoint = string.IsNullOrWhiteSpace(Program.EntryPoint) == false;
        Logger.ILog("Has Entry Point: " + hasEntryPoint);
        if (OperatingSystem.IsMacOS() && hasEntryPoint)
        {
            Logger.ILog("Upgrading Mac App");
            startInfo.ArgumentList.Add("mac");
            startInfo.ArgumentList.Add(Program.EntryPoint!);
            startInfo.ArgumentList.Add(Globals.Version.Split('.').Last());
        }
        base.PreUpgradeArgumentsAdd(startInfo);
    }
    
    /// <summary>
    /// Downloads the binary update from the FileFlows server
    /// </summary>
    /// <returns>the downloaded binary filename</returns>
    protected override string DownloadUpdateBinary()
    {   
        Logger.DLog("Checking for auto update");
        var service = ServiceLoader.Load<ISettingsService>();
        var serverVersion = service.GetServerVersion().Result;
        Logger.DLog("Checking for auto update: " + serverVersion);
        if (serverVersion == CurrentVersion)
            return string.Empty;

        Logger.ILog($"New Node version {serverVersion} detected, starting download");

        var nodeService = ServiceLoader.Load<INodeService>();
        var data = nodeService.GetNodeUpdater().Result;
        if (data?.Any() != true)
        {
            Logger.WLog("Failed to download Node updater.");
            return string.Empty;
        }

        var updateDir = Path.Combine(DirectoryHelper.BaseDirectory, "NodeUpdate");
        if (Directory.Exists(updateDir)) // delete the update dir so we get a full fresh update
            Directory.Delete(updateDir, true);
        Directory.CreateDirectory(updateDir);

        string update = Path.Combine(updateDir, "update.zip");
        File.WriteAllBytes(update, data);
        return update;
    }

    /// <summary>
    /// Gets if automatic updates should be downloaded
    /// </summary>
    /// <returns>true if automatic updates are enabled</returns>
    protected override bool GetAutoUpdatesEnabled()
    {
        var settingsService = ServiceLoader.Load<INodeService>();
        return settingsService.AutoUpdateNodes().Result;
    }

    /// <inheritdoc />
    protected override bool GetUpdateAvailable()
    {
        var service = ServiceLoader.Load<INodeService>();
        var serverVersion = service.GetNodeUpdateVersion().Result;
        Logger.DLog("Checking for auto update: " + serverVersion);
        if (serverVersion.Major == 0)
            return false; // means not licensed for auto updates
        return CurrentVersion != serverVersion;
    }
}