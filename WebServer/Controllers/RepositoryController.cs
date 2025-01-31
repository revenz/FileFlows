using FileFlows.ServerModels;
using FileFlows.ServerShared.Models;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for the Repository
/// </summary>
[Route("/api/repository")]
[FileFlowsAuthorize(UserRole.Flows | UserRole.Plugins | UserRole.Scripts | UserRole.DockerMods)]
public class RepositoryController : BaseController
{
    /// <summary>
    /// Gets the repository objects by types
    /// </summary>
    /// <param name="type">the type of objects to get</param>
    /// <param name="missing">only include missing objects not downloaded</param>
    /// <returns>a collection of objects</returns>
    [HttpGet("by-type/{type}")]
    public async Task<IEnumerable<RepositoryObject>> GetByType([FromRoute] string type, [FromQuery] bool missing = true)
    {
        var repo = await new RepositoryService().GetRepository();
        
        var objects = (type.ToLowerInvariant() switch
        {
            "dockermod" => repo.DockerMods,
            "script:system" => repo.SystemScripts,
            "script:flow" => repo.FlowScripts,
            "script:shared" => repo.SharedScripts,
            "script:webhook" => repo.WebhookScripts,
            _ => new List<RepositoryObject>()
        }).Where(x => x.MinimumVersion == null || new Version(Globals.Version) >= x.MinimumVersion)
        .OrderBy(x => x.Name.ToLowerInvariant())
        .ToList();
        
        if (missing)
        {
            var known = type.ToLowerInvariant() switch
            {
                "dockermod" => CanAccess(UserRole.DockerMods) ? (await ServiceLoader.Load<DockerModService>().GetAll()).Select(x => x.Name).ToList() : [],
                "script:system" => CanAccess(UserRole.Scripts) ? (await ServiceLoader.Load<ScriptService>().GetAllByType(ScriptType.System)).Where(x => x.Repository).Select(x => x.Name).ToList() : [],
                "script:shared" => CanAccess(UserRole.Scripts) ? (await ServiceLoader.Load<ScriptService>().GetAllByType(ScriptType.Shared)).Where(x => x.Repository).Select(x => x.Name).ToList() : [],
                "script:flow" => CanAccess(UserRole.Scripts) | CanAccess(UserRole.Flows) ? (await ServiceLoader.Load<ScriptService>().GetAllByType(ScriptType.Flow)).Where(x => x.Repository).Select(x => x.Name).ToList() : [],
                "script:webhook" => CanAccess(UserRole.Webhooks) ? (await ServiceLoader.Load<ScriptService>().GetAllByType(ScriptType.Webhook)).Where(x => x.Repository).Select(x => x.Name).ToList() : [],
                _ => []
            };

            if(known?.Any() == true)
                objects = objects.Where(x => x.Path != null && known.Contains(x.Path) == false && known.Contains(x.Name) == false).ToList();
        }
        return objects;
    }
    
    /// <summary>
    /// Download script into the FileFlows system
    /// </summary>
    /// <param name="type">the type of objects to download</param>
    /// <param name="objects">A list of objects to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download/{type}")]
    public async Task<IActionResult> DownloadByType([FromRoute] string type, [FromBody] List<RepositoryObject> objects)
    {
        if (objects?.Any() != true)
            return Ok(); // nothing to download

        Func<RepositoryObject, string, AuditDetails?, Task<Result<bool>>>? processor = null;

        switch (type.ToLowerInvariant())
        {
            case "dockermod":
            {
                if (CanAccess(UserRole.DockerMods) == false)
                    throw new UnauthorizedAccessException();

                var dmService = ServiceLoader.Load<DockerModService>();
                processor = dmService.ImportFromRepository;
            }
            break;
            case "script:flow":
            {
                if (CanAccess(UserRole.Scripts) == false && CanAccess(UserRole.Flows) == false)
                    throw new UnauthorizedAccessException();
                
                var repoService = new RepositoryService();
                await repoService.Init();
                await repoService.DownloadSharedScripts();
                
                var scriptService = ServiceLoader.Load<ScriptService>();
                processor = (ro, content, auditDetails) => scriptService.ImportFromRepository(ScriptType.Flow, ro, content, auditDetails);
            }
            break;
            case "script:system":
            {
                if (CanAccess(UserRole.Scripts) == false)
                    throw new UnauthorizedAccessException();
                
                var repoService = new RepositoryService();
                await repoService.Init();
                await repoService.DownloadSharedScripts();
                
                var scriptService = ServiceLoader.Load<ScriptService>();
                processor = (ro, content,auditDetails) => scriptService.ImportFromRepository(ScriptType.System, ro, content, auditDetails);
            }
            break;
            case "script:webhook":
            {
                if (CanAccess(UserRole.Webhooks) == false)
                    throw new UnauthorizedAccessException();
                
                var repoService = new RepositoryService();
                await repoService.Init();
                await repoService.DownloadSharedScripts();
                
                var scriptService = ServiceLoader.Load<ScriptService>();
                processor = (ro, content,auditDetails) => scriptService.ImportFromRepository(ScriptType.Webhook, ro, content, auditDetails);
            }
                break;
        }

        if (processor == null)
            return BadRequest("Invalid type");
        
        var service = ServiceLoader.Load<RepositoryService>();
        var auditDetails = await GetAuditDetails();
        foreach (var ro in objects)
        {
            if (ro.Path != null)
            {
                var result = await service.GetContent(ro.Path);
                if (result.Failed(out string error))
                    return BadRequest(error);
                var rr = await processor(ro, result.Value, auditDetails);
                if (rr.Failed(out error))
                    return BadRequest(error);
            }
        }

        return Ok();
    }
    
