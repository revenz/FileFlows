using System.Text.RegularExpressions;
using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Remote flow controllers
/// </summary>
[Route("/remote/configuration")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class ConfigurationController : Controller
{
    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current revision</returns>
    [HttpGet("revision")]
    public Task<int> GetCurrentConfigRevision()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
    
    /// <summary>
    /// Loads the current configuration
    /// </summary>
    /// <returns>the current configuration</returns>
    [HttpGet("current-config")]
    public Task<ConfigurationRevision?> GetCurrentConfig()
        => ServiceLoader.Load<ISettingsService>().GetCurrentConfiguration();
    
    /// <summary>
    /// Loads the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    [HttpGet("settings")]
    public Task<Settings?> GetSettings()
        => ServiceLoader.Load<ISettingsService>().Get();
    
    /// <summary>
    /// Downloads a plugin
    /// </summary>
    /// <param name="name">the name of the plugin</param>
    /// <returns>the plugin file</returns>
    [HttpGet("download-plugin/{name}")]
    public IActionResult DownloadPlugin(string name)
    {
        try
        {
            Logger.Instance?.ILog("DownloadPlugin: " + name);
            if (Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9\-\._+]+\.ffplugin$", RegexOptions.CultureInvariant) == false)
            {
                Logger.Instance?.WLog("DownloadPlugin.Invalid Plugin: " + name);
                return BadRequest("Invalid plugin: " + name);
            }

            var file = new FileInfo(Path.Combine(DirectoryHelper.PluginsDirectory, name));
            if (file.Exists == false)
                return NotFound(); // Plugin file not found

            var stream = FileOpenHelper.OpenRead_NoLocks(file.FullName);
            return File(stream, "application/octet-stream");
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed to download plugin '{name}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return BadRequest(ex.Message);
        }
    }

}