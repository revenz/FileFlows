using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop auto tokens Controller
/// </summary>
[Route("/api/file-drop/auto-tokens")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropAutoTokensController : BaseController
{
    /// <summary>
    /// Gets the file drop auto tokens
    /// </summary>
    /// <returns>the file drop auto tokens</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            settings.AutoTokens,
            settings.AutoTokensAmount,
            settings.AutoTokensMaximum,
            settings.AutoTokensPeriodMinutes,
        });
    }

    /// <summary>
    /// Save the file drop auto tokens
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
        existing.AutoTokens = model.AutoTokens;
        existing.AutoTokensAmount = model.AutoTokensAmount;
        existing.AutoTokensMaximum = model.AutoTokensMaximum;
        existing.AutoTokensPeriodMinutes = model.AutoTokensPeriodMinutes;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}