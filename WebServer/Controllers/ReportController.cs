using System.Text.Json;
using FileFlows.Managers;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for the Reports
/// </summary>
[Route("/api/report")]
[FileFlowsAuthorize(UserRole.Reports)]
public class ReportController : BaseController
{
    /// <summary>
    /// Gets the report definitions
    /// </summary>
    /// <returns>the report definitions</returns>
    [HttpGet("definitions")]
    public IActionResult GetReportDefinitions()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        
        var results = new ReportManager().GetReports();
        return Ok(results);
    }

    /// <summary>
    /// Gets a report definition by its UID
    /// </summary>
    /// <param name="uid">the UID fo the report to get the definition for</param>
    /// <returns>the report definitions</returns>
    [HttpGet("definition/{uid}")]
    public IActionResult GetReportDefinition([FromRoute] Guid uid)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        
        var result = new ReportManager().GetReports().FirstOrDefault(x => x.Uid == uid);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
    
    /// <summary>
    /// Generates the report
    /// </summary>
    /// <param name="uid">the UID of the report</param>
    /// <param name="model">the report model</param>
    /// <returns>the reports HTML</returns>
    [HttpPost("generate/{uid}")]
    public async Task<IActionResult> Generate([FromRoute] Guid uid, [FromBody] Dictionary<string, object> model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");

        string? email = null;
        if (model.TryGetValue("Email", out var value) && value is JsonElement je )
            email = je.ValueKind == JsonValueKind.String ? je.GetString() : null;

        bool emailing = string.IsNullOrWhiteSpace(email) == false;

        var manager = new ReportManager();
        string? name = manager.GetReportName(uid);
        if (name == null)
            return BadRequest("Report not found.");

        if (emailing)
        {
            _ = Task.Run(async () =>
            {
                var result = await new ReportManager().Generate(uid, emailing, model);
                if (result.Failed(out var rError))
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record(NotificationSeverity.Warning, $"Report '{name}' failed to generate", rError);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.Value))
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record(NotificationSeverity.Warning, $"Report '{name}' had not matching data", rError);
                    return;
                }

                _ = ServiceLoader.Load<ScheduledReportService>().Email(name, [email], "FileFlows: " + name, result.Value);
            });
            
            // email reports we just exit early
            return Ok();
        }

        var result = await new ReportManager().Generate(uid, emailing, model);
        if (result.Failed(out var error))
            return BadRequest(error);
        if (string.IsNullOrWhiteSpace(result.Value))
            return BadRequest("Pages.Report.Messages.NoMatchingData");
        return Ok(result.Value);
    }

}