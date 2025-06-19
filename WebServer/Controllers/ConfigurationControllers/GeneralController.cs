using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// General Controller
/// </summary>
[Route("/api/configuration/general")]
[FileFlowsAuthorize(UserRole.Admin)]
public class GeneralController : BaseController
{
    /// <summary>
    /// Get the general settings
    /// </summary>
    /// <returns>The general settings</returns>
    [HttpGet]
    public async Task<GeneralModel> Get()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get() ?? new ();
        var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;

        GeneralModel model = new();
        model.Language = settings.Language;
        model.ScanWhenPaused = settings.ScanWhenPaused;
        model.MaxPageSize = settings.MaxPageSize;
        model.KeepFailedFlowTempFiles = settings.KeepFailedFlowTempFiles;
        model.UseTempFilesWhenMovingOrCopying = settings.UseTempFilesWhenMovingOrCopying;
        model.DisableTelemetry = settings.DisableTelemetry;
        model.DockerModsOnServer = appSettings.DockerModsOnServer;
        model.IsLicensed = LicenseService.IsLicensed();
        return model;
    }
    
    /// <summary>
    /// Saves the general model
    /// </summary>
    /// <param name="model">the general model</param>
    [HttpPut]
    public async Task Save([FromBody] GeneralModel model)
    {
        if (model == null)
            return;

        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get() ?? new ();
        settings.Language = model.Language;
        settings.ScanWhenPaused = model.ScanWhenPaused;
        settings.MaxPageSize = model.MaxPageSize;
        settings.KeepFailedFlowTempFiles = model.KeepFailedFlowTempFiles;
        settings.UseTempFilesWhenMovingOrCopying = model.UseTempFilesWhenMovingOrCopying;
        settings.DisableTelemetry = LicenseService.IsLicensed() && model.DisableTelemetry;
        await service.Save(settings, await GetAuditDetails());

        var appSettingsService = ServiceLoader.Load<AppSettingsService>();
        appSettingsService.Settings.DockerModsOnServer = model.DockerModsOnServer;
        appSettingsService.Save();
        
        ServiceLoader.Load<PausedService>().ScanWhenPaused = model.ScanWhenPaused;
    }
}