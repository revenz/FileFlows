using System.Text.RegularExpressions;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;

namespace FileFlows.ServerShared.Workers;

/// <summary>
/// Worker to clean up old log files
/// </summary>
public class LogFileCleaner : Worker
{
    /// <summary>
    /// Constructs a log file cleaner
    /// </summary>
    public LogFileCleaner() : base(ScheduleType.Daily, 5)
    {
        Clean(true);
    }

    /// <summary>
    /// Executes the cleaner
    /// </summary>
    protected sealed override void Execute()
        => Clean(false);

    /// <summary>
    /// Performs the actual file cleaning
    /// </summary>
    /// <param name="startup">if this is being called on startup</param>
    private void Clean(bool startup)
    {
        var dir = DirectoryHelper.LoggingDirectory;
        var dirInfo = new DirectoryInfo(dir);
        if (startup)
        {
            var rgxToDelete = new Regex("(Dispatcher|Auto Updater|Cache|Library)", RegexOptions.IgnoreCase);
            foreach (var file in dirInfo.GetFiles("*.log"))
            {
                if (rgxToDelete.IsMatch(file.Name))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            }
        }

        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        if (settings == null)
            return; // not yet ready
        var minDate = DateTime.Now.AddDays(-(settings.LogFileRetention < 1 ? 5 : settings.LogFileRetention));
        foreach (var file in new DirectoryInfo(dir).GetFiles("*.log")
                     .OrderByDescending(x => x.LastWriteTime))
        {
            if (file.LastWriteTime > minDate)
                continue;

            try
            {
                file.Delete();
                Logger.Instance.ILog("Deleted log file: " + file.Name);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
