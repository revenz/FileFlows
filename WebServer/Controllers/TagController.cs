using FileFlows.WebServer.Authentication;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Tag Controller
/// </summary>
[Route("/api/tag")]
[FileFlowsAuthorize]
public class TagController : BaseController
{   
    /// <summary>
    /// Get all tags configured in the system
    /// </summary>
    /// <returns>A list of all configured tags</returns>
    [HttpGet]
    public async Task<IEnumerable<Tag>> GetAll() 
        => (await ServiceLoader.Load<TagService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Get tag
    /// </summary>
    /// <param name="uid">The UID of the tag to get</param>
    /// <returns>The tag instance</returns>
    [HttpGet("{uid}")]
    public Task<Tag?> Get(Guid uid)
        => ServiceLoader.Load<TagService>().GetByUidAsync(uid);

    /// <summary>
    /// Get a tag by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the tag</param>
    /// <returns>The tag instance if found</returns>
    [HttpGet("name/{name}")]
    public Task<Tag?> GetByName(string name)
        => ServiceLoader.Load<TagService>().GetByName(name);

    /// <summary>
    /// Saves a tag
    /// </summary>
    /// <param name="tag">The tag to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    [FileFlowsAuthorize(UserRole.Tags)]
    public async Task<IActionResult> Save([FromBody] Tag tag)
    {
        var result = await ServiceLoader.Load<TagService>().Update(tag, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete tags from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    [FileFlowsAuthorize(UserRole.Tags)]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<TagService>().Delete(model.Uids, await GetAuditDetails());
}