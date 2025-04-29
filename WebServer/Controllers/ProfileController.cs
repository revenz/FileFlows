using FileFlows.Plugin;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.ServerModels;
using FileFlows.Services;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.WebServer.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Profile Controller
/// </summary>
[Route("/api/profile")]
[FileFlowsAuthorize]
public class ProfileController : Controller
{
    /// <summary>
    /// Gets the profile
    /// </summary>
    /// <returns>the profile</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profile = await HttpContext.GetProfile();
        if (profile  == null)
            return Unauthorized();
        return Ok(profile);
    }
}