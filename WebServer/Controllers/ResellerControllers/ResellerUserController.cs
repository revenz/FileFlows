using FileFlows.Services.ResellerServices;

namespace FileFlows.WebServer.Controllers.ResellerControllers;

/// <summary>
/// Reseller User Controller
/// </summary>
[Route("/api/reseller/user")]
[FileFlowsAuthorize(UserRole.Admin)]
public class ResellerFlowController : BaseController
{
    /// <summary>
    /// Gets all reseller users in the system
    /// </summary>
    /// <returns>a list of all reseller users</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return BadRequest("Not licensed");
        
        var service = ServiceLoader.Load<ResellerUserService>();
        var list = await service.GetAll() ?? [];
        return Ok(list.OrderBy(x => x.Name.ToLowerInvariant()));
    }

    /// <summary>
    /// Saves a Reseller User
    /// </summary>
    /// <param name="user">The Reseller User to save</param>
    /// <returns>the saved Reseller User instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ResellerUser user)
    {
        var service = ServiceLoader.Load<ResellerUserService>();

        await service.SetTokens(user.Uid, user.Tokens);
        
        var existing = await service.GetByUid(user.Uid);
        if (existing == null)
            return NotFound();
        return Ok(existing);
    }
    
    /// <summary>
    /// Delete reseller flows from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task,</returns>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return BadRequest("Not licensed");
        
        if (model?.Uids?.Any() != true)
            return Ok();
        
        // then delete the libraries
        await ServiceLoader.Load<ResellerUserService>().Delete(model.Uids, await GetAuditDetails());
        return Ok();
    }

}