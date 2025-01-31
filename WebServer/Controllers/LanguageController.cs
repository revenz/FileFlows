using FileFlows.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Language controller
/// </summary>
[Route("/api/language")]
public class LanguageController : Controller
{
    /// <summary>
    /// Gets the language file
    /// </summary>
    /// <param name="language">Optional language to load</param>
    /// <returns>the language JSON</returns>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery]string? language = null)
    {
        var json = await ServiceLoader.Load<LanguageService>().GetLanguageJson(language);
        return Content(json, "application/json");
    }
}