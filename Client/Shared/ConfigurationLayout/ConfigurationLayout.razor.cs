using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Shared;

public partial class ConfigurationLayout : LayoutComponentBase
{
    private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();

    public NavMenuItem Active { get; private set; }
    /// <summary>
    /// Gets or sets the navigation service
    /// </summary>
    [Inject] private INavigationService NavigationService { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] FrontendService feService { get; set; }

    /// <summary>
    /// Translations
    /// </summary>
    private string lblVersion;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblVersion = Translater.Instant("Labels.Version");
        var profile = feService.Profile.Profile;
        MenuItems = GetMenu(profile);
        try
        {
            string currentRoute = NavigationManager.Uri[NavigationManager.BaseUri.Length..];
            Active = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x?.Url == currentRoute);
            Active ??= MenuItems[0].Items.First();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static List<NavMenuGroup> GetMenu(Profile profile)
    {
        List<NavMenuGroup> menuItems = new();
        
        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Icon = "fas fa-code-branch",
            Items = new List<NavMenuItem>
            {
                profile.IsAdmin ? new ("Pages.Settings.Title", "fas fa-cogs", "config/settings") : null,
                profile.HasRole(UserRole.Tags) ? new("Pages.Tags.Title", "fas fa-tags", "config/tags") : null,
                profile.HasRole(UserRole.Variables)
                    ? new("Pages.Variables.Title", "fas fa-at", "config/variables")
                    : null,
                profile.HasRole(UserRole.Resources) && profile.LicensedFor(LicenseFlags.AutoUpdates)
                    ? new("Pages.Resources.Title", "fas fa-box-open", "config/resources")
                    : null,
            }.Where(x => x != null).ToList()
        });

        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Extensions"),
            Icon = "fas fa-laptop-house",
            Items = new NavMenuItem[]
            {
                profile.HasRole(UserRole.Plugins)
                    ? new("Pages.Plugins.Title", "fas fa-puzzle-piece", "config/plugins")
                    : null,
                profile.HasRole(UserRole.Scripts)
                    ? new("Pages.Scripts.Title", "fas fa-scroll", "config/scripts")
                    : null,
                profile.HasDockerInstances && profile.HasRole(UserRole.DockerMods)
                    ? new("Pages.DockerMod.Plural", "fab fa-docker", "config/dockermods")
                    : null,
            }.Where(x => x != null).ToList()
        });
        
        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.System"),
            Icon = "fas fa-desktop",
            Items = new NavMenuItem[]
            {
                profile.HasRole(UserRole.Revisions) && profile.LicensedFor(LicenseFlags.Revisions) ? new ("Pages.Revisions.Title", "fas fa-history", "config/revisions") : null,
                profile.HasRole(UserRole.Tasks) && profile.LicensedFor(LicenseFlags.Tasks) ? new ("Pages.Tasks.Title", "fas fa-clock", "config/tasks") : null,
                profile.HasRole(UserRole.Webhooks) && profile.LicensedFor(LicenseFlags.Webhooks) ? new ("Pages.Webhooks.Title", "fas fa-handshake", "config/webhooks") : null,
            }.Where(x => x != null).ToList()
        });
        
        if(profile.IsAdmin)
        {
            menuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Security"),
                Icon = "fas fa-user-shield",
                Items = new List<NavMenuItem>
                {
                    profile.LicensedFor(LicenseFlags.Auditing) && profile.UsersEnabled ? new ("Pages.Audit.Title", "fas fa-clipboard-list", "config/audit") : null,
                    profile.LicensedFor(LicenseFlags.AccessControl) ? new ("Pages.AccessControl.Title", "fas fa-shield-alt", "config/access-control") : null,
                    profile.LicensedFor(LicenseFlags.UserSecurity) ? new ("Pages.Users.Title", "fas fa-users", "config/users") : null
                }.Where(x => x != null).ToList()
            });
        }

        if (profile.LicensedFor(LicenseFlags.FileDrop))
        {
            menuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.FileDrop"),
                Icon = "fas fa-tint",
                Items = new List<NavMenuItem>
                {
                    new ("Settings", "fas fa-tint", "config/file-drop/settings"),
                    new ("Users", "fas fa-user-astronaut", "config/file-drop/users")
                }
            });
        }
        
        menuItems = menuItems.Where(x => x.Items.Count > 0).ToList();
        
        return menuItems;
    }

    async Task Click(NavMenuItem item)
    {
        bool ok = await NavigationService.NavigateTo(item.Url);
        if (ok)
        {
            SetActive(item);
        }
    }
    
    private void SetActive(NavMenuItem item)
    {
        Active = item;
        this.StateHasChanged();
    }
    
    /// <summary>
    /// Gets the link to the first available item
    /// </summary>
    /// <param name="profile">the users profile</param>
    /// <returns>the first available item</returns>
    public static string GetFirstAvailableItem(Profile profile)
        => GetMenu(profile).FirstOrDefault()?.Items?.FirstOrDefault()?.Url;
}

