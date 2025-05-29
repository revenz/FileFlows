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
        _ = SyncStorageSaved();
    }
    
    /// <inheritdoc />
    protected override void Execute()
    {
        _ = SyncStorageSaved();
        _ = RefreshDashboard();
    }
    
    private async Task SyncStorageSaved()
        => await ServiceLoader.Load<StatisticService>().SyncStorageSaved();
    
    private async Task RefreshDashboard()
        => await ServiceLoader.Load<DashboardFileOverviewService>().RefreshAsync();
}