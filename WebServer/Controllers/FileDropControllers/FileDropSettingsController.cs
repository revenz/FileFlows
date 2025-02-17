using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop Settings Controller
/// </summary>
[Route("/api/file-drop/settings")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropSettingsController : BaseController
{
    /// <summary>
    /// Gets the file drop settings
    /// </summary>
    /// <returns>the file drop settings</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(settings);
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

        var service = ServiceLoader.Load<FileDropSettingsService>();
        await service.Save(model, await GetAuditDetails());

        if (ServiceLoader.TryLoad<IFileDropWebServerService>(out var webService))
        {
            if(model.Enabled)
                webService.Restart();
            else
                webService.Stop();
        }

        return Ok();
    }

}