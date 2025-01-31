using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;
using FileFlows.WebServer.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.WebServer.Authentication;

/// <summary>
/// FileFlows API authentication attribute
/// </summary>
public class FileFlowsApiAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// If this needs a valid Node UID to be called
    /// </summary>
    private readonly bool Node;

    /// <summary>
    /// Initialises a new instance of the attribute
    /// </summary>
    /// <param name="node">if this needs a valid Node UID to be caleld</param>
    public FileFlowsApiAuthorizeAttribute(bool node = true)
    {
        Node = node;
    }
    
    
    /// <summary>
    /// Handles the on on authorization
    /// </summary>
    /// <param name="context">the context</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var mode = AuthenticationHelper.GetSecurityMode();
        if (mode == SecurityMode.Off)
            return;
        
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        
        var token = context.HttpContext.Request.Headers["x-token"].ToString();
        
        if(token != settings.AccessToken)
        {
            context.Result = new ContentResult
            {
                StatusCode = 401,
                Content = "Unauthorized: Invalid API token",
                ContentType = "text/plain"
            };
            return;
        }
    }
}