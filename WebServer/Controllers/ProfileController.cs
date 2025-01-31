using FileFlows.Plugin;
using FileFlows.WebServer.Authentication;
using FileFlows.Server.Helpers;
using FileFlows.ServerModels;
using FileFlows.Services;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.WebServer.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Profile Controller
/// </summary>
[Route("/api/profile")]
[FileFlowsAuthorize]
public class ProfileController : Controller
{
    /// <summary>
    /// Gets the profile
    /// </summary>
    /// <returns>the profile</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profile = new Profile();
        profile.Security = AuthenticationHelper.GetSecurityMode(); 
        if (profile.Security  == SecurityMode.Off)
        {
            profile.Role = UserRole.Admin;
        }
        else
        {
            var user = await HttpContext.GetLoggedInUser();
            if (user == null)
                return Unauthorized();
            profile.Role = user.Role;
            profile.Name = user.Name;
            profile.Uid = user.Uid;
            profile.Email = user.Email;
        }

        profile.ServerOS = PlatformHelper.GetOperatingSystemType();

        bool libs = (await ServiceLoader.Load<LibraryService>().GetAllAsync()).Any(x =>
            x.Uid != CommonVariables.ManualLibraryUid);
        bool flows = await ServiceLoader.Load<FlowService>().HasAny();
        bool users = await ServiceLoader.Load<UserService>().HasAny();

        var settings = await ServiceLoader.Load<ISettingsService>().Get();

        if (settings.InitialConfigDone)
            profile.ConfigurationStatus |= ConfigurationStatus.InitialConfig;
        if (settings.EulaAccepted)
            profile.ConfigurationStatus |= ConfigurationStatus.EulaAccepted;
        if (flows)
            profile.ConfigurationStatus |= ConfigurationStatus.Flows;
        if (libs)
            profile.ConfigurationStatus |= ConfigurationStatus.Libraries;
        if (users)
            profile.ConfigurationStatus |= ConfigurationStatus.Users;
        profile.IsWebView = Globals.UsingWebView;
        var licenseService = ServiceLoader.Load<LicenseService>();
        var license = licenseService.GetLicense();
        if (license?.Status == LicenseStatus.Valid)
        {
            profile.License = license.Flags;
            profile.LicenseLevel = license.Level;
            profile.UsersEnabled = (license.Flags & LicenseFlags.UserSecurity) == LicenseFlags.UserSecurity &&
                                   ServiceLoader.Load<AppSettingsService>().Settings.Security != SecurityMode.Off && 
                                   await ServiceLoader.Load<UserService>().HasAny();
        }

        if (profile.IsAdmin)
            profile.UnreadNotifications = await ((NotificationService)ServiceLoader.Load<INotificationService>()).GetUnreadNotificationsCount();

        if (Globals.IsDocker)
            profile.HasDockerInstances = true;
        else
        {
            // check the nodes
            var nodes = await ServiceLoader.Load<NodeService>().GetAllAsync();
            profile.HasDockerInstances = nodes.Any(x => x.OperatingSystem == OperatingSystemType.Docker);
        }
        #if(DEBUG)
        profile.HasDockerInstances = true;
        #endif
        profile.Language = settings.Language?.ToLowerInvariant() == "en" ? null : settings.Language?.EmptyAsNull(); 
        profile.LanguageOptions = GetLanguageOptions();

        return Ok(profile);
    }
    
    /// <summary>
    /// Get the language options
    /// </summary>
    /// <returns>the language options</returns>
    private List<ListOption> GetLanguageOptions()
    {
        var options = new List<ListOption>();
        var langPath = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot", "i18n");
#if(DEBUG)
        var dllDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        dllDir = dllDir[..(dllDir.IndexOf("Server", StringComparison.Ordinal))];
        langPath = Path.Combine(dllDir, "Client", "wwwroot", "i18n");
#endif        
        if (Directory.Exists(langPath) == false)
            return options;
        foreach (var file in new DirectoryInfo(langPath).GetFiles("*.json"))
        {
            if(file.Name.Contains("Plugins", StringComparison.InvariantCultureIgnoreCase))
                continue;
            var parts = file.Name.Split('.');
            if (parts.Length != 2)
                continue;
            var langName = LanguageHelper.GetNativeName(parts[0]);
            options.Add(new ListOption
            {
                Value = parts[0],
                Label = langName
            });
        }

        return options.OrderBy(x => x.Label == "English" ? 0 : 1).ThenBy(x => x.Label.ToLowerInvariant()).ToList();
    }
}