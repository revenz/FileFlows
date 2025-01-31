using FileFlows.Server;
using FileFlows.WebServer.Authentication;
using FileFlows.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Bsae controller
/// </summary>
public abstract class BaseController : Controller
{
    /// <summary>
    /// Gets the audit details
    /// </summary>
    /// <returns>the audit details</returns>
    protected async Task<AuditDetails?> GetAuditDetails()
    {
        var ip = HttpContext.Request.GetActualIP();
        var user = await HttpContext.GetLoggedInUser();
        if (user == null)
            return null;
        return new AuditDetails()
        {
            IPAddress = ip,
            UserName = user.Name,
            UserUid = user.Uid
        };
    }

    /// <summary>
    /// Checks if the logged in user can access this role
    /// </summary>
    /// <param name="role">the role</param>
    /// <returns>true if they can access, otherwise false</returns>
    protected bool CanAccess(UserRole role)
    {
        if (HttpContext.Items.TryGetValue("USER_ROLE", out var oUserRole) == false)
            return false;
        if (oUserRole is UserRole userRole == false)
            return false;
        return (userRole & role) == role;
    }
}