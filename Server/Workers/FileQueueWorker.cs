using FileFlows.ServerShared.Workers;
using FileFlows.Services;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that just refreshes the file queue in case any file is missed
/// </summary>
public class FileQueueWorker() : Worker(ScheduleType.Hourly, 3)
{
    /// <inheritdoc />
    protected override void Execute()
    {
        var service = ServiceLoader.Load<FileQueueService>();
        _ = service.LoadQueue();
    }
}