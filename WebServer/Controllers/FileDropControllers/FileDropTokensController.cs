using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop Tokens Controller
/// </summary>
[Route("/api/file-drop/tokens")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropTokensController : BaseController
{
    /// <summary>
    /// Gets the file drop Tokens
    /// </summary>
    /// <returns>the file drop Tokens</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            TokenPurchaseUrl = settings.TokenPurchaseUrl ?? string.Empty,
            settings.TokenPurchaseInPopup,
            settings.NewUserTokens
        });
    }

    /// <summary>
    /// Save the file drop Tokens
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
        existing.TokenPurchaseUrl = model.TokenPurchaseUrl ?? string.Empty;
        existing.TokenPurchaseInPopup = model.TokenPurchaseInPopup;
        existing.NewUserTokens = model.NewUserTokens;
        
        await service.Save(existing, await GetAuditDetails());
        return Ok();
    }

}