using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop HCaptcha Controller
/// </summary>
[Route("/api/file-drop/hcaptcha")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropHCaptchaController : BaseController
{
    /// <summary>
    /// Gets the file drop HCaptcha
    /// </summary>
    /// <returns>the file drop HCaptcha</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            hCaptchaSiteId = settings.hCaptchaSiteId ?? string.Empty,
            hCaptchaSecret = settings.hCaptchaSecret ?? string.Empty
        });
    }

    /// <summary>
    /// Save the file drop HCaptcha
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>A task to await</returns>
    [HttpPut]
    public async Task<IActionResult> SaveUiModel([FromBody] FileDropSettings model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var existing = service.Get();
        existing.hCaptchaSiteId = model.hCaptchaSiteId ?? string.Empty;
        existing.hCaptchaSecret = model.hCaptchaSecret ?? string.Empty;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}