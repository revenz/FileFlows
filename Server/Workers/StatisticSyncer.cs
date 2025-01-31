using FileFlows.Services;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that resyncs the statistics data
/// </summary>
public class StatisticSyncer : Worker
{
    /// <summary>
    /// Constructs a new instance of the worker
    /// </summary>
    public StatisticSyncer() : base(ScheduleType.Daily, 4)
    {
        Trigger();
    }
    
    /// <inheritdoc />
    protected override void Execute()
    {
        _ = ServiceLoader.Load<StatisticService>().SyncStorageSaved();
    }
}