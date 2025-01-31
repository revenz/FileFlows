using Microsoft.JSInterop;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace FileFlows.Client.Shared;

/// <summary>
/// Navigation Menu
/// </summary>
public partial class NavMenu : IDisposable
{
    /// <summary>
    /// Gets or sets the navigation service
    /// </summary>
    [Inject] private INavigationService NavigationService { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets teh client service
    /// </summary>
    [Inject] private ClientService ClientService { get; set; }
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jSRuntime { get; set; }
    
    
    private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();
    /// <summary>
    /// This is if the menu is hidden, only used on mobile to hide the menu completely
    /// Different from collapsing which is shown on non-mobile when the menu is just the icons
    /// </summary>
    private bool hideNavMenu = true;
    
    /// <summary>
    /// Gets or sets the change password dialog
    /// </summary>
    private ChangePassword ChangePassword { get; set; }

    public NavMenuItem Active { get; private set; }

    private string lblVersion, lblHelp, lblReddit, lblDiscord, lblChangePassword, lblLogout;

    private NavMenuItem nmiFlows, nmiLibraries, nmiPause;
    /// <summary>
    /// If the user menu is opened or closed
    /// </summary>
    private bool UserMenuOpened = false;

    private int Unprocessed = -1, Processing = -1, Failed = -1, OnHold = -1;
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }

    private List<NavMenuItem> UserMenu = new();
    /// <summary>
    /// Gets or sets the users profile
    /// </summary>
    private Profile Profile;
    /// <summary>
    /// If the bubbles have been loaded at least once
    /// </summary>
    private bool bubblesLoadedOnce = false;

    private bool InitDone = false;
    private IJSObjectReference jsNavMenu;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblVersion = Translater.Instant("Labels.Version");
        lblHelp = Translater.Instant("Labels.Help");
        lblReddit = "Reddit";
        lblDiscord = Translater.Instant("Labels.Discord");
        lblChangePassword = Translater.Instant("Labels.ChangePassword");
        lblLogout = Translater.Instant("Labels.Logout");
        
        NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
        
        _ = RefreshBubbles();
        
        ClientService.FileStatusUpdated += ClientServiceOnFileStatusUpdated;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;

        ProfileService.OnRefresh += ProfileServiceOnOnRefresh; 
        Profile = await ProfileService.Get();

