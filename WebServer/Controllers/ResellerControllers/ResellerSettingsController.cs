using FileFlows.Services.ResellerServices;

namespace FileFlows.WebServer.Controllers.ResellerControllers;

/// <summary>
/// Reseller Settings Controller
/// </summary>
[Route("/api/reseller/settings")]
[FileFlowsAuthorize(UserRole.Admin)]
public class ResellerSettingsController : BaseController
{
    /// <summary>
    /// Gets the reseller settings
    /// </summary>
    /// <returns>the reseller settings</returns>
    [HttpGet]
    public IActionResult Get()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return new UnauthorizedResult();
        
        var service = ServiceLoader.Load<ResellerSettingsService>();
        var settings = service.Get();
        return Ok(settings);
    }

    /// <summary>
    /// Save the reseller settings
    /// </summary>
    /// <param name="model">the reseller settings to save</param>
    /// <returns>The saved reseller settings</returns>
    [HttpPut]
    public async Task<IActionResult> SaveUiModel([FromBody] ResellerSettings model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return new UnauthorizedResult();
        
        if (model == null)
            return BadRequest();

        var service = ServiceLoader.Load<ResellerSettingsService>();
        await service.Save(model, await GetAuditDetails());
        
        if(ServiceLoader.TryLoad<IResellerWebServerService>(out var webService))
            webService.Restart();
        
        return Ok();
    }

}