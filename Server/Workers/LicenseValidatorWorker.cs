using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to validate and refresh the user license
/// </summary>
class LicenseValidatorWorker : ServerWorker
{
    
    /// <summary>
    /// Creates a new instance of the license validator worker
    /// </summary>
    public LicenseValidatorWorker() : base(ScheduleType.Daily, 1)
    {
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings) 
        => FileFlows.Services.ServiceLoader.Load<LicenseService>().Update().Wait();
}