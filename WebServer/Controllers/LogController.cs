using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.WebServer.Middleware;
using FileFlows.Services;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;
using FileFlows.Shared.Helpers;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using NodeService = FileFlows.Services.NodeService;
using SettingsService = FileFlows.Services.SettingsService;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// System log controller
/// </summary>
[Route("/api/fileflows-log")] // FF-1060 renamed route to fileflows-log to avoid uBlock origin blacking /api/log
[FileFlowsAuthorize(UserRole.Log)]
public class LogController : Controller
{
    /// <summary>
    /// Gets the system log
    /// </summary>
    /// <returns>the system log</returns>
    [HttpGet]
    public async Task<string> Get([FromQuery] LogType logLevel = LogType.Info)
    {
        if (Logger.Instance.TryGetLogger(out FileLogger logger))
        {
            string log = await logger.GetTail(1000, logLevel);
            string html = LogToHtml.Convert(log);
            return FixLog(html);
        }
        return string.Empty;
    }
    
    private string FixLog(string log)
    => log.Replace("\\u0022", "\"")
            .Replace("\\u0027", "'");

    /// <summary>
    /// Get the available log sources
    /// </summary>
    /// <returns>the available log sources</returns>
    [HttpGet("log-sources")]
    public IDictionary<string, List<LogFile>> GetLogSources()
    {
        var dir = DirectoryHelper.LoggingDirectory;
        Dictionary<string, List<LogFile>> files = new();
        foreach (var file in new DirectoryInfo(dir).GetFiles("*.log").OrderByDescending(x => x.CreationTime))
        {
            var parts = file.Name[..^4].Split('-');
            if (int.TryParse(parts[^1], out int revision) == false)
                continue;
            DateTime date = file.CreationTime.Date;
            var source = string.Join('-', parts[..^2]);
            var lf = new LogFile()
            {
                Date = date,
                Revision = revision,
                FileName = file.Name,
                Source = source,
                ShortName = revision == 0 ?  $"{date:d MMM}" : $"{date:d MMM} [{revision:00}]"
            };

            if (source == "FileFlowsHTTP")
                source = "Web Requests";
            else if (source != "FileFlows" && source.StartsWith("Node") == false)
                source = source.Replace("FileFlows", string.Empty).Humanize(LetterCasing.Title);

            if (files.TryGetValue(source, out var list) == false)
            {
                list = new();
                files[source] = list;
                lf.Active = true; // new group, must be active
            }

            if (list.Count > 10)
                continue;

            if (lf.Active == false)
                lf.ShortName += $" ({FileSizeFormatter.Format(file.Length)})";

            list.Add(lf);
        }

        return files.OrderBy(x => x.Key == "FileFlows" ? 1 : 2)
            .ThenBy(x => x.Key.ToLowerInvariant())
            .ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Downloads the full system log
    /// </summary>
    /// <param name="source">the source to download from</param>
    /// <returns>a download result of the full system log</returns>
    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string source)
    {
        if (Regex.IsMatch(source, @"^[a-zA-Z0-9\-\.]+\.log$") == false || source.Contains(".."))
            return BadRequest("Invalid file: " + source);

        // Combine the base directory and the file name
        var baseDirectory = DirectoryHelper.LoggingDirectory;
        var filePath = Path.Combine(baseDirectory, source);

        // Ensure the file path is within the intended directory
        var fullPath = Path.GetFullPath(filePath);
        if (fullPath.StartsWith(Path.GetFullPath(baseDirectory), StringComparison.OrdinalIgnoreCase) == false)
            return BadRequest("Access denied: " + source);

        // Check if the file exists
        if (System.IO.File.Exists(fullPath) == false)
            return NotFound("Log file not found: " + fullPath);
        
        await Task.CompletedTask;
        
        try
        {
            // Create a FileStream to read the file
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Return a FileStreamResult to stream the file content
            return new FileStreamResult(stream, "text/plain")
            {
                FileDownloadName = source
            };
        }
        catch (Exception ex)
        {
            // Log the exception (if you have a logging mechanism)
            return StatusCode(500, $"Error reading log file: {ex.Message}");
        }
    }
}
