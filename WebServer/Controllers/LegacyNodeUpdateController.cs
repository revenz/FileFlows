using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Legacy node updater controller
/// Will be removed in 24.05
/// </summary>
[Route("/api/system")]
public class LegacyNodeUpdateController: Controller
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    [HttpGet("version")]
    public string GetVersion() => Globals.Version.ToString();

    /// <summary>
    /// Gets the version an node update available
    /// </summary>
    /// <returns>the version an node update available</returns>
    [HttpGet("node-update-version")]
    public string GetNodeUpdateVersion()
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return string.Empty;
        return Globals.Version.ToString();
    }
    /// <summary>
    /// Gets an node update available
    /// </summary>
    /// <param name="version">the current version of the node</param>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>if there is a node update available, returns the update</returns>
    [HttpGet("node-updater-available")]
    public IActionResult GetNodeUpdater([FromQuery]string version, [FromQuery] bool windows)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        if (string.IsNullOrWhiteSpace(version))
            return new ContentResult();
        var current = new Version(Globals.Version);
        var node =  new Version(version);
        if (node >= current)
            return new ContentResult();

        return GetNodeUpdater(windows);
    }
    /// <summary>
    /// Gets the node updater
    /// </summary>
    /// <param name="windows">if the update is for a windows system</param>
    /// <returns>the node updater</returns>
    [HttpGet("node-updater")]
    public IActionResult GetNodeUpdater([FromQuery] bool windows)
    {
        if (LicenseService.IsLicensed(LicenseFlags.AutoUpdates) == false)
            return new ContentResult();
        
        string updateFile = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "Nodes",
            $"FileFlows-Node-{Globals.Version}.zip");
        if (System.IO.File.Exists(updateFile) == false)
            return new ContentResult();

        return File(System.IO.File.ReadAllBytes(updateFile), "application/zip");
    }
}