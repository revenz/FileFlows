using System.Diagnostics;
using FileFlows.Shared.Formatters;
using FileFlows.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that automatically updates FileFlows
/// </summary>
public class ServerUpdater : UpdaterWorker, IOnlineUpdateService
{
    private static string UpdateUrl = Globals.FileFlowsDotComUrl + "/auto-update";

    internal static ServerUpdater Instance;
    private Version? NotifiedUpdateVersion;
    private Version? DownloadedVersion;
    
    /// <summary>
    /// Creates an instance of a worker to automatically update FileFlows Server
    /// </summary>
    public ServerUpdater() : base("server-upgrade", ScheduleType.Daily, 4) 
    {
        Instance = this;
        ServiceLoader.AddSpecialCase<IOnlineUpdateService>(this);
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        if (settings.EulaAccepted == false)
            return; // cannot execute if EULA not accepted
        base.Execute();
    }

    /// <summary>
    /// Pre-check to run before executing
    /// </summary>
    /// <returns>if false, no update will be checked for</returns>
    protected override bool PreCheck() => LicenseService.IsLicensed(LicenseFlags.AutoUpdates);

    /// <inheritdoc />
    protected override void PreUpgradeArgumentsAdd(ProcessStartInfo startInfo)
    {
        Logger.ILog("Is MacOS: " + OperatingSystem.IsMacOS());
        bool hasEntryPoint = string.IsNullOrWhiteSpace(Application.EntryPoint) == false;
        Logger.ILog("Has Entry Point: " + hasEntryPoint);
        if (OperatingSystem.IsMacOS() && hasEntryPoint)
        {
            Logger.ILog("Upgrading Mac App");
            startInfo.ArgumentList.Add("mac");
            startInfo.ArgumentList.Add(Application.EntryPoint);
            startInfo.ArgumentList.Add(Globals.Version.Split('.').Last());
        }
        base.PreUpgradeArgumentsAdd(startInfo);
    }

