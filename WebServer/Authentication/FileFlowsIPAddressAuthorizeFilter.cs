using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.WebServer.Authentication;

/// <summary>
/// FileFlows authentication filter that checks the IP Address is allowed
/// </summary>
public class FileFlowsIPAddressAuthorizeFilter
{
    /// <summary>
    /// Next request delegate
    /// </summary>
    private readonly RequestDelegate _next;
    
    /// <summary>
    /// Constructs a instance of the middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public FileFlowsIPAddressAuthorizeFilter(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        if (WebServerApp.FullyStarted == false || IsAuthorizedPath(context.Request.Path) == false)
        {
            await _next(context);
            return;
        }

        if (LicenseService.IsLicensed(LicenseFlags.AccessControl) == false)
        {
            await _next(context);
            return;
        }

        bool remote = context.Request.Path.StartsWithSegments("/remote");

        bool ipAllowed = await ServiceLoader.Load<AccessControlService>()
            .CanAccess(remote ? AccessControlType.RemoteServices : AccessControlType.Console,
                context.Request.GetActualIP());
        
        if(ipAllowed)
        {
            await _next(context);
            return;
        }
        
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized: IP Address not allowed access");
    }
    
    /// <summary>
    /// Helper method to check if the request path matches any authorized paths.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns>True if the request path matches any authorized paths; otherwise, false.</returns>
    private bool IsAuthorizedPath(PathString path)
    {
        return path.StartsWithSegments("/api") ||
               path.StartsWithSegments("/authorize") ||
               path.StartsWithSegments("/remote");
    }
}