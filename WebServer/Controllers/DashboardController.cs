using FileFlows.Plugin;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Widgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for the dashboard
/// </summary>
[Route("/api/dashboard")]
[FileFlowsAuthorize]
public class DashboardController : BaseController
{
    /// <summary>
    /// Gets the basic info for loading of the dashboard
    /// </summary>
    /// <returns>the info</returns>
    [HttpGet("info")]
    public SystemInfo GetInfo()
        => ServiceLoader.Load<DashboardService>().GetSystemInfo();
    
    /// <summary>
    /// Gets the file overview
    /// </summary>
    /// <returns></returns>
    [HttpGet("file-overview")]
    public FileOverviewData GetFileOverview()
        => ServiceLoader.Load<DashboardFileOverviewService>().GetData();

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the processing node</returns>
    [HttpGet("node-summary")]
    public async Task<List<NodeStatusSummary>> GetNodeSummary()
        => await ServiceLoader.Load<NodeService>().GetStatusSummaries();

    /// <summary>
    /// Gets the executors info minified
    /// </summary>
    /// <returns>the executors info minified</returns>
    [HttpGet("executors-info-minified")]
    public async Task<List<FlowExecutorInfoMinified>> GetExecutorsInfoMinified()
        => (await ServiceLoader.Load<FlowRunnerService>().GetExecutors()).Values.ToList();
    
    /// <summary>
    /// Gets any updates
    /// </summary>
    /// <returns>the updates</returns>
    [HttpGet("updates")]
    public UpdateInfo GetUpates()
        => ServiceLoader.Load<UpdateService>().Info;

    /// <summary>
    /// Gets a node icon
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <returns>the icon</returns>
    [HttpGet("node/{uid}/icon")]
    [AllowAnonymous]
    public async Task<IActionResult> NodeIcon(Guid uid)
    {
        var node = await ServiceLoader.Load<NodeService>().GetByUidAsync(uid);
        if (string.IsNullOrWhiteSpace(node.Icon) || node.Icon.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) == false)
            return NotFound();
        try
        {
            // Remove the "data:" prefix and decode the base64 data
            var base64Data = node.Icon.Substring(node.Icon.IndexOf(',', StringComparison.Ordinal) + 1);
            var imageData = Convert.FromBase64String(base64Data);

            // Determine the MIME type from the data URL (e.g., "data:image/png;base64,")
            var mimeType = node.Icon[5..node.Icon.IndexOf(';', StringComparison.Ordinal)];
        
            // Return the image data as a file with the appropriate MIME type
            return File(imageData, mimeType);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 image data.");
        }
    }
    
    /// <summary>
    /// Get all dashboards in the system
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet]
    public async Task<List<Dashboard>> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.Dashboards) == false)
             return new List<Dashboard>();

        var service = ServiceLoader.Load<DashboardService>();
        var dashboards = (await service.GetAll())
            .OrderBy(x => x.Name.ToLower()).ToList();
        if (dashboards.Any() == false)
        {
            // add default
        }
        return dashboards;
    }

    /// <summary>
    /// Get a list of all dashboards
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet("list")]
    public async Task<List<ListOption>> ListAll()
    {
        var service = ServiceLoader.Load<DashboardService>();
        var dashboards = LicenseService.IsLicensed(LicenseFlags.Dashboards) == false
            ? new List<ListOption>()
            : (await service.GetAll())
                .OrderBy(x => x.Name.ToLower()).Select(x => new ListOption
                {
                    Label = x.Name,
                    Value = x.Uid
                }).ToList();
        
        // add default
        dashboards.Insert(0, new ()
        {
            Label = Dashboard.DefaultDashboardName,
            Value = Dashboard.DefaultDashboardUid
        });
        
        return dashboards;
    }

    /// <summary>
    /// Get a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <returns>The dashboard instance</returns>
    [HttpGet("{uid}/Widgets")]
    public async Task<List<WidgetUiModel>> Get(Guid uid)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Dashboards) == false)
            uid = Dashboard.DefaultDashboardUid;
        var service = ServiceLoader.Load<DashboardService>();
        var db = await service.GetByUid(uid);
        if ((db == null || db.Uid == Guid.Empty) && uid == Dashboard.DefaultDashboardUid)
        {
            var nodes = (await ServiceLoader.Load<NodeService>().GetAllAsync()).Count(x => x.Enabled);
            var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;
            db = Dashboard.GetDefaultDashboard(appSettings.DatabaseType == DatabaseType.Sqlite, nodes);
        }
        else if (db == null)
            throw new Exception("Dashboard not found");
        List<WidgetUiModel> Widgets = new List<WidgetUiModel>();
        foreach (var p in db.Widgets)
        {
            try
            {
                var pd = WidgetDefinition.GetDefinition(p.WidgetDefinitionUid);
                WidgetUiModel pui = new()
                {
                    X = p.X,
                    Y = p.Y,
                    Height = p.Height,
                    Width = p.Width,
                    Uid = p.WidgetDefinitionUid,

                    Flags = pd.Flags,
                    Name = pd.Name,
                    Type = pd.Type,
                    Url = pd.Url,
                    Icon = pd.Icon
                };
                #if(DEBUG)
                pui.Url = "http://localhost:6868" + pui.Url;
                #endif
                Widgets.Add(pui);
            }
            catch (Exception)
            {
                // can throw if Widget definition is not found
                Logger.Instance.WLog("Widget definition not found: " + p.WidgetDefinitionUid);
            }
        }

        return Widgets;
    }

    /// <summary>
    /// Delete a dashboard from the system
    /// </summary>
    /// <param name="uid">The UID of the dashboard to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public async Task Delete([FromRoute] Guid uid)
        => await ServiceLoader.Load<DashboardService>().Delete(new [] { uid }, await GetAuditDetails());

    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="model">The dashboard being saved</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] Dashboard model)
    {
        var result = await ServiceLoader.Load<DashboardService>().Update(model, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <param name="widgets">The Widgets to save</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut("{uid}")]
    public async Task<IActionResult> Save([FromRoute] Guid uid, [FromBody] List<Widget> widgets)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Dashboards) == false)
            return null;
        var service = ServiceLoader.Load<DashboardService>();
        var dashboard = await service.GetByUid(uid);
        if (dashboard == null)
            throw new Exception("Dashboard not found");
        dashboard.Widgets = widgets ?? new List<Widget>();
        var result = await service.Update(dashboard, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }
}