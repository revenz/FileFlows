using System.Diagnostics;
using System.IO.Compression;
using FileFlows.Plugin;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker that will automatically update the system
/// </summary>
public abstract class UpdaterWorker : Worker
{
    /// <summary>
    /// Gets if there is an updated pending installation
    /// </summary>
    public static bool UpdatePending { get; private set; }

    /// <summary>
    /// Gets the current version
    /// </summary>
    protected Version CurrentVersion { get; init; }
    
    /// <summary>
    /// The name of the updater
    /// </summary>
    protected readonly string UpdaterName;

    /// <summary>
    /// The upgrade script prefix
    /// </summary>
    private readonly string UpgradeScriptPrefix;

    private ILogger _logger = null!;

    /// <summary>
    /// Gets the logger instance to use in this worker
    /// </summary>
    protected ILogger Logger
    {
        get => _logger;
        set => _logger = value;
    } 

    /// <summary>
    /// Constructs an instance of a Update Worker
    /// </summary>
    /// <param name="upgradeScriptPrefix">The script to execute in the upgrade zip file</param>
    /// <param name="schedule">the type of schedule this worker runs at</param>
    /// <param name="interval">the interval of this worker</param>
    public UpdaterWorker(string upgradeScriptPrefix, ScheduleType schedule, int interval) : base(schedule, interval)
    {
        CurrentVersion = new Version(Globals.Version);
        this.UpgradeScriptPrefix = upgradeScriptPrefix;
        UpdaterName = this.GetType().Name;
        RunCheck();
    }

    /// <inheritdoc />
    protected override void Initialize(ScheduleType schedule, int interval)
    {
        base.Initialize(schedule, interval);
        if(_logger == null) // this might have been set a sub class, eg server updater sets this
            _logger = Shared.Logger.Instance;
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        RunCheck();
    }

    /// <summary>
    /// Gets if the update can run
    /// </summary>
    protected abstract bool CanUpdate();

    /// <summary>
    /// Quits the current application
    /// </summary>
    protected abstract void QuitApplication();

    /// <summary>
    /// Pre-check to run before executing
    /// </summary>
    /// <returns>if false, no update will be checked for</returns>
    protected virtual bool PreCheck() => true;
    
