using FileFlows.Managers.InitializationManagers;
using FileFlows.Server;
using FileFlows.Services;

namespace FileFlows.WebServer.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Home controller
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Main application index page
    /// </summary>
    /// <returns>the index page</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(NoStore = true, Duration = 0)]
    public IActionResult Index()
    {
        return File("~/index.html", "text/html");
    }

    /// <summary>
    /// Endpoints that just responds that the server is listening
    /// </summary>
    /// <returns>the response</returns>
    [HttpGet("frasier")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(NoStore = true, Duration = 0)]
    public IActionResult Frasier()
        => Ok("I'm listening");

    /// <summary>
    /// Loading page
    /// </summary>
    /// <returns></returns>
    [HttpGet("loading")]
    public IActionResult Loading()
    {
        if(WebServerApp.FullyStarted)
            return Redirect("/");
        return View();
    }

    /// <summary>
    /// Database is offline error message
    /// </summary>
    /// <returns>the view</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("database-offline")]
    public IActionResult DatabaseOffline()
    {
        var appSettingsService = ServiceLoader.Load<AppSettingsService>();
        var result = MigrationManager.CanConnect(appSettingsService.Settings.DatabaseType, appSettingsService.Settings.DatabaseConnection);
        if (result is { IsFailed: false, Value: true })
        {
            // no longer disconnected
            return Redirect("/");
        }
        ViewBag.Title = "Database Offline";
        ViewBag.Message = "Database is offline.\nPlease check the database is running and FileFlows has access to it.";
        return View("Error");
    }
}
