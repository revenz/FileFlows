using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File drop single sign on Controller
/// </summary>
[Route("/api/file-drop/single-sign-on")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropSingleSignOnController : BaseController
{
    /// <summary>
    /// Gets the file drop single sign on
    /// </summary>
    /// <returns>the file drop single sign on</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<FileDropSettingsService>();
        var settings = service.Get();
        return Ok(new
        {
            GoogleClientId = settings.GoogleClientId ?? string.Empty,
            GoogleClientSecret = settings.GoogleClientSecret ?? string.Empty,
            MicrosoftClientId = settings.MicrosoftClientId ?? string.Empty,
            MicrosoftClientSecret = settings.MicrosoftClientSecret ?? string.Empty,
            CustomProviderName = settings.CustomProviderName ?? string.Empty,
            CustomProviderAuthority = settings.CustomProviderAuthority ?? string.Empty,
            CustomProviderClientId = settings.CustomProviderClientId ?? string.Empty,
            CustomProviderClientSecret = settings.CustomProviderClientSecret ?? string.Empty
        });
    }

    /// <summary>
    /// Save the file drop single sign on
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
        
        bool changed = existing.GoogleClientId?.EmptyAsNull() != model.GoogleClientId?.EmptyAsNull() ||
                       existing.GoogleClientSecret?.EmptyAsNull() != model.GoogleClientSecret?.EmptyAsNull() ||
                       existing.MicrosoftClientId?.EmptyAsNull() != model.MicrosoftClientId?.EmptyAsNull() ||
                       existing.MicrosoftClientSecret?.EmptyAsNull() != model.MicrosoftClientSecret?.EmptyAsNull() ||
                       existing.CustomProviderAuthority?.EmptyAsNull() != model.CustomProviderAuthority?.EmptyAsNull() ||
                       existing.CustomProviderClientId?.EmptyAsNull() != model.CustomProviderClientId?.EmptyAsNull() ||
                       existing.CustomProviderClientSecret?.EmptyAsNull() != model.CustomProviderClientSecret?.EmptyAsNull();

        if (changed == false)
            return Ok();
        
        existing.GoogleClientId = model.GoogleClientId ?? string.Empty;
        existing.GoogleClientSecret = model.GoogleClientSecret ?? string.Empty;
        existing.MicrosoftClientId = model.MicrosoftClientId ?? string.Empty;
        existing.MicrosoftClientSecret = model.MicrosoftClientSecret ?? string.Empty;
        existing.CustomProviderName = model.CustomProviderName ?? string.Empty;
        existing.CustomProviderAuthority = model.CustomProviderAuthority ?? string.Empty;
        existing.CustomProviderClientId = model.CustomProviderClientId ?? string.Empty;
        existing.CustomProviderClientSecret = model.CustomProviderClientSecret ?? string.Empty;
        
        await service.Save(existing, await GetAuditDetails());

        if (existing.Enabled &&ServiceLoader.TryLoad<IFileDropWebServerService>(out var webService))
            webService.Restart();
        
        return Ok();
    }

}