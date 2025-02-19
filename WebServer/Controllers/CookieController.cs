using FileFlows.WebServer.Helpers;
using Swashbuckle.AspNetCore.Annotations;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller that uses cookie authentication
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class CookieController : Controller
{
    /// <summary>
    /// Gets a thumbnail 
    /// </summary>
    /// <param name="uid">the UID of the file</param>
    /// <param name="extension">the extension of the file</param>
    /// <param name="pad">the padding</param>
    /// <param name="folder">if this is a folder</param>
    /// <returns>the thumbnail</returns>
    [HttpGet("/api/thumbnail/{uid}")]
    [SwaggerIgnore]
    public async Task<IActionResult> FileThumbnail([FromRoute] Guid uid, [FromQuery] string extension,
        [FromQuery] int pad = 20, [FromQuery] bool folder = false)
    {
        if (AuthenticationHelper.GetSecurityMode() != SecurityMode.Off)
        {
            var token = HttpContext.Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            var user = await HttpContextExtensions.GetLoggedInUser(token, Request);
            if (user == null)
                return Unauthorized();
        }

        var path = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid + ".webp");
        if (System.IO.File.Exists(path) == false)
            return Redirect($"/icon/filetype/{pad}/{(folder ? "folder" : extension)}.svg");
            
        return PhysicalFile(path, "application/octet-stream");
    }
}