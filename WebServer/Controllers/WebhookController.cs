using FileFlows.Plugin;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for webhooks
/// </summary>
[Route("/api/webhook")]
[FileFlowsAuthorize(UserRole.Webhooks)]
public class WebhookController : BaseController
{
    /// <summary>
    /// Get all webhooks configured in the system
    /// </summary>
    /// <returns>A list of all configured webhooks</returns>
    [HttpGet]
    public async Task<IEnumerable<Webhook>> GetAll()
    {
        if(LicenseService.IsLicensed(LicenseFlags.Webhooks) == false)
            return new List<Webhook>();
        var all = await new ScriptController().GetAllByType(ScriptType.Webhook);
        var results = all.Select(x => FromScript(x))
            .Where(x => x != null)
            .Select(x => x!).ToList();
        return results;
    }

    /// <summary>
    /// Gets a webhook from a script
    /// </summary>
    /// <param name="script">the script</param>
    /// <returns>the webhook</returns>
    private Webhook? FromScript(Script script)
    {
        Webhook webhook = new()
        {
            Code = script.Code,
            Name = script.Name,
            Path = script.Path,
            Repository = script.Repository,
            Revision = script.Revision,
            Type = script.Type,
            Uid = script.Uid,
            LatestRevision = script.LatestRevision,
            UsedBy = script.UsedBy,
            Description = script.Description,
            Author = script.Author,
            MinimumVersion = script.MinimumVersion,
            Parameters = script.Parameters,
            Outputs = script.Outputs
        };

        if(webhook.Code.StartsWith("// path: "))
            webhook.Code = webhook.Code.Substring(webhook.Code.IndexOf('\n') + 1).Trim();

        // CommentBlock cblock = new(webhook.CommentBlock);
        // webhook.Code = webhook.Code.Replace(webhook.CommentBlock, string.Empty);
        //
        // webhook.Method = cblock.GetValue("method") == "POST" ? HttpMethod.Post : HttpMethod.Get;
        // webhook.Route = cblock.GetValue("route");
        if (string.IsNullOrWhiteSpace(webhook.Route))
        {
            Logger.Instance.WLog("Webhook: No route found for webhook: " + webhook.Name);
            return null;
        }

        return webhook;
    }

    /// <summary>
    /// Get webhook
    /// </summary>
    /// <param name="uid">The UID of the webhook</param>
    /// <returns>The webhook instance</returns>
    [HttpGet("{uid}")]
    public async Task<Webhook?> Get(Guid uid)
    {
        if(LicenseService.IsLicensed(LicenseFlags.Webhooks) == false)
            return null;
        var script = await new ScriptService().Get(uid);
        return script?.Type == ScriptType.Webhook ? FromScript(script) : null;
    }

    /// <summary>
    /// Saves a webhook
    /// </summary>
    /// <param name="webhook">The webhook to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<Webhook> Save([FromBody] Webhook webhook)
    {
        if(LicenseService.IsLicensed(LicenseFlags.Webhooks) == false || string.IsNullOrWhiteSpace(webhook.Name))
            return null;
        var existing = await Get(webhook.Uid);
        //CommentBlock comments;
        // if (existing != null)
        // {
        //     if (existing.HasChanged(webhook) == false)
        //         return existing;
        //     comments = new(existing.CommentBlock);
        // }
        // else
        // {
        //     comments = new(webhook.CommentBlock);
        // }
        // comments.AddOrUpdate("name", webhook.Name);
        // comments.AddOrUpdate("route", webhook.Route);
        // comments.AddOrUpdate("method", webhook.Method.ToString().ToUpperInvariant());
        
        // string code = 
        
        // new WebhookService().Update(webhook);
        return webhook;
    }

    /// <summary>
    /// Delete webhooks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        if(LicenseService.IsLicensed(LicenseFlags.Webhooks) == false)
            return;
        var service = ServiceLoader.Load<ScriptService>();
        var auditDetails = await GetAuditDetails();
        foreach(var uid in model.Uids)
            await service.Delete(uid, auditDetails);
    }
}