    /// <summary>
    /// Gets the fields for a repository object
    /// </summary>
    /// <param name="type">the type of objects to download</param>
    /// <param name="ro">A object to get the fields for</param>
    /// <returns>The fields</returns>
    [HttpPost("{type}/fields")]
    public async Task<IActionResult> GetFields([FromRoute] string type, [FromBody] RepositoryObject ro)
    {
        if (string.IsNullOrWhiteSpace(ro.Path))
            return BadRequest("No object passed in"); // nothing to download
        
        var service = ServiceLoader.Load<RepositoryService>();
        var result = await service.GetContent(ro.Path);
        if (result.Failed(out string error))
            return BadRequest(error);

        switch (type.ToLowerInvariant())
        {
            case "dockermod":
            {
                if (CanAccess(UserRole.DockerMods) == false)
                    throw new UnauthorizedAccessException();

                var dmService = ServiceLoader.Load<DockerModService>();
                var modResult = dmService.Parse(result.Value);
                if (modResult.Failed(out error))
                    return BadRequest(error);
                var form = new FormFieldsModel()
                {
                    Model = modResult.Value,
                    Fields = GetElementFields(modResult.Value.Code, "shell")
                };
                return Ok(form);
            }
            case "script:flow":
            case "script:system":
            case "script:webhook":
            {
                if (CanAccess(UserRole.Scripts) == false && CanAccess(UserRole.Flows) == false)
                    throw new UnauthorizedAccessException();

                var form = new FormFieldsModel()
                {
                    Model = new { Code = result.Value},
                    Fields = [
                        new()
                        {
                            Name = "Code",
                            InputType = FormInputType.Code
                        }
                    ]
                };
                return Ok(form);
            }
        }

        return BadRequest("Invalid Type");

        
        List<ElementField> GetElementFields(string? code = null, string? language = null)
        {
            var fields = new List<ElementField>
            {
                new()
                {
                    Name = nameof(ro.Name),
                    InputType = FormInputType.TextLabel
                },
                new()
                {
                    Name = nameof(ro.Author),
                    InputType = FormInputType.TextLabel
                },
                new()
                {
                    Name = nameof(ro.Revision),
                    InputType = FormInputType.TextLabel
                },
                new()
                {
                    Name = nameof(ro.Description),
                    InputType = FormInputType.TextLabel,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Pre", true }
                    }
                }
            };
            if (string.IsNullOrWhiteSpace(code) == false)
            {
                fields.Add(
                new()
                {
                    Name = "Code",
                    InputType = FormInputType.Code,
                    Parameters = new Dictionary<string, object>
                    {
                        { "Language", language! }
                    }
                });
            }

            return fields;
        }
    }


    /// <summary>
    /// Updates the repository objects
    /// </summary>
    /// <param name="type">the type of objects to update</param>
    /// <param name="model">The list of objects to update</param>
    /// <returns>The result</returns>
    [HttpPost("{type}/update")]
    public async Task<IActionResult> GetFields([FromRoute] string type, [FromBody] ReferenceModel<Guid> model)
    {
        if (model?.Uids?.Any() == false)
            return Ok(); // nothing to update

        var service = ServiceLoader.Load<RepositoryService>();
        var repo = await service.GetRepository();
        List<RepositoryObject>? toUpdate = null;
        Func<RepositoryObject, string, AuditDetails?, Task<Result<bool>>>? updater = null;
        switch (type.ToLowerInvariant())
        {
            case "dockermod":
                if (CanAccess(UserRole.DockerMods) == false)
                    return Unauthorized();
                var dmService = ServiceLoader.Load<DockerModService>();
                toUpdate = (await dmService.GetAll()).Select(x =>
                {
                    if (x.Repository == false) return null;
                    if (model?.Uids?.Contains(x.Uid) != true) 
                        return null;
                    var repoObject = repo.DockerMods.FirstOrDefault(y => y.Name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (repoObject == null || repoObject.Revision <= x.Revision)
                        return null;
                    return repoObject;
                }).Where(x => x != null).Select(x => x!).ToList();
                updater = dmService.ImportFromRepository;
                break;
        }

        if (toUpdate?.Any() != true || updater == null)
            return Ok();

        var auditDetails = await GetAuditDetails();
        foreach (var ro in toUpdate)
        {
            if (ro.Path != null)
            {
                var cResult = await service.GetContent(ro.Path);
                if (cResult.IsFailed)
                    continue;
                await updater(ro, cResult.Value, auditDetails);
            }
        }

        _ = ServiceLoader.Load<UpdateService>().Trigger();

        return Ok();
    }

    /// <summary>
    /// Gets the scripts
    /// </summary>
    /// <param name="type">the type of scripts to get</param>
    /// <param name="missing">only include scripts not downloaded</param>
    /// <returns>a collection of scripts</returns>
    [HttpGet("scripts")]
    public async Task<IEnumerable<RepositoryObject>> GetScripts([FromQuery] ScriptType type, [FromQuery] bool missing = true)
    {
        var repo = await new RepositoryService().GetRepository();
        var scripts = (
                type == ScriptType.System ? repo.SystemScripts : 
                type == ScriptType.Webhook ? repo.WebhookScripts : 
                repo.FlowScripts)
            .Where(x => new Version(Globals.Version) >= x.MinimumVersion);
        if (missing)
        {
            var service = ServiceLoader.Load<ScriptService>();
            var known = (await service.GetAllByType(type)).Where(x => x.Repository).Select(x => x.Path).ToList();

            scripts = scripts.Where(x => known.Contains(x.Path) == false && known.Contains(x.Name) == false).ToList();
        }
        return scripts;
    }
    
    /// <summary>
    /// Gets the sub flows
    /// </summary>
    /// <param name="missing">only include sub flows not downloaded</param>
    /// <returns>a collection of sub flows</returns>
    [HttpGet("subflows")]
    public async Task<IEnumerable<RepositoryObject>> GetSubFlows([FromQuery] bool missing = true)
    {
        var repo = await new RepositoryService().GetRepository();
        var subflows = repo.SubFlows
            .Where(x => new Version(Globals.Version) >= x.MinimumVersion);
        if (missing)
        {
            var service = ServiceLoader.Load<FlowService>();
            var known = (await service.GetAllAsync()).Where(x => x.Type == FlowType.SubFlow).Select(x => x.Uid).ToList();
            subflows = subflows.Where(x => x.Uid != null && known.Contains(x.Uid.Value) == false).ToList();
        }
        return subflows;
    }
    
    /// <summary>
    /// Increments the configuration revision
    /// </summary>
    /// <returns>an awaited task</returns>
    private Task RevisionIncrement()
        => ((SettingsService)ServiceLoader.Load<ISettingsService>()).RevisionIncrement();

    /// <summary>
    /// Gets the code of a script
    /// </summary>
    /// <param name="path">the script path</param>
    /// <returns>the script code</returns>
    [HttpGet("content")]
    public async Task<IActionResult> GetContent([FromQuery] string path)
    {
        var result = await new RepositoryService().GetContent(path);
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }
    
    /// <summary>
    /// Download script into the FileFlows system
    /// </summary>
    /// <param name="model">A list of script to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download")]
    public async Task Download([FromBody] RepositoryDownloadModel model)
    {
        if (model == null || model.Scripts?.Any() != true)
            return; // nothing to download

        // always re-download all the shared scripts to ensure they are up to date
        await DownloadActual(model.Scripts);
        // await RevisionIncrement();
    }

    /// <summary>
    /// Download sub flows from the repository
    /// </summary>
    /// <param name="model">A list of sub flows to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download-sub-flows")]
    public async Task DownloadSubFlows([FromBody] RepositoryDownloadModel model)
    {
        if (model == null || model.Scripts?.Any() != true)
            return; // nothing to download

        // always re-download all the shared scripts to ensure they are up to date
        var repoService = new RepositoryService();
        await repoService.Init();
        var service = ServiceLoader.Load<FlowService>();
        
        foreach (string path in model.Scripts)
        {
            var result = await repoService.GetContent(path);
            if (result.IsFailed)
            {
                Logger.Instance.WLog("Failed to retrieve sub flow content.");
                continue;
            }

            string json = result.Value;
            var flow = JsonSerializer.Deserialize<Flow>(json);
            if (flow == null)
            {
                Logger.Instance.WLog("Failed to deserialize sub flow.");
                continue;
            }

            if (await ServiceLoader.Load<FlowService>().UidInUse(flow.Uid))
            {
                Logger.Instance.WLog("Sub flow UID already in use.");
                continue;
            }

            flow.ReadOnly = true;
            await service.Update(flow, await GetAuditDetails());
        }
    }
    
    /// <summary>
    /// Perform the actual downloading of scripts
    /// </summary>
    /// <param name="scripts">the scripts to download</param>
    private async Task DownloadActual(List<string> scripts)
    {
        // always re-download all the shared scripts to ensure they are up to date
        var service = new RepositoryService();
        await service.Init();
        await service.DownloadSharedScripts(true);
        await service.DownloadScripts(scripts);
        
    }


    /// <summary>
    /// Update the scripts from th repository
    /// </summary>
    [HttpPost("update-scripts")]
    public async Task UpdateScripts()
    {
        var service = new RepositoryService();
        var original = (await GetScripts(ScriptType.Flow, missing: false))
            .Where(x => x.Path != null).ToDictionary(x => x.Path!, x => x.Revision);
        await service.Init();
        await service.Update();
        var updated = (await GetScripts(ScriptType.Flow, missing: false))
            .Where(x => x.Path != null).ToDictionary(x => x.Path!, x => x.Revision);
        bool changes = false;
        foreach (var key in original.Keys)
        {
            if (updated.TryGetValue(key, out var value) == false)
            {
                // shouldn't happen, but if it does
                changes = true;
                break;
            }

            if (value != original[key])
            {
                // revision changed, this means the config must update
                changes = true;
                break;
            }
        }
        if(changes)
        {
            // scripts were update, increment the revision
            await RevisionIncrement();
            _ = ServiceLoader.Load<UpdateService>().Trigger();
        }
    }

    /// <summary>
    /// Download the latest revisions for the specified scripts
    /// </summary>
    /// <param name="model">The list of scripts to update</param>
    /// <returns>if the updates were successful or not</returns>
    [HttpPost("update-specific-scripts")]
    public async Task<bool> UpdateSpecificScripts([FromBody] ReferenceModel<string> model)
    {
        var service = new RepositoryService();
        await service.Init();
        var repo = await service.GetRepository();
        var objects = repo.FlowScripts.Union(repo.SystemScripts).Union(repo.SharedScripts).Where(x => x.MinimumVersion <= new Version(Globals.Version))
            .Where(x =>
            {
                if(model.Uids.Contains(x.Path))
                    return true;
                if (model.Uids.Contains(x.Uid.ToString()))
                    return true;
                return false;
            }).ToList();
        if (objects.Any() == false)
            return false; // nothing to update
        await DownloadActual(objects.Where(x => x.Path != null).Select(x => x.Path!).ToList());
        await ServiceLoader.Load<UpdateService>().Trigger();
        // we always do an update here, its a user forcing an update
        await RevisionIncrement();
        return true;
    }
    
    /// <summary>
    /// Download model
    /// </summary>
    public class RepositoryDownloadModel
    {
        /// <summary>
        /// A list of plugin packages to download
        /// </summary>
        public List<string> Scripts { get; init; } = [];
    }
}