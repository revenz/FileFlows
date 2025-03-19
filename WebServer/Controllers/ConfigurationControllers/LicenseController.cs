using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// License Controller
/// </summary>
[Route("/api/configuration/license")]
[FileFlowsAuthorize(UserRole.Admin)]
public class LicenseController : BaseController
{
    /// <summary>
    /// Get the license settings
    /// </summary>
    /// <returns>The license settings</returns>
    [HttpGet]
    public LicenseModel Get()
    {
        var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;
        var licenseService = ServiceLoader.Load<LicenseService>();
        var license = licenseService.GetLicense();
        if ((license == null || license.Status == LicenseStatus.Unlicensed) &&
            string.IsNullOrWhiteSpace(appSettings.LicenseKey) == false)
        {
            license = new();
            license.Status = LicenseStatus.Invalid;
        }
        
        LicenseModel model = new();
        
        model.LicenseKey = appSettings.LicenseKey ?? string.Empty;
        model.LicenseEmail  = appSettings.LicenseEmail ?? string.Empty;
        model.LicenseFiles = license == null ? string.Empty :
            license.Files >= 1_000_000_000 ? "Unlimited" : license.Files.ToString();
        model.LicenseFlags = license?.Flags ?? 0;
        model.LicenseLevel = license?.Level ?? 0;
        if(license != null && (license.Flags & LicenseFlags.FileDrop) == LicenseFlags.FileDrop)
            model.LicensedFileDropUsers = license.FileDropUsers;
        
        model.LicenseProcessingNodes = licenseService.GetLicensedProcessingNodes();
        model.LicenseExpiryDate = license == null ? DateTime.MinValue : license.ExpirationDateUtc.ToLocalTime();
        model.LicenseStatus = (license == null ? LicenseStatus.Unlicensed : license.Status).ToString();

        return model;
    }
    
    
    /// <summary>
    /// Saves the license model
    /// </summary>
    /// <param name="model">the license model</param>
    [HttpPut]
    public async Task Save([FromBody] LicenseModel model)
    {
        if (model == null)
            return;

        var service = ServiceLoader.Load<AppSettingsService>();
        var appSettings = service.Settings;
        appSettings.LicenseKey = model.LicenseKey?.Trim() ?? string.Empty;
        appSettings.LicenseEmail = model.LicenseEmail?.Trim() ?? string.Empty;
        service.Save();
        
        var licenseService = ServiceLoader.Load<LicenseService>();
        await licenseService.Update();
    }
}