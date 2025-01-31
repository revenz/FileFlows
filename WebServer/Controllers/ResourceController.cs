namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for Resources
/// </summary>
[Route("/api/resource")]
[FileFlowsAuthorize(UserRole.Resources)]
public class ResourceController : BaseController
{
    /// <summary>
    /// Get all resources configured in the system
    /// </summary>
    /// <returns>A list of all resources</returns>
    [HttpGet]
    public async Task<IEnumerable<Resource>> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return [];
        var resources = (await ServiceLoader.Load<ResourceService>().GetAllAsync()).
        OrderBy(x => x.Name.ToLowerInvariant()).ToList();
        // don't include data
        return resources.Select(x => new Resource()
        {
            Uid = x.Uid,
            Name = x.Name,
            DateCreated = x.DateCreated,
            DateModified = x.DateModified,
            MimeType = x.MimeType,
            UsedBy = x.UsedBy
        });
    }

    /// <summary>
    /// Get scheduled task
    /// </summary>
    /// <param name="uid">The UID of the scheduled task to get</param>
    /// <returns>The scheduled task instance</returns>
    [HttpGet("{uid}")]
    public Task<Resource?> Get(Guid uid)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return null!;
        return ServiceLoader.Load<ResourceService>().GetByUidAsync(uid);
    }

    /// <summary>
    /// Get a resource by its name, case-insensitive
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <returns>The resource instance if found</returns>
    [HttpGet("name/{name}")]
    public Task<Resource?> GetByName(string name)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return null!;
        return ServiceLoader.Load<ResourceService>().GetByNameAsync(name);
    }

    /// <summary>
    /// Saves a resource
    /// </summary>
    /// <param name="resource">The resource to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Resource resource)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return null!;
        var result = await ServiceLoader.Load<ResourceService>().Update(resource, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    
    /// <summary>
    /// Delete resources from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<ResourceService>().Delete(model.Uids, await GetAuditDetails());
}