        if ((Profile.ConfigurationStatus & ConfigurationStatus.InitialConfig) != ConfigurationStatus.InitialConfig || 
            (Profile.ConfigurationStatus & ConfigurationStatus.EulaAccepted) != ConfigurationStatus.EulaAccepted)
        {
            if (Profile.IsAdmin == false)
            {
                await ProfileService.Logout("Labels.AdminRequired");
                return;
            }
            NavigationManager.NavigateTo("/initial-config");
            return;
        }
        
        
        var jsObjectReference = await jSRuntime.InvokeAsync<IJSObjectReference>("import", $"./Shared/NavMenu/NavMenu.razor.js?v={Globals.Version}");
        jsNavMenu = await jsObjectReference.InvokeAsync<IJSObjectReference>("createNavMenu");
        this.LoadMenu();
    }

    private void ProfileServiceOnOnRefresh()
    {
        this.LoadMenu();
        StateHasChanged();
    }

    private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (MenuItems?.Any() != true)
            return;
        
        var lastRoute = e?.Location?.Contains("/flows/") == true ? "flows" :
            e?.Location?.Contains("/report/") == true ? "reporting" : 
            e?.Location?.Split('/')?.LastOrDefault();
        if (string.IsNullOrWhiteSpace(lastRoute))
            return;
        
        var item = MenuItems.Where(x => x.Items != null)
            .SelectMany(x => x.Items)
            .FirstOrDefault(x => x?.Url == lastRoute);
        if (item == null)
            return;

        Active = item;
        StateHasChanged();
    }

    private void PausedServiceOnOnPausedLabelChanged(string label)
    {
        if (nmiPause == null || nmiPause.Title == label)
            return;
        
        nmiPause.Title = label;
        StateHasChanged();
    }

    private void ClientServiceOnFileStatusUpdated(List<LibraryStatus> data)
    {
        Unprocessed = data.Where(x => x.Status == FileStatus.Unprocessed).Select(x => x.Count).FirstOrDefault();
        Processing = data.Where(x => x.Status == FileStatus.Processing).Select(x => x.Count).FirstOrDefault();
        Failed = data.Where(x => x.Status == FileStatus.ProcessingFailed).Select(x => x.Count).FirstOrDefault();
        OnHold = data.Where(x => x.Status == FileStatus.OnHold).Select(x => x.Count).FirstOrDefault();
        Logger.Instance.ILog($"NavMenu Updated: ({Unprocessed}) ({Processing}) ({Failed}) ({OnHold})");
        this.StateHasChanged();
    }

    private async Task RefreshBubbles()
    {
        if (bubblesLoadedOnce) // if loaded once, check document has focus
        {
            bool hasFocus = await jSRuntime.InvokeAsync<bool>("eval", "document.hasFocus()");
            if (hasFocus == false)
                return;
        }
        else
        {
            // else we want to load it once to just show the data
            bubblesLoadedOnce = true;
        }

        var sResult = await HttpHelper.Get<List<LibraryStatus>>("/api/library-file/status");
        if (sResult.Success == false || sResult.Data?.Any() != true)
            return;
        Unprocessed = sResult.Data.Where(x => x.Status == FileStatus.Unprocessed).Select(x => x.Count).FirstOrDefault();
        Processing = sResult.Data.Where(x => x.Status == FileStatus.Processing).Select(x => x.Count).FirstOrDefault();
        Failed = sResult.Data.Where(x => x.Status == FileStatus.ProcessingFailed).Select(x => x.Count).FirstOrDefault();
        OnHold = sResult.Data.Where(x => x.Status == FileStatus.OnHold).Select(x => x.Count).FirstOrDefault();
        this.StateHasChanged();
    }
    
    void LoadMenu()
    {
        this.InitDone = false;
        this.MenuItems.Clear();
        nmiPause = Profile.HasRole(UserRole.PauseProcessing) ? new(PausedService.PausedLabel, "far fa-pause-circle", "#pause") : null;


        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Overview"),
            Icon = "fas fa-info-circle",
            Items = new List<NavMenuItem>
            {
                new ("Pages.Dashboard.Title", "fas fa-chart-pie", ""),
                // newDashboard ? new ("Old Dashboard", "fas fa-chart-pie", "old-dashboard") : null,
                Profile.HasRole(UserRole.Files) ? new ("Pages.LibraryFiles.Title", "fas fa-copy", "library-files") : null,
                nmiPause
            }.Where(x => x != null).ToList()
        });

        nmiFlows = Profile.HasRole(UserRole.Flows) ? new("Pages.Flows.Title", "fas fa-sitemap", "flows") : null;
        nmiLibraries = Profile.HasRole(UserRole.Libraries) ? new("Pages.Libraries.Title", "fas fa-folder", "libraries") : null;

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Icon = "fas fa-code-branch",
            Items = new List<NavMenuItem>
            {
                nmiFlows,
                nmiLibraries,
                Profile.HasRole(UserRole.Nodes) ? new ("Pages.Nodes.Title", "fas fa-desktop", "nodes") : null,
                Profile.HasRole(UserRole.Tags) ? new("Pages.Tags.Title", "fas fa-tags", "tags") : null,
            }
        });

        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Extensions"),
            Icon = "fas fa-laptop-house",
            Items = new List<NavMenuItem>
            {
                Profile.HasRole(UserRole.Plugins) ? new("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins") : null,
                Profile.HasRole(UserRole.Scripts) ? new("Pages.Scripts.Title", "fas fa-scroll", "scripts") : null,
                Profile.HasRole(UserRole.Variables) ? new("Pages.Variables.Title", "fas fa-at", "variables") : null,
                Profile.HasRole(UserRole.Resources) && Profile.LicensedFor(LicenseFlags.AutoUpdates) ? new("Pages.Resources.Title", "fas fa-box-open", "resources") : null,
                Profile.HasDockerInstances && Profile.HasRole(UserRole.DockerMods) ? new ("Pages.DockerMod.Plural", "fab fa-docker", "dockermods") : null,
            }
        });

        if (Profile.LicensedFor(LicenseFlags.Reseller))
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Reseller"),
                Icon = "fas fa-people-carry",
                Items = new List<NavMenuItem>
                {
                    new ("Settings", "fas fa-people-carry", "reseller/settings"),
                    new ("Flows", "fas fa-sitemap", "reseller/flows"),
                    new ("Users", "fas fa-user-astronaut", "reseller/users")
                }
            });
            
        }
        
        MenuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.System"),
            Icon = "fas fa-desktop",
            Items = new List<NavMenuItem>
            {
                Profile.HasRole(UserRole.Log) ? new ("Pages.Log.Title", "fas fa-file-alt", "log") : null,
                Profile.HasRole(UserRole.Revisions) && Profile.LicensedFor(LicenseFlags.Revisions) ? new ("Pages.Revisions.Title", "fas fa-history", "revisions") : null,
                Profile.HasRole(UserRole.Tasks) && Profile.LicensedFor(LicenseFlags.Tasks) ? new ("Pages.Tasks.Title", "fas fa-clock", "tasks") : null,
                Profile.HasRole(UserRole.Webhooks) && Profile.LicensedFor(LicenseFlags.Webhooks) ? new ("Pages.Webhooks.Title", "fas fa-handshake", "webhooks") : null,
            }
        });
        if(Profile.IsAdmin)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Admin"),
                Icon = "fas fa-user-shield",
                Items = new List<NavMenuItem>
                {
                    new ("Pages.Settings.Title", "fas fa-cogs", "settings"),
                    Profile.LicensedFor(LicenseFlags.Reporting) ? new ("Pages.Reporting.Title", "fas fa-chart-pie", "reporting") : null,
                    new ("Pages.Notifications.Title", "fas fa-bullhorn", "notifications")
                }
            });
            
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Security"),
                Icon = "fas fa-user-shield",
                Items = new List<NavMenuItem>
                {
                    Profile.LicensedFor(LicenseFlags.Auditing) && Profile.UsersEnabled ? new ("Pages.Audit.Title", "fas fa-clipboard-list", "audit") : null,
                    Profile.LicensedFor(LicenseFlags.AccessControl) ? new ("Pages.AccessControl.Title", "fas fa-shield-alt", "access-control") : null,
                    Profile.LicensedFor(LicenseFlags.UserSecurity) ? new ("Pages.Users.Title", "fas fa-users", "users") : null
                }
            });
        }

        if (App.Instance.IsMobile)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.Information"),
                Icon = "fas fa-question-circle",
                Items = new List<NavMenuItem>
                {
                    new (lblHelp, "fas fa-question-circle", "https://fileflows.com/docs"), 
                    new (lblReddit, "fab fa-reddit-alien", "https://reddit.com/r/FileFlows"),
                    new (lblDiscord, "fab fa-discord", "https://fileflows.com/discord")
                }
            });
        }

        if (App.Instance.IsMobile && Profile.Security != SecurityMode.Off)
        {
            MenuItems.Add(new NavMenuGroup
            {
                Name = Translater.Instant("MenuGroups.User"),
                Icon = "fas fa-user",
                Items = new List<NavMenuItem>
                {
                    Profile.Security == SecurityMode.Local ? new (lblChangePassword, "fas fa-key", "#change-password") : null,
                    new (lblLogout, "fas fa-unlock", "#logout"),
                }
            });
        }

        UserMenu.Clear();
        UserMenu.Add(new("fileflows.com", "fas fa-globe", "https://fileflows.com"));
        UserMenu.Add(new(lblHelp, "fas fa-question-circle", "https://fileflows.com/docs"));
        if(Profile.Security == SecurityMode.Local)
            UserMenu.Add(new (lblChangePassword, "fas fa-key", "#change-password"));
        if(Profile.Security != SecurityMode.Off)
            UserMenu.Add(new (lblLogout, "fas fa-unlock", "#logout"));

        try
        {
            string currentRoute = NavigationManager.Uri[NavigationManager.BaseUri.Length..];
            Active = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x?.Url == currentRoute);
            if (Active == null)
            {
                if (NavigationManager.Uri.Contains("/flows"))
                {
                    // flow editor
                    Active = MenuItems.SelectMany(x => x.Items).FirstOrDefault(x => x.Url.Contains("flows"));
                }
                
                Active ??= MenuItems[0].Items.First();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        NavMenuCollapsedUpdated(App.Instance.NavMenuCollapsed);
        this.InitDone = false;
    }

    private void FileFlowsSystemUpdated(FileFlowsStatus system)
    {
        this.LoadMenu();
        this.StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (InitDone == false && MenuItems?.Any() == true)
        {
            jsNavMenu?.InvokeVoidAsync("resizeMenu");
            InitDone = true;
        }
    }

    private string GetStepLabel(NavMenuItem nmi)
    {
        if (nmi == Active)
            return null;
        if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) !=
            ConfigurationStatus.Flows)
        {
            return nmi == nmiFlows ? "Step 1" : null;
        }

        if ((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) !=
            ConfigurationStatus.Libraries)
        {
            return nmi == nmiLibraries ? "Step 2" : null; 
        }

        return null;
    }
    

    private void ToggleNavMenu()
    {
        hideNavMenu = !hideNavMenu;
    }

    async Task Click(NavMenuItem item)
    {
        UserMenuOpened = false;
        if (item == nmiPause)
        {
            await PausedService.Toggle();
            return;
        }

        if (item.Url.StartsWith("http"))
        {
            await jSRuntime.InvokeVoidAsync("open", item.Url);
            return;
        }

        if (item.Url == "#change-password")
        {
            await ChangePassword.Show();
            return;
        }

        if (item.Url == "#logout")
        {
            await ProfileService.Logout();
            return;
        }

        bool ok = await NavigationService.NavigateTo(item.Url);
        if (ok)
        {
            await jSRuntime.InvokeVoidAsync("eval", $"document.title = 'FileFlows'");
            SetActive(item);
            hideNavMenu = true;
            this.StateHasChanged();
        }
    }

    private void SetActive(NavMenuItem item)
    {
        Active = item;
        this.StateHasChanged();
    }


    public void Dispose()
    {
        // _ = bubblesTask?.StopAsync();
        // bubblesTask = null;
    }

    /// <summary>
    /// Toggles the visibility of the user menu
    /// </summary>
    private void ToggleUserMenu()
    {
        UserMenuOpened = !UserMenuOpened;
    }

    /// <summary>
    /// Updates if the nav menu is collapsed
    /// </summary>
    /// <param name="collapsed">if it is collapsed or not</param>
    public void NavMenuCollapsedUpdated(bool collapsed)
    {
        _ = jsNavMenu.InvokeVoidAsync("menuSet", new object []
        {
            MenuItems.Count, 
            MenuItems.SelectMany(x => x.Items).Count(x => x != nmiPause),
            collapsed
        });
    }
}

public class NavMenuGroup
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public List<NavMenuItem> Items { get; set; } = new List<NavMenuItem>();
}

public class NavMenuItem
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }

    public NavMenuItem(string title = "", string icon = "", string url = "")
    {
        this.Title = title == "fileflows.com" ? title : Translater.TranslateIfNeeded(title);
        this.Icon = icon;
        this.Url = url;
    }
}