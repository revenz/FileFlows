using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will automatically delete logs for non existing library files
/// </summary>
public class LibraryFileLogPruner:ServerWorker
{
    /// <summary>
    /// Constructor for the log pruner
    /// </summary>
    public LibraryFileLogPruner() : base(ScheduleType.Daily, 5)
    {
        try
        {
            _ = ExecuteAsync();
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Executes the log pruner, Run calls this 
    /// </summary>
    protected override void ExecuteActual(Settings settings)
    {
        _ = ExecuteAsync();
    }
    
    /// <summary>
    /// Executes the pruner asynchronously
    /// </summary>
    private async Task ExecuteAsync()
    {
        var libFiles = (await ServiceLoader.Load<LibraryFileService>().GetUids())
            .Select(x => x.ToString()).ToList();

        var maxDays = (await ServiceLoader.Load<ISettingsService>().Get()).LibraryFileLogFileRetention;

        var dirInfo = new DirectoryInfo(DirectoryHelper.LibraryFilesLoggingDirectory);
        var files = dirInfo.GetFiles();
        foreach (var file in files)
        {
            // first check if the file is somewhat new, if it is, dont delete just yet
            if (file.LastWriteTimeUtc > DateTime.UtcNow.AddHours(-1))
                continue;
            
            string shortName = file.Name;
            if (file.Extension?.Length > 0)
                shortName = shortName[..shortName.LastIndexOf(file.Extension, StringComparison.Ordinal)];
            
            // .html.gz, .log.gz
            if (shortName.EndsWith(".html"))
                shortName = shortName.Replace(".html", "");
            if (shortName.EndsWith(".log"))
                shortName = shortName.Replace(".log", "");
            
            bool exists = libFiles.Contains(shortName);
            bool isOld = maxDays > 0 && file.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-maxDays);

            if (exists && isOld == false)
                continue;
            try
            {
                file.Delete();
                Logger.Instance?.DLog("Deleted library file log: " + file);
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}