    /// <inheritdoc />
    protected override void Initialize(ScheduleType schedule, int interval)
    {
        var logger = new Logger();
        logger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "AutoUpdater", false));
        Logger = logger;
        
        if (int.TryParse(Environment.GetEnvironmentVariable("AutoUpdateInterval") ?? string.Empty, out int minutes) &&
            minutes > 0)
        {
            Logger.DLog($"{nameof(ServerUpdater)}: Using Auto Update Interval: " + minutes + " minute" + (minutes == 1 ? "" : "s"));
            interval = minutes;
            schedule = ScheduleType.Minute;
        }

        var updateUrl = Globals.AutoUpdateUrl?.EmptyAsNull() ?? Environment.GetEnvironmentVariable("AutoUpdateUrl");
        if (string.IsNullOrEmpty(updateUrl) == false)
        {
            if (updateUrl.EndsWith("/"))
                updateUrl = updateUrl[..^1];
            Logger.DLog($"{nameof(ServerUpdater)}: Using Auto Update URL: " + updateUrl);
            UpdateUrl = updateUrl;
        }
        base.Initialize(schedule, interval);
    }

    /// <summary>
    /// Quits the FileFlows server application
    /// </summary>
    protected override void QuitApplication()
    {
        Logger.ILog($"{nameof(ServerUpdater)} - Exiting Application to run update");
        WorkerManager.StopWorkers();
        // systemd needs an OK status not to auto restart, we dont want to auto restart that when upgrading
        Environment.Exit(Globals.IsSystemd ? 0 : 99);  
    }
    
    /// <summary>
    /// Gets if auto updates are enabled
    /// </summary>
    /// <returns>if auto updates are enabled</returns>
    protected override bool GetAutoUpdatesEnabled()
    {
        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        return settings?.AutoUpdate == true;
    }

    /// <summary>
    /// Checks if an update can run now, ie if no flow runners are processing
    /// </summary>
    /// <returns>if an update can run now</returns>
    protected override bool CanUpdate()
    {
        var service = ServiceLoader.Load<FlowRunnerService>();
        var workers = service.GetExecutors()?.Result;
        return workers?.Any() != true;
    }

    /// <summary>
    /// Prepares the application for shutdown, ie stops all workers
    /// </summary>
    protected override void PrepareApplicationShutdown()
    {
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Runs any pre update scripts
    /// </summary>
    /// <param name="updateScript">a script to run</param>
    protected override void PreRunUpdateScript(string updateScript)
    {
        if (DownloadedVersion != null)
        {
            var service = ServiceLoader.Load<ISystemEventsService>();
            service.TriggerServerUpdating(DownloadedVersion.ToString());
        }
    }

    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>true if an update is available</returns>
    protected override bool GetUpdateAvailable()
    {
        var result = GetLatestOnlineVersion();
        if (result.updateAvailable)
        {
            if (NotifiedUpdateVersion != result.onlineVersion)
            {
                var service = ServiceLoader.Load<ISystemEventsService>();
                service.TriggerServerUpdateAvailable(result.onlineVersion.ToString());
                NotifiedUpdateVersion = result.onlineVersion;
            }
        }
        return result.updateAvailable;
    }

    /// <summary>
    /// Downloads the binary update
    /// </summary>
    /// <returns>the location of the saved binary file</returns>
    protected override string DownloadUpdateBinary()
    {
        var result = GetLatestOnlineVersion();
        if (result.updateAvailable == false)
            return string.Empty;
        
        Version onlineVersion = result.onlineVersion;

        string updateDirectory = Path.Combine(DirectoryHelper.BaseDirectory, "Update");

        string file = Path.Combine(updateDirectory, $"FileFlows-{onlineVersion}.zip");
        if (File.Exists(file))
        {
            string size = FileSizeFormatter.Format(new FileInfo(file).Length);
            Logger.ILog($"{UpdaterName}: Update already downloaded: {file} ({size})");
            return file;
        }

        if (Directory.Exists(updateDirectory))
            Directory.Delete(updateDirectory, true);
        Directory.CreateDirectory(updateDirectory);

        Logger.ILog($"{UpdaterName}: Downloading update: " + onlineVersion);
        
        
        string url = $"{UpdateUrl}/download/{onlineVersion}?ts={DateTime.UtcNow.Ticks}";
        HttpHelper.DownloadFile(url, file).Wait();
        if (File.Exists(file) == false)
        {
            Logger.WLog($"{UpdaterName}: Download failed");
            return string.Empty;
        }

        string dlSize = FileSizeFormatter.Format(new FileInfo(file).Length);
        Logger.ILog($"{UpdaterName}: Download complete: {file} ({dlSize})");
        DownloadedVersion = result.onlineVersion;
        return file;
    }

    /// <summary>
    /// Gets the latest version available online
    /// </summary>
    /// <returns>The latest version available online</returns>
    public (bool updateAvailable, Version onlineVersion) GetLatestOnlineVersion()
    {
        try
        {
            string url = UpdateUrl + $"/latest-version?version={Globals.Version}&platform=";
            if (Globals.IsDocker)
                url += "docker";
            else if (OperatingSystem.IsWindows())
                url += "windows";
            else if (OperatingSystem.IsLinux())
                url += "linux";
            else if (OperatingSystem.IsMacOS())
                url += "macos";
            var result = HttpHelper.Get<string>(url, noLog: true).Result;
            if (result.Success == false)
            {
                Logger.ILog($"{nameof(ServerUpdater)}: Failed to retrieve online version");
                return (false, new Version(0, 0, 0, 0));
            }

            Version current = new Version(Globals.Version);
            Version? onlineVersion;
            if (Version.TryParse(result.Data, out onlineVersion) == false)
            {
                Logger.ILog($"{nameof(ServerUpdater)}: Failed to parse online version: " + result.Data);
                return (false, new Version(0, 0, 0, 0));
            }
            
            if (current.Revision >= onlineVersion.Revision)
            {
                Logger.ILog(
                    $"{nameof(ServerUpdater)}: Current version '{Globals.Version}' newer or same as online version '{onlineVersion}' [{current.Revision} v {onlineVersion.Revision}]");
                return (false, onlineVersion);
            }

            return (true, onlineVersion);
        }
        catch (Exception ex)
        {
            while(ex.InnerException != null)
                ex = ex.InnerException;
            Logger.ELog($"{nameof(ServerUpdater)}: Failed checking online version: " + ex.Message);
            return (false, new Version(0, 0, 0, 0));
        }
    }
}
