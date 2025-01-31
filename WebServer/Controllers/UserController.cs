using System.Text.RegularExpressions;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;
using FileFlows.WebServer.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// User Controller
/// </summary>
[Route("/api/user")]
[FileFlowsAuthorize(UserRole.Admin)]
public class UserController : BaseController
{
    /// <summary>
    /// Dummy password
    /// </summary>
    private const string DUMMY_PASSWORD = "**************";
    
    /// <summary>
    /// Get all users in the system
    /// </summary>
    /// <returns>A list of users</returns>
    [HttpGet]
    public async Task<List<User>> GetAll()
    {
        if (LicenseService.IsLicensed(LicenseFlags.UserSecurity) == false)
            return new List<User>();

        var service = ServiceLoader.Load<UserService>();
        var users = await service.GetAll();

        var uiList = users.Select(x => new User()
        {
            Uid = x.Uid,
            Name = x.Name,
            Email = x.Email,
            Role = x.Role,
            Password = string.IsNullOrWhiteSpace(x.Password) ? string.Empty : DUMMY_PASSWORD,
            LastLoggedIn = x.LastLoggedIn,
            LastLoggedInAddress = x.LastLoggedInAddress,
            DateCreated = x.DateCreated,
            DateModified = x.DateModified,
        }).ToList();
        return uiList;
    }
    
    /// <summary>
    /// Saves a user
    /// </summary>
    /// <param name="user">The user to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] User user)
    {
        if (user == null)
            return BadRequest("User model required");
        
        var service = ServiceLoader.Load<UserService>();
        if (user.Password == DUMMY_PASSWORD && user.Uid != Guid.Empty)
        {
            var existing = await service.GetByUid(user.Uid);
            if (existing == null)
                return BadRequest("Failed to locate existing user");
            user.Password = existing.Password;
        }
        else
        {
            user.Password = user.Password?.EmptyAsNull() ?? AuthenticationHelper.GenerateRandomPassword();
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        }
        var result = await service.Update(user, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete users
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReferenceModel<Guid> model)
    {
        var user = await HttpContext.GetLoggedInUser();
        if(user != null && model.Uids.Contains(user.Uid))
            return BadRequest("Pages.Users.Messages.CannotDeleteYourself");
           
        await ServiceLoader.Load<UserService>().Delete(model.Uids, await GetAuditDetails());
        return Ok();
    }
}