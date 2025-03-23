using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop custom css Controller
/// </summary>
[Route("/api/file-drop/custom-css")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropCustomCssController : BaseController
{
    /// <summary>
    /// Gets the file drop custom css
    /// </summary>
    /// <returns>the file drop custom css html</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(settings.CustomCss ?? string.Empty);
    }

    /// <summary>
    /// Save the file drop custom css
    /// </summary>
    /// <param name="css">the custom CSS for the custom css</param>
    /// <returns>A task to await</returns>
    [HttpPut]
    public async Task<IActionResult> SaveUiModel([FromBody] string css)
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var existing = service.Get();
        existing.CustomCss = css ?? string.Empty;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}