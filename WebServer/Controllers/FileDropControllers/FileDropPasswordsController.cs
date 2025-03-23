using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop passwords Controller
/// </summary>
[Route("/api/file-drop/passwords")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropPasswordsController : BaseController
{
    /// <summary>
    /// Gets the file drop passwords
    /// </summary>
    /// <returns>the file drop passwords</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            FormsMinLength = settings.FormsMinLength == 0 ? 8 : settings.FormsMinLength,
            settings.FormsRequireDigits,
            settings.FormsRequireMixedCase,
            settings.FormsRequireSpecialCharacters
        });
    }

    /// <summary>
    /// Save the file drop passwords
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
        existing.FormsMinLength = model.FormsMinLength;
        existing.FormsRequireDigits = model.FormsRequireDigits;
        existing.FormsRequireMixedCase = model.FormsRequireMixedCase;
        existing.FormsRequireSpecialCharacters = model.FormsRequireSpecialCharacters;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}