using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.ServerShared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// System remote controller
/// </summary>
[Route("/remote/system")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class SystemController : Controller
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    [HttpGet("version")]
    public string GetVersion() => Globals.Version;

    /// <summary>
    /// Gets if the server is licensed
    /// </summary>
    [HttpGet("is-licensed")]
    public bool GetIsLicensed() => LicenseService.IsLicensed();

}