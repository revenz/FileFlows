using FileFlows.WebServer.Helpers;

namespace FileFlows.WebServer;

/// <summary>
/// Extension Methods
/// </summary>
public static class ExtensionMethods
{
    
    /// <summary>
    /// Gets the actual IP address of the request
    /// </summary>
    /// <param name="Request">the request</param>
    /// <returns>the actual IP Address</returns>
    public static string GetActualIP(this HttpRequest Request)
    {
        try
        {
            foreach (string header in new[] { "True-Client-IP", "CF-Connecting-IP", "HTTP_X_FORWARDED_FOR" })
            {
                if (Request.Headers.ContainsKey(header) && string.IsNullOrEmpty(Request.Headers[header]) == false)
                {
                    string? ip = Request.Headers[header].FirstOrDefault()
                        ?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
                    if (string.IsNullOrEmpty(ip) == false)
                        return ip;
                }
            }

            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    /// <returns>the profile or null if the user is not logged in</returns>
    public static async Task<Profile?> GetProfile(this HttpContext context)
    {
        var profile = new Profile();
        profile.Security = AuthenticationHelper.GetSecurityMode(); 
        if (profile.Security  == SecurityMode.Off)
        {
            profile.Role = UserRole.Admin;
        }
        else
        {
            var user = await context.GetLoggedInUser();
            if (user == null)
                return null;
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
        profile.Language = (settings.Language?.ToLowerInvariant() == "en" ? null : settings.Language?.EmptyAsNull()) ?? string.Empty; 
        profile.LanguageOptions = ServiceLoader.Load<LanguageService>().GetLanguageOptions();

        return profile;
    }
}