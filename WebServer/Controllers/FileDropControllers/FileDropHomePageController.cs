using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop home page Controller
/// </summary>
[Route("/api/file-drop/home-page")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropHomePageController : BaseController
{
    /// <summary>
    /// Gets the file drop home page
    /// </summary>
    /// <returns>the file drop home page html</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(settings.HomePageHtml ?? string.Empty);
    }

    /// <summary>
    /// Save the file drop home page
    /// </summary>
    /// <param name="html">the custom HTML for the home page</param>
    /// <returns>A task to await</returns>
    [HttpPut]
    public async Task<IActionResult> SaveUiModel([FromBody] string html)
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var existing = service.Get();
        existing.HomePageHtml = html ?? string.Empty;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}