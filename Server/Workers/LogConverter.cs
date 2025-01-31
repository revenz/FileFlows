using FileFlows.Helpers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that will convert logs to/from a compressed format, depending on the system setting
/// </summary>
public class LogConverter:ServerWorker
{
    /// <summary>
    /// Creates a new instance of the Log Converter 
    /// </summary>
    public LogConverter() : base(ScheduleType.Hourly, 3)
    {
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        Run();
    }
     
    /// <summary>
    /// Runs the log converter
    /// </summary>
    internal void Run()
    {
        var files = new DirectoryInfo(DirectoryHelper.LibraryFilesLoggingDirectory).GetFiles();
        foreach (var file in files)
        {
            if(file.LastWriteTime > DateTime.Now.AddHours(-3))
                continue; //file is too new, dont process it yet
            
            if (file.Extension == ".log")
            {
                // need to create a gz file
                Gzipper.CompressFile(file.FullName, file.FullName[..^4] + ".log.gz", true);
                continue;
            }
            
            if (file.FullName.EndsWith(".html"))
            {
                // old html log file, compress it
                Gzipper.CompressFile(file.FullName, file.FullName + ".gz", true);
                continue;
            }
        }
    }
}