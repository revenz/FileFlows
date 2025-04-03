using Microsoft.JSInterop;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Services.Frontend.Handlers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace FileFlows.Client.Shared;

/// <summary>
/// Main navigation bar
/// </summary>
public partial class NavBar
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }
    
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
    [Inject] private FrontendService feService { get; set; }
    
    
    private List<NavBarItem> MenuItems = new ();
    private List<NavBarItem> BottomNavBarItems = new ();
    
    public NavBarItem Active { get; private set; }

    private string lblVersion, lblHelp, lblDiscord, lblChangePassword, lblLogout, lblStep1, lblStep2; //, lblReddit;

    /// <summary>
    /// Totals for the bubbles
    /// </summary>
    private int TotalUnprocessed, TotalFailed, TotalProcessing;

    /// <summary>
    /// If the user menu is opened or closed
    /// </summary>
    private bool UserMenuOpened = false;

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
    /// If the flows or libraries are pending creation
    /// </summary>
    private bool FlowsPending, LibrariesPending;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblVersion = Translater.Instant("Labels.Version");
        lblHelp = Translater.Instant("Labels.Help");
        lblDiscord = Translater.Instant("Labels.Discord");
        lblChangePassword = Translater.Instant("Labels.ChangePassword");
        lblLogout = Translater.Instant("Labels.Logout");
        
        lblStep1 = Translater.Instant("Labels.Step1");
        lblStep2 = Translater.Instant("Labels.Step2");
        
        NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;

        ProfileService.OnRefresh += ProfileServiceOnOnRefresh; 
        Profile = feService.Profile.Profile;

        TotalUnprocessed = feService.Files.FileQueue.Count;
        TotalProcessing = feService.Files.Processing.Count;
        TotalFailed = feService.Files.FailedFiles.Count;
        
        this.LoadMenu();
        //BottomNavBarItems.Add(new(lblReddit, "fab fa-reddit-alien", "https://reddit.com/r/FileFlows"));
        //BottomNavBarItems.Add(new(lblDiscord, "fab fa-discord", "https://fileflows.com/discord"));
        
        feService.Files.UnprocessedUpdated += OnUnprocessedUpdated;
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
        feService.Files.FailedFilesUpdated += OnFailedFilesUpdated;

        if (feService.Flow.Flows.Count == 0)
        {
            FlowsPending = true;
            feService.Flow.FlowsUpdated += OnFlowsUpdated;
        }

        if (feService.Library.Libraries.Count == 1) // 1 since Manual library is one
        {
            LibrariesPending = true;
            feService.Library.LibrariesUpdated += OnLibrariesUpdated;
        }
        try
        {
            string currentRoute = NavigationManager.Uri[NavigationManager.BaseUri.Length..];
            Active = MenuItems.Union(BottomNavBarItems).FirstOrDefault(x => x?.Url == currentRoute);
            if (Active == null)
            {
                if (NavigationManager.Uri.Contains("flows/"))
                {
                    // flow editor
                    Active = MenuItems.FirstOrDefault(x => x.Url.Contains("flows"));
                }
                
                if (NavigationManager.Uri.Contains("config/"))
                    Active = MenuItems.FirstOrDefault(x => x.Url.Contains("config/"));
                
                Active ??= MenuItems.First();
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void OnUnprocessedUpdated(List<LibraryFileMinimal> obj)
    {
        if (TotalUnprocessed == obj.Count)
            return;
        TotalUnprocessed = obj.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the processing files is updated
    /// </summary>
    /// <param name="obj">the data</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> obj)
    {
        if (TotalProcessing == obj.Count)
            return;
        TotalProcessing = obj.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the failed files is updated
    /// </summary>
    /// <param name="obj">the data</param>
    private void OnFailedFilesUpdated(FileHandler.ListAndCount<LibraryFileMinimal> obj)
    {
        if (obj.Total == TotalFailed)
            return;
        TotalFailed = obj.Total;
        StateHasChanged();
    }

    /// <summary>
    /// Called when flows are updated
    /// </summary>
    /// <param name="flows">the updated flows</param>
    private void OnFlowsUpdated(List<FlowListModel> flows)
    {
        // once a flow has been added, the step 1 indicator is no longer tracked
        feService.Flow.FlowsUpdated -= OnFlowsUpdated;
        FlowsPending = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called when libraries are updated
    /// </summary>
    /// <param name="libraries">the updated libraries</param>
    private void OnLibrariesUpdated(List<LibraryListModel> libraries)
    {
        // once a library has been added, the step 1 indicator is no longer tracked
        feService.Library.LibrariesUpdated -= OnLibrariesUpdated;
        LibrariesPending = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the counts were updated
    /// </summary>
    /// <param name="counts">the updated counts</param>
    private void FilesOnLibraryFileCountsUpdated(List<LibraryStatus> counts)
    {
        int newTotalFailed = counts.FirstOrDefault(x => x.Status == FileStatus.ProcessingFailed)?.Count ?? 0;

        if (newTotalFailed == TotalFailed)
            return;
        
        TotalFailed = newTotalFailed;
        StateHasChanged();
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
        
        var item = MenuItems.FirstOrDefault(x => x?.Url == lastRoute);
        if (item == null)
            return;

        Active = item;
        StateHasChanged();
    }

    
    void LoadMenu()
    {
        this.MenuItems.Clear();

        MenuItems.Add(new ("dashboard", Translater.Instant("Pages.Dashboard.Title"), "fas fa-chart-pie", ""));
        MenuItems.Add(new ("files", Translater.Instant("Pages.LibraryFiles.Title"), "fas fa-copy", "library-files"));
        
        if(Profile.HasRole(UserRole.Flows))
            MenuItems.Add(new ("flows", Translater.Instant("Pages.Flows.Title"), "fas fa-sitemap", "flows", false));
        
        if(Profile.HasRole(UserRole.Libraries))
            MenuItems.Add(new ("libraries", Translater.Instant("Pages.Libraries.Title"), "fas fa-folder", "libraries"));

        if(Profile.HasRole(UserRole.Nodes))
            MenuItems.Add(new ("nodes", Translater.Instant("Pages.Nodes.Title"), "fas fa-desktop", "nodes"));
         
        if(Profile.LicensedFor(LicenseFlags.Reporting) && Profile.HasRole(UserRole.Reports))
            MenuItems.Add(new ("reporting", Translater.Instant("Pages.Reporting.Title"), "fas fa-chart-pie", "reporting", false));

        if(Profile.HasRole(UserRole.Log))
            BottomNavBarItems.Add(new ("log", Translater.Instant("Pages.Log.Title"), "fas fa-file-alt", "log"));

        var firstConfig = ConfigLayout.GetFirstAvailableItem(Profile);
        if(string.IsNullOrEmpty(firstConfig) == false)
            BottomNavBarItems.Add(new ("config", Translater.Instant("MenuGroups.Config"), "fas fa-cogs", firstConfig));

        if (Profile.LicensedFor(LicenseFlags.FileDrop))
        {
            BottomNavBarItems.Add(new ("filedrop", "FileDrop", "fas fa-tint", "/file-drop/general", false));
        }        
        
        if(Profile.Security == SecurityMode.Local)
            BottomNavBarItems.Add(new ("change-password", lblChangePassword, "fas fa-key", "#change-password"));
        if(Profile.Security != SecurityMode.Off)
            BottomNavBarItems.Add(new ("logout", lblLogout, "fas fa-unlock", "/logout"));
    }
    
    async Task Click(NavBarItem item)
    {
        UserMenuOpened = false;
        if (item.Url.StartsWith("http"))
        {
            await jsRuntime.InvokeVoidAsync("open", item.Url);
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
            await jsRuntime.InvokeVoidAsync("eval", $"document.title = 'FileFlows'");
            SetActive(item);
        }
    }

    private void SetActive(NavBarItem item)
    {
        Active = item;
        this.StateHasChanged();
    }

    /// <summary>
    /// Toggles the visibility of the user menu
    /// </summary>
    private void ToggleUserMenu()
    {
        UserMenuOpened = !UserMenuOpened;
    }

    /// <summary>
    /// Confirms a user log out
    /// </summary>
    /// <returns>a task to await</returns>
    private async Task ConfirmLogOut()
    {
        if (await Confirm.Show(
                Translater.Instant("Labels.ConfirmLogOutTitle"), 
                Translater.Instant("Labels.ConfirmLogOutMessage")) == false)
            return;

        await jsRuntime.InvokeVoidAsync("ff.logout");
        NavigationManager.NavigateTo("/logout", true);
    }
}


public record NavBarItem(string Uid, string Title, string Icon, string Url, bool Mobile = true);