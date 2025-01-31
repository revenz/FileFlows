using System.Text;
using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Library controller
/// </summary>
[Route("/api/dockermod")]
[FileFlowsAuthorize(UserRole.DockerMods)]
public class DockerModController : BaseController
{
    /// <summary>
    /// Gets all DockerMods in the system
    /// </summary>
    /// <returns>a list of all DockerMods</returns>
    [HttpGet]
    public async Task<IEnumerable<DockerMod>> GetAll()
    {
        var items = (await ServiceLoader.Load<DockerModService>().GetAll())
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name.ToLowerInvariant());
        var repo = await ServiceLoader.Load<RepositoryService>().GetRepository();
        foreach (var item in items)
        {
            if (item.Repository == false)
                continue;
            var ro = repo.DockerMods.FirstOrDefault(x => x.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase));
            if (ro != null)
                item.LatestRevision = ro.Revision;
        }
        return items;
    }

    /// <summary>
    /// Saves a DockerMod
    /// </summary>
    /// <param name="mod">The DockerMod to save</param>
    /// <returns>the saved DockerMod instance</returns>
    [HttpPost]
    public async Task<DockerMod> Save([FromBody] DockerMod mod)
    {
        ++mod.Revision;
        var result = await ServiceLoader.Load<DockerModService>().Save(mod, await GetAuditDetails());
        if (result.Failed(out string error))
            BadRequest(error);
        return result.Value;
    }

    /// <summary>
    /// Exports a DockerMod
    /// </summary>
    /// <param name="uid">the UID of the DockerMod</param>
    /// <returns>The file download result</returns>
    [HttpGet("export/{uid}")]
    public async Task<IActionResult> Export([FromRoute] Guid uid)
    {
        var mod = await ServiceLoader.Load<DockerModService>().Export(uid);
        if (mod.IsFailed)
            return NotFound();
        
        var data = Encoding.UTF8.GetBytes(mod.Value.Content);
        return File(data, "application/octet-stream", mod.Value.Name + ".sh");
    }

    /// <summary>
    /// Delete DockerMods from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<DockerModService>().Delete(model.Uids, await GetAuditDetails());
    
    /// <summary>
    /// Set state of a DockerMod
    /// </summary>
    /// <param name="uid">The UID of the DockerMod</param>
    /// <param name="enable">Whether this DockerMod is enabled</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<DockerModService>();
        var item = await service.GetByUid(uid);
        if (item == null)
            return BadRequest("DockerMod not found.");
        if (enable != null && item.Enabled != enable.Value)
        {
            item.Enabled = enable.Value;
            item = await service.Save(item, await GetAuditDetails());
        }
        return Ok(item);
    }
    
    /// <summary>
    /// Moves DockerMods
    /// </summary>
    /// <param name="model">A reference model containing UIDs to move</param>
    /// <param name="up">If the items are being moved up or down</param>
    /// <returns>an awaited task</returns>
    [HttpPost("move")]
    public async Task Move([FromBody] ReferenceModel<Guid> model, [FromQuery] bool up)
        => await ServiceLoader.Load<DockerModService>().Move(model.Uids, up, await GetAuditDetails());
}