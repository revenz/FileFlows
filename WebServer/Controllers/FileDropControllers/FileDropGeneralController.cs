using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop General Settings Controller
/// </summary>
[Route("/api/file-drop/general")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropSettingsController : BaseController
{
    /// <summary>
    /// Gets the file drop general settings
    /// </summary>
    /// <returns>the file drop general settings</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            settings.Enabled,
            settings.CustomPort,
            settings.SessionExpireInMinutes,
            settings.AllowRegistrations,
            settings.RequireEmailVerification
        });
    }

    /// <summary>
    /// Save the file drop settings
    /// </summary>
    /// <param name="model">the file drop settings to save</param>
    /// <returns>The saved file drop settings</returns>
    [HttpPut]
    public async Task<IActionResult> SaveUiModel([FromBody] FileDropSettings model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        if (model == null)
            return BadRequest();
        
        model.CustomPort = Math.Clamp(model.CustomPort, 1, 65535);

        var service = ServiceLoader.Load<FileDropSettingsService>();
        var existing = service.Get();
        bool changed = existing.Enabled != model.Enabled ||
                       existing.SessionExpireInMinutes != model.SessionExpireInMinutes ||
                       existing.CustomPort != model.CustomPort;
        
        existing.Enabled = model.Enabled;
        existing.CustomPort = model.CustomPort;
        existing.SessionExpireInMinutes = model.SessionExpireInMinutes;
        existing.AllowRegistrations = model.AllowRegistrations;
        existing.RequireEmailVerification = model.RequireEmailVerification;        
        
        await service.Save(existing, await GetAuditDetails());

        if (ServiceLoader.TryLoad<IFileDropWebServerService>(out var webService))
        {
            if (existing.Enabled)
            {
                if(changed)
                    webService.Restart();
            }
            else
                webService.Stop();
        }

        return Ok();
    }

}