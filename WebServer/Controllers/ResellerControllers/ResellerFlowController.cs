using FileFlows.Services.ResellerServices;

namespace FileFlows.WebServer.Controllers.ResellerControllers;

/// <summary>
/// Reseller Flows Controller
/// </summary>
[Route("/api/reseller/flow")]
[FileFlowsAuthorize(UserRole.Admin)]
public class ResellerFlowsController : BaseController
{
    /// <summary>
    /// Gets all reseller flows in the system
    /// </summary>
    /// <returns>a list of all reseller flows</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return BadRequest("Not licensed");
        
        var service = ServiceLoader.Load<ResellerFlowService>();
        var list = await service.GetAll() ?? [];
        return Ok(list.OrderBy(x => x.Name.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Saves a reseller flow
    /// </summary>
    /// <param name="flow">The reseller flow to save</param>
    /// <returns>the saved reseller flow instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ResellerFlow flow)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return BadRequest("Not licensed");
        
        if(flow == null)
            return BadRequest("No data provided");

        var service = ServiceLoader.Load<ResellerFlowService>();
        
        var result  = await service.Update(flow, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);

        return Ok(result.Value);
    }
    
    /// <summary>
    /// Set the enable state for a reseller flow
    /// </summary>
    /// <param name="uid">The UID of the reseller flow</param>
    /// <param name="enable">true if enabled, otherwise false</param>
    /// <returns>the updated reseller flow instance</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool enable)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reseller) == false)
            return BadRequest("Not licensed");
        
        var service = ServiceLoader.Load<ResellerFlowService>();
        var item = await service.GetByUid(uid);
        if (item == null)
            throw new Exception("Reseller Flow not found.");
        
        if (item.Enabled != enable)
        {
            item.Enabled = enable;
            await service.Update(item, await GetAuditDetails());
        }
        return Ok();
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
        await ServiceLoader.Load<ResellerFlowService>().Delete(model.Uids, await GetAuditDetails());
        return Ok();
    }

}