using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that removes old entries from the distributed cache
/// </summary>
public class DistributedCacheCleanerWorker() : ServerWorker(ScheduleType.Minute, 10)
{
    /// <summary>
    /// The service
    /// </summary>
    private DistributedCacheService service;
    
    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        service ??= ServiceLoader.Load<DistributedCacheService>();
        
        service.CleanupExpiredEntries();
    }
}