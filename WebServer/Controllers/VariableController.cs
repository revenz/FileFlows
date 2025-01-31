using FileFlows.WebServer.Authentication;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Variable Controller
/// </summary>
[Route("/api/variable")]
[FileFlowsAuthorize(UserRole.Variables)]
public class VariableController : BaseController
{   
    /// <summary>
    /// Get all variables configured in the system
    /// </summary>
    /// <returns>A list of all configured variables</returns>
    [HttpGet]
    public async Task<IEnumerable<Variable>> GetAll() 
        => (await ServiceLoader.Load<VariableService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Get variable
    /// </summary>
    /// <param name="uid">The UID of the variable to get</param>
    /// <returns>The variable instance</returns>
    [HttpGet("{uid}")]
    public Task<Variable?> Get(Guid uid)
        => ServiceLoader.Load<VariableService>().GetByUidAsync(uid);

    /// <summary>
    /// Get a variable by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <returns>The variable instance if found</returns>
    [HttpGet("name/{name}")]
    public Task<Variable?> GetByName(string name)
        => ServiceLoader.Load<VariableService>().GetByName(name);

    /// <summary>
    /// Saves a variable
    /// </summary>
    /// <param name="variable">The variable to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Variable variable)
    {
        var result = await ServiceLoader.Load<VariableService>().Update(variable, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete variables from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<VariableService>().Delete(model.Uids, await GetAuditDetails());
}