using FileFlows.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that run in the FileFlows Server
/// </summary>
/// <param name="schedule">the type of schedule this worker runs at</param>
/// <param name="interval">the interval of this worker</param>
/// <param name="quiet">If this worker is quiet and has reduced logging</param>
public abstract class ServerWorker (Worker.ScheduleType schedule, int interval, bool quiet = false) 
    : Worker(schedule, interval, quiet)
{

    /// <inheritdoc />
    protected override void Execute()
    {
        var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        if (settings.EulaAccepted == false)
        {
            Logger.Instance.ILog("EULA Not accepted cannot execute worker: " + GetType().Name);
            return; // cannot proceed unless they have accepted the EULA
        }

        ExecuteActual(settings);
    }

    /// <summary>
    /// Executes the actual worker
    /// </summary>
    protected abstract void ExecuteActual(Settings settings);
}