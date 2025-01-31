using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using FileFlows.WebServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.WebServer.Authentication;

/// <summary>
/// FileFlows authentication attribute
/// </summary>
public class FileFlowsAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// Gets or sets the role
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Constructs a new instance of the FileFlows authorize filter
    /// </summary>
    /// <param name="role">the role</param>
    public FileFlowsAuthorizeAttribute(UserRole role = (UserRole)0)
    {
        Role = role;
    }
    
    /// <summary>
    /// Handles the on on authorization
    /// </summary>
    /// <param name="context">the context</param>
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (AuthenticationHelper.GetSecurityMode() == SecurityMode.Off)
        {
            context.HttpContext.Items["USER_ROLE"] = UserRole.Admin;
            return Task.CompletedTask;
        }

        UserRole roleToTest = Role;
        
        // Check if the action method has the AllowAnonymous attribute
        if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            var allowAnonymousAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .FirstOrDefault();

            if (allowAnonymousAttribute != null)
            {
                // The action method has the AllowAnonymous attribute applied
                // Skip the authorization check
                return Task.CompletedTask;
            }
            var authorizeAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                .OfType<FileFlowsAuthorizeAttribute>()
                .FirstOrDefault();
            if (authorizeAttribute != null)
                roleToTest = authorizeAttribute.Role; // method level authorization in place for this
        }
        
        var user = context.HttpContext.GetLoggedInUser().Result;
        if(user == null)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }
        context.HttpContext.Items["USER_ROLE"] = UserRole.Admin;

        if ((int)roleToTest == 0)
            return Task.CompletedTask; // any role
        
        if(user.Role == UserRole.Admin)
            return Task.CompletedTask; // admins allow all access
        
        if(roleToTest == UserRole.Admin)
            context.Result = new UnauthorizedResult(); // theyre not admin, otherwise the previous check would have returned
        
        if ((user.Role & roleToTest) == 0) // they require any of the enums
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}