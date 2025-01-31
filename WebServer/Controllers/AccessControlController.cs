using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for access control lists
/// </summary>
[Route("/api/acl")]
[FileFlowsAuthorize(UserRole.Admin)]
public class AccessControlController : BaseController
{
    /// <summary>
    /// Get all access control entries in the system
    /// </summary>
    /// <returns>A list of all access control entries</returns>
    [HttpGet]
    public Task<List<AccessControlEntry>> GetAll([FromQuery] AccessControlType? type = null)
        => ServiceLoader.Load<AccessControlService>().GetAllAsync(type);

    /// <summary>
    /// Saves a access control entry
    /// </summary>
    /// <param name="entry">The entry to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] AccessControlEntry entry)
    {
        if (entry.Uid == Guid.Empty)
        {
            // new entry
            var all = await GetAll(entry.Type);
            entry.Order = all.Any() ? all.Max(x => x.Order) + 1 : 1;
        }
        var result = await ServiceLoader.Load<AccessControlService>().Update(entry, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete access control entries
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<AccessControlService>().Delete(model.Uids, await GetAuditDetails());

    /// <summary>
    /// Moves access control entries
    /// </summary>
    /// <param name="model">A reference model containing UIDs to move</param>
    /// <param name="type">The type of entries being moved</param>
    /// <param name="up">If the items are being moved up or down</param>
    /// <returns>an awaited task</returns>
    [HttpPost("move")]
    public async Task Move([FromBody] ReferenceModel<Guid> model, [FromQuery] AccessControlType type, [FromQuery] bool up)
        => await ServiceLoader.Load<AccessControlService>().Move(model.Uids, type, up, await GetAuditDetails());
}
