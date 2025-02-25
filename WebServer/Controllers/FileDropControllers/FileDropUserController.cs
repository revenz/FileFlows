using FileFlows.Services.FileDropServices;

namespace FileFlows.WebServer.Controllers.FileDropControllers;

/// <summary>
/// File Drop User Controller
/// </summary>
[Route("/api/file-drop/user")]
[FileFlowsAuthorize(UserRole.Admin)]
public class FileDropUserController : BaseController
{
    /// <summary>
    /// Gets all file drop users in the system
    /// </summary>
    /// <returns>a list of all file drop users</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return BadRequest("Not licensed");
        
        var service = ServiceLoader.Load<FileDropUserService>();
        var list = await service.GetAll() ?? [];
        return Ok(list.OrderBy(x => x.Name.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Set state of the FileDrop User
    /// </summary>
    /// <param name="uid">The UID of the FileDrop</param>
    /// <param name="enable">Whether or not this FileDrop is enabled</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<FileDropUserService>();
        var fdu = await service.GetByUid(uid);
        if (fdu == null)
            return BadRequest("FileDrop User not found.");
        if (enable == null || fdu.Enabled == enable.Value) 
            return Ok();
        
        fdu.Enabled = enable.Value;
        await service.Update(fdu, await GetAuditDetails());
        return Ok();
    }

    /// <summary>
    /// Saves a file drop User
    /// </summary>
    /// <param name="user">The file drop User to save</param>
    /// <returns>the saved file drop User instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FileDropUser user)
    {
        var service = ServiceLoader.Load<FileDropUserService>();

        await service.SetTokens(user.Uid, user.Tokens);
        
        var existing = await service.GetByUid(user.Uid);
        if (existing == null)
            return NotFound();
        return Ok(existing);
    }
    
    /// <summary>
    /// Delete file drop flows from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task,</returns>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return BadRequest("Not licensed");
        
        if (model?.Uids?.Any() != true)
            return Ok();
        
        // then delete the libraries
        await ServiceLoader.Load<FileDropUserService>().Delete(model.Uids, await GetAuditDetails());
        return Ok();
    }

}