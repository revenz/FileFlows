using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Shared;

/// <summary>
/// Configuration layout
/// </summary>
public abstract partial class ConfigurationLayout : LayoutComponentBase
{
    /// <summary>
    /// The menu items
    /// </summary>
    private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();
    
    /// <summary>
    /// Gets or sets if they are opened
    /// </summary>
    private bool Opened { get; set; }

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

    /// <summary>
    /// Gets the menu to show
    /// </summary>
    /// <param name="profile">the users profile</param>
    /// <returns>the menu items</returns>
    protected abstract List<NavMenuGroup> GetMenu(Profile profile);

    async Task Click(NavMenuItem item)
    {
        Opened = false;
        bool ok = await NavigationService.NavigateTo(item.Url);
        if (ok)
        {
            SetActive(item);
        }
    }
    
    /// <summary>
    /// Sets the active menu item
    /// </summary>
    /// <param name="item">the menu item to activate</param>
    private void SetActive(NavMenuItem item)
    {
        Active = item;
        this.StateHasChanged();
    }

    /// <summary>
    /// Toggles the menu opened
    /// </summary>
    private void ToggleOpen()
    {
        Opened = !Opened;
    }
}