    /// <summary>
    /// Runs a check for update and if found will download it 
    /// </summary>
    /// <param name="skipEnabledCheck">if the enabled checks should be skipped</param>
    /// <returns>A update has been downloaded</returns>
    public bool RunCheck(bool skipEnabledCheck = false)
    {
        if (PreCheck() == false)
            return false;

        Logger.ILog($"{UpdaterName}: Checking for update");
        try
        {
#if(DEBUG)
            return false; // disable during debugging
#else
            string updateScript = DownloadUpdate(skipEnabledCheck);
            if (string.IsNullOrEmpty(updateScript))
                return false;

            UpdatePending = true;
            PrepareApplicationShutdown();
            Logger.ILog($"{UpdaterName}: Update pending installation");
            do
            {
                Logger.ILog($"{UpdaterName}: Waiting to run update");
                // sleep just in case something has just started
                Thread.Sleep(10_000);
            } while (CanUpdate() == false);

            Logger.ILog($"{UpdaterName} - Update about to be installed");
            RunUpdateScript(updateScript);
            return true;
#endif
        }
        catch (Exception ex)
        {
            Logger.ELog($"{UpdaterName} Error: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Prepares the application to be shutdown
    /// Called after the update has been downloaded, but before it has run
    /// </summary>
    protected virtual void PrepareApplicationShutdown()
    {
    }

    /// <summary>
    /// Called before the update script is run
    /// </summary>
    /// <param name="updateScript">the update script about to be run</param>
    protected virtual void PreRunUpdateScript(string updateScript)
    {
        
    }
    
    /// <summary>
    /// Called before any upgrade arguments have been added to inject custom ones first
    /// </summary>
    /// <param name="startInfo">the process start info</param>
    protected virtual void PreUpgradeArgumentsAdd(ProcessStartInfo startInfo) {}

    private void RunUpdateScript(string updateScript)
    {
        try
        {
            PreRunUpdateScript(updateScript);
            
            // if inside docker or systemd we just restart, the restart policy should automatically kick in then run the upgrade script when it starts
            if (Globals.IsDocker == false && Globals.IsSystemd == false)
            {
                MakeExecutable(updateScript);
                
                Logger.ILog($"{UpdaterName}About to execute upgrade script: " + updateScript);
                var fi = new FileInfo(updateScript);

                var psi = new ProcessStartInfo(updateScript);
                PreUpgradeArgumentsAdd(psi);
                psi.ArgumentList.Add(Process.GetCurrentProcess().Id.ToString());
                psi.WorkingDirectory = fi.DirectoryName;
                psi.UseShellExecute = true;
                psi.CreateNoWindow = true;
                Process.Start(psi);
            }
            
            QuitApplication();
        }
        catch (Exception ex)
        {
            Logger.ELog($"{UpdaterName}: Failed running update script: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Makes the specified file executable by changing its permissions.
    /// </summary>
    /// <param name="filename">The filename of the file to make executable.</param>
    /// <returns>True if the file was made executable successfully, otherwise false.</returns>
    public bool MakeExecutable(string filename) 
    {
        // Check if the operating system supports chmod command
        if (OperatingSystem.IsLinux() == false && OperatingSystem.IsMacOS() == false && OperatingSystem.IsFreeBSD() == false) 
            return false;

        try 
        {
            Logger.ILog("Making upgrade script executable: " + filename);
            // Execute the chmod command to make the file executable
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = "/bin/chmod",
                ArgumentList = { "+x", filename }, // Add +x to make the file executable
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process? process = Process.Start(psi);
            if (process == null)
                return false;
            
            process.WaitForExit();

            // Check the exit code to determine if chmod was successful
            if (process.ExitCode == 0)
                return true;
                
            Logger.WLog("Failed making file executable: " + process.StandardError.ReadToEnd());
            return false;
            
        } catch (Exception ex) {
            Logger.WLog($"Error making file executable: {ex.Message}");
            return false;
        }
    }
    

    /// <summary>
    /// Downloads an update
    /// </summary>
    /// <returns>The update file</returns>
    protected abstract string DownloadUpdateBinary();

    /// <summary>
    /// Gets if auto updates are enabled
    /// </summary>
    /// <returns>if auto updates are enabled</returns>
    protected abstract bool GetAutoUpdatesEnabled();

    /// <summary>
    /// Gets if an update is available
    /// </summary>
    /// <returns>if an update is available</returns>
    protected abstract bool GetUpdateAvailable();

    /// <summary>
    /// Downloads the update
    /// </summary>
    /// <param name="skipEnabledCheck">if the enabled checks should be skipped</param>
    /// <returns>the download URL</returns>
    private string DownloadUpdate(bool skipEnabledCheck)
    {
        try
        {
            if (GetUpdateAvailable() == false)
                return string.Empty;
            
            if ((skipEnabledCheck || GetAutoUpdatesEnabled()) == false)
                return string.Empty;

            Logger.DLog($"{UpdaterName}: Checking for new update binary");
            string update = DownloadUpdateBinary();
            if (string.IsNullOrEmpty(update))
            {
                Logger.DLog($"{UpdaterName}: No update available");
                return string.Empty;
            }

            Logger.DLog($"{UpdaterName}: Downloaded update: " + update);

            var updateDir = new FileInfo(update).DirectoryName;

            Logger.ILog($"{UpdaterName}: Extracting update to: " + updateDir);
            try
            {
                ZipFile.ExtractToDirectory(update, updateDir!, true);
            }
            catch (Exception)
            {
                Logger.ELog($"{UpdaterName}: Failed extract update zip, file likely corrupt during download, deleting update");
                File.Delete(update);
                return string.Empty;
            }

            Logger.ILog($"{UpdaterName}: Extracted update to: " + updateDir);
            // delete the upgrade file after extraction
            File.Delete(update);
            Logger.ILog($"{UpdaterName}: Deleted update file: " + update);

            var updateFile = Path.Combine(updateDir!, UpgradeScriptPrefix + (Globals.IsWindows ? ".bat" : ".sh"));
            if (File.Exists(updateFile) == false)
            {
                Logger.WLog($"{UpdaterName}: No update script found: " + updateFile);
                return string.Empty;
            }

            Logger.ILog($"{UpdaterName}: Update script found: " + updateFile);

            if (Globals.IsLinux && ServerShared.Helpers.FileHelper.MakeExecutable(updateFile) == false)
            {
                Logger.WLog($"{UpdaterName}: Failed to make update script executable");
                return string.Empty;
            }

            Logger.ILog($"{UpdaterName}: Upgrade directory ready: " + updateDir);
            Logger.ILog($"{UpdaterName}: Upgrade script ready: " + updateFile);

            return updateFile;
        }
        catch (Exception ex)
        {
            //if (ex.Message == "Object reference not set to an instance of an object")
            //    return string.Empty; // just ignore this error, likely due ot it not being configured yet.
            Logger.ELog($"{UpdaterName}: Failed checking for update: " + ex.Message);
            return string.Empty;
        }
    }

}