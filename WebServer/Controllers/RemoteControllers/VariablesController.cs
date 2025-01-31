using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Variables controller
/// </summary>
[Route("/remote/variables")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class VariablesController : Controller
{
    /// <summary>
    /// Get all variables configured in the system
    /// </summary>
    /// <returns>A list of all configured variables</returns>
    [HttpGet]
    public async Task<IEnumerable<Variable>> GetAll() 
        => (await ServiceLoader.Load<VariableService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());
}