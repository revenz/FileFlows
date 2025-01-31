using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that monitors FlowRunners and will cancel
/// any "dead" runners
/// </summary>
public class FlowRunnerMonitor:ServerWorker
{
    private List<Guid> StartUpRunningFiles;
    private DateTime StartedAt = DateTime.UtcNow;

    /// <summary>
    /// Constructs a Flow Runner Monitor worker
    /// </summary>
    public FlowRunnerMonitor() : base(ScheduleType.Second, 10, quiet: true)
    {
        StartUpRunningFiles = FlowRunnerService.ExecutingLibraryFiles().Result;
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        var service = ServiceLoader.Load<FlowRunnerService>();
        service.AbortDisconnectedRunners().Wait();
        if (StartUpRunningFiles?.Any() == true)
        {
            var array = StartUpRunningFiles.ToArray();
            var lfService = ServiceLoader.Load<LibraryFileService>();
            foreach (var lf in array)
            {
                if (service.IsLibraryFileRunning(lf))
                    StartUpRunningFiles.Remove(lf);
                else if(DateTime.UtcNow > StartedAt.AddMinutes(2))
                {
                    // no update in 2minutes, kill it
                    try
                    {
                        var status = lfService.GetFileStatus(lf).Result;
                        if (status != null && status != FileStatus.Processing)
                        {
                            StartUpRunningFiles.Remove(lf);
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        // not known, silently ignore this file then
                        StartUpRunningFiles.Remove(lf);
                        continue;
                    }
                    
                    // abort the file
                    ServiceLoader.Load<FlowRunnerService>().AbortByFile(lf).Wait();
                    StartUpRunningFiles.Remove(lf);
                }
            }
        }
    }
}