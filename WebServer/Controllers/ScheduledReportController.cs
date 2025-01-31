namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for scheduled tasks
/// </summary>
[Route("/api/scheduled-report")]
[FileFlowsAuthorize(UserRole.Reports)]
public class ScheduledReportController : BaseController
{
    /// <summary>
    /// Get all scheduled reports configured in the system
    /// </summary>
    /// <returns>A list of all configured scheduled reports</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        var results = (await ServiceLoader.Load<ScheduledReportService>().GetAll()).OrderBy(x => x.Name.ToLowerInvariant());
        return Ok(results);
    }

    /// <summary>
    /// Get scheduled report
    /// </summary>
    /// <param name="uid">The UID of the scheduled report to get</param>
    /// <returns>The scheduled report instance</returns>
    [HttpGet("{uid}")]
    public async Task<IActionResult> Get(Guid uid)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        var report = await ServiceLoader.Load<ScheduledReportService>().GetByUid(uid);
        return report == null ? NotFound() : Ok(report);
    }

    /// <summary>
    /// Saves a scheduled report
    /// </summary>
    /// <param name="model">The scheduled reportto save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ScheduledReport model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        
        var result = await ServiceLoader.Load<ScheduledReportService>().Update(model, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Set state of a scheduled report
    /// </summary>
    /// <param name="uid">The UID of the scheduled report</param>
    /// <param name="enable">Whether this scheduled report is enabled</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
        
        var service = ServiceLoader.Load<ScheduledReportService>();
        var item = await service.GetByUid(uid);
        if (item == null)
            return BadRequest("Scheduled report not found.");
        if (enable != null && item.Enabled != enable.Value)
        {
            item.Enabled = enable.Value;
            var result = await service.Update(item, await GetAuditDetails());
            if (result.Failed(out string error))
                return BadRequest(error);
            item = result.Value;
        }
        return Ok(item);
    }

    /// <summary>
    /// Delete scheduled tasks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReferenceModel<Guid> model)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return BadRequest("Not licensed");
                
        await ServiceLoader.Load<ScheduledReportService>().Delete(model.Uids, await GetAuditDetails());
        return Ok();
    }
}
