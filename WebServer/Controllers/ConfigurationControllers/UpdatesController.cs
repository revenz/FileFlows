using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// Updates Controller
/// </summary>
[Route("/api/configuration/updates")]
[FileFlowsAuthorize(UserRole.Admin)]
public class UpdatesController : BaseController
{

    /// <summary>
    /// Get the updates settings
    /// </summary>
    /// <returns>The updates settings</returns>
    [HttpGet]
    public async Task<UpdatesModel> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();

        UpdatesModel model = new();
        model.AutoUpdate = settings.AutoUpdate;
        model.AutoUpdateNodes = settings.AutoUpdateNodes;
        model.AutoUpdatePlugins = settings.AutoUpdatePlugins;
        return model;
    }
    
    
    /// <summary>
    /// Saves the updates model
    /// </summary>
    /// <param name="model">the updates model</param>
    [HttpPut]
    public async Task Save([FromBody] UpdatesModel model)
    {
        if (model == null)
            return;

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get() ?? new ();
        settings.AutoUpdate = model.AutoUpdate;
        settings.AutoUpdateNodes = model.AutoUpdateNodes;
        settings.AutoUpdatePlugins = model.AutoUpdatePlugins;
        
        await service.Save(settings, await GetAuditDetails(), dontUpdateRevision: true);
    }
    
    /// <summary>
    /// Triggers a check for an update
    /// </summary>
    [HttpPost("check-for-update-now")]
    public async Task<bool> CheckForUpdateNow()
    {
        var service = ServiceLoader.Load<UpdateService>();
        await service.Trigger();
        
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return false;

        if (string.IsNullOrEmpty(service.Info.FileFlowsVersion))
            return false;
        return true;
    }

    /// <summary>
    /// Triggers a upgrade now
    /// </summary>
    [HttpPost("upgrade-now")]
    public async Task UpgradeNow()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1);
            var service = ServiceLoader.Load<IOnlineUpdateService>();
            return service.RunCheck(skipEnabledCheck: true);
        });
        await Task.CompletedTask;
    }

}