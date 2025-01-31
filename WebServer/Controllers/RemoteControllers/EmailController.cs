using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers.RemoteControllers;

/// <summary>
/// Controller used to send emails
/// </summary>
[Route("/remote/email")]
[FileFlowsApiAuthorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class EmailController : Controller
{
    /// <summary>
    /// Sends an email using the email settings of the server 
    /// </summary>
    /// <param name="model">the email to send</param>
    /// <returns>the result of action, a 200 result for ok, else a failure</returns>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] EmailModel model)
    {
        var service = ServiceLoader.Load<IEmailService>();
        var result = await service.Send(model.To, model.Subject, model.Body);
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok();
    }
}

/// <summary>
/// The model used for sending emails
/// </summary>
public class EmailModel
{
    /// <summary>
    /// Gets who the email will be sent to
    /// </summary>
    public string[] To { get; init; } = [];
    /// <summary>
    /// Gets the subject of the email
    /// </summary>
    public string Subject { get; init; } = string.Empty;
    /// <summary>
    /// Gets the body of the email
    /// </summary>
    public string Body { get; init; } = string.Empty;
}