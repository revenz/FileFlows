namespace FileFlows.Client.Shared;

/// <summary>
/// Config Menu
/// </summary>
public class ConfigLayout : ConfigurationLayout
{
    /// <inheritdoc />
    protected override List<NavMenuGroup> GetMenu(Profile profile)
        => GetMenuItems(profile);
    
    
    /// <summary>
    /// Gets the link to the first available item
    /// </summary>
    /// <param name="profile">the users profile</param>
    /// <returns>the first available item</returns>
    public static string GetFirstAvailableItem(Profile profile)
        => GetMenuItems(profile).FirstOrDefault()?.Items?.FirstOrDefault()?.Url;
    
    /// <inheritdoc />
    public static List<NavMenuGroup> GetMenuItems(Profile profile)
    {
        List<NavMenuGroup> menuItems = new();
        
        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Extensions"),
            Icon = "fas fa-laptop-house",
            Items = new NavMenuItem[]
                {
                    profile.HasDockerInstances && profile.HasRole(UserRole.DockerMods)
                        ? new("Pages.DockerMod.Plural", "fab fa-docker", "config/dockermods")
                        : null,
                    profile.HasRole(UserRole.Plugins)
                        ? new("Pages.Plugins.Title", "fas fa-puzzle-piece", "config/plugins")
                        : null,
                    profile.HasRole(UserRole.Scripts)
                        ? new("Pages.Scripts.Title", "fas fa-scroll", "config/scripts")
                        : null,
                    profile.HasRole(UserRole.Variables)
                        ? new("Pages.Variables.Title", "fas fa-at", "config/variables")
                        : null,
                    profile.HasRole(UserRole.Tags) ? new("Pages.Tags.Title", "fas fa-tags", "config/tags") : null,
                }.Where(x => x != null)
                .OrderBy(x => x.Title.ToLowerInvariant()).ToList()
        });

        var nmgConfig = new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Icon = "fas fa-code-branch",
            Items = new List<NavMenuItem>
                {
                    profile.IsAdmin
                        ? new("Labels.License", "fas fa-money-check", "config/license")
                        : null,
                    profile.IsAdmin ? new("Pages.Settings.Labels.Logging", "fas fa-file-alt", "config/logging") : null,
                    profile.IsAdmin && profile.LicensedFor(LicenseFlags.AutoUpdates)
                        ? new("Pages.Settings.Labels.Updates", "fas fa-cloud", "config/updates")
                        : null,
                    profile.IsAdmin && profile.LicensedFor(LicenseFlags.FileServer)
                        ? new("Pages.Settings.Labels.FileServer", "fas fa-server", "config/file-server")
                        : null,
                    profile.IsAdmin && profile.LicensedFor(LicenseFlags.ExternalDatabase)
                        ? new("Pages.Settings.Labels.Database", "fas fa-database", "config/database")
                        : null,
                    profile.IsAdmin ? new("Pages.Settings.Labels.Email", "fas fa-envelope", "config/email") : null,
                }.Where(x => x != null)
                .OrderBy(x => x.Title.ToLowerInvariant()).ToList()
        };
        if (profile.IsAdmin)
            nmgConfig.Items.Insert(0,
                new(Translater.Instant("Pages.Settings.Labels.General"), "fas fa-cogs", "config/settings"));
        
        menuItems.Add(nmgConfig);

        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.System"),
            Icon = "fas fa-desktop",
            Items = new NavMenuItem[]
            {   
                profile.HasRole(UserRole.Resources) && profile.LicensedFor(LicenseFlags.AutoUpdates)
                    ? new("Pages.Resources.Title", "fas fa-box-open", "config/resources")
                    : null,
                profile.HasRole(UserRole.Revisions) && profile.LicensedFor(LicenseFlags.Revisions) ? new ("Pages.Revisions.Title", "fas fa-history", "config/revisions") : null,
                profile.HasRole(UserRole.Tasks) && profile.LicensedFor(LicenseFlags.Tasks) ? new ("Pages.Tasks.Title", "fas fa-clock", "config/tasks") : null,
              profile.HasRole(UserRole.Webhooks) && profile.LicensedFor(LicenseFlags.Webhooks) ? new ("Pages.Webhooks.Title", "fas fa-handshake", "config/webhooks") : null
            }.Where(x => x != null)
            .OrderBy(x => x.Title.ToLowerInvariant()).ToList()
        });
        
        if(profile.IsAdmin)
        {
            menuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Security"),
                Icon = "fas fa-user-shield",
                Items = new List<NavMenuItem>
                {
                    profile.LicensedFor(LicenseFlags.UserSecurity) && profile.IsAdmin ? new ("Pages.Settings.Fields.Security.Title", "fas fa-shield-alt", "config/security") : null,
                    profile.LicensedFor(LicenseFlags.Auditing) && profile.UsersEnabled ? new ("Pages.Audit.Title", "fas fa-clipboard-list", "config/audit") : null,
                    profile.LicensedFor(LicenseFlags.AccessControl) ? new ("Pages.AccessControl.Title", "fas fa-shield-alt", "config/access-control") : null,
                    profile.LicensedFor(LicenseFlags.UserSecurity) ? new ("Pages.Users.Title", "fas fa-users", "config/users") : null
                }.Where(x => x != null)
                .OrderBy(x => x.Title.ToLowerInvariant()).ToList()
            });
        }
        
        menuItems = menuItems.Where(x => x.Items.Count > 0).ToList();
        
        return menuItems;
    }
}