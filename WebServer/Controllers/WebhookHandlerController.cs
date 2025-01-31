using FileFlows.Server.Helpers;
using FileFlows.ServerShared;
using Microsoft.AspNetCore.Mvc;
using HttpMethod = FileFlows.Common.HttpMethod;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller processing webhooks
/// </summary>
[Route("/webhook")]
public class WebhookHandlerController:Controller
{
    /// <summary>
    /// Processes a GET webhook
    /// </summary>
    /// <param name="route">the name of the route</param>
    /// <returns>the result from the webhook</returns>
    [HttpGet("{route}")]
    public Task<IActionResult> Get([FromRoute] string route)
        => Process(HttpMethod.Get, route);
    
    /// <summary>
    /// Processes a POST webhook
    /// </summary>
    /// <param name="route">the name of the route</param>
    /// <returns>the result from the webhook</returns>
    [HttpPost("{route}")]
    public Task<IActionResult> Post([FromRoute] string route)
        => Process(HttpMethod.Post, route);
    
    /// <summary>
    /// Processes a PUT webhook
    /// </summary>
    /// <param name="route">the name of the route</param>
    /// <returns>the result from the webhook</returns>
    [HttpPut("{route}")]
    public Task<IActionResult> Put([FromRoute] string route)
        => Process(HttpMethod.Put, route);
    
    /// <summary>
    /// Processes a DELETE webhook
    /// </summary>
    /// <param name="route">the name of the route</param>
    /// <returns>the result from the webhook</returns>
    [HttpDelete("{route}")]
    public Task<IActionResult> Delete([FromRoute] string route)
        => Process(HttpMethod.Get, route);

    /// <summary>
    /// Processes a webhook
    /// </summary>
    /// <param name="method">the method of the webhook to process</param>
    /// <param name="route">the name of the route</param>
    /// <returns>the result from the webhook</returns>
    private async Task<IActionResult> Process(HttpMethod method, string route)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Webhooks) == false)
            return NotFound();

        var webhook = (await new WebhookController().GetAll()).FirstOrDefault(x => x.Route == route && x.Method == method);
        if (webhook == null)
            return NotFound();

        // need to get the code of the webhook
        webhook = await new WebhookController().Get(webhook.Uid);
        if (webhook == null)
            return NotFound();

        string code = webhook.Code;
        if (string.IsNullOrEmpty(code))
            return Ok();

        try
        {
            StreamReader reader = new StreamReader(Request.Body);
            string body = await reader.ReadToEndAsync();
            var result = ScriptExecutor.Execute(code, new()
            {
                { "Request", Request },
                { "Body", body },
                { "FileFlows.Url", Globals.ServerUrl }
            }, sharedDirectory: null, dontLogCode: true);
            if (result.Success)
                return Ok(result.Log);
            return BadRequest(result.Log);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}