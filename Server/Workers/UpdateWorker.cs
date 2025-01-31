using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Update Worker that will update the server
/// </summary>
public class UpdateWorker : ServerWorker
{
    /// <summary>
    /// Create a new instance of the Update Worker
    /// </summary>
    public UpdateWorker() : base(ScheduleType.Hourly, 1)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("AutoUpdateInterval") ?? string.Empty, out int minutes) &&
            minutes > 0)
        {
            base.Initialize(ScheduleType.Minute, minutes);
        }
        Trigger();
    }
    
    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        ServiceLoader.Load<UpdateService>().Trigger().Wait();
    }
}