using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Shared;

public partial class MainLayout : LayoutComponentBase
{
    public NavBar Menu { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor { get; set; }
    
    /// <summary>
    /// Gets or sets the main front end service that proxies all live data from the server
    /// </summary>
    [Inject] private FrontendService FrontendService { get; set; }
    
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }

    public static MainLayout Instance { get; private set; }

    private bool SearchVisible = false;

    public MainLayout()
    {
        Instance = this;
    }

    protected override async Task OnInitializedAsync()
    {
        HttpHelper.On401 = On401;
        HttpHelper.OnRedirect = OnRedirect;
        App.Instance.NavMenuCollapsed = await LocalStorage.GetItemAsync<bool>("NavMenuCollapsed");
    }


    private void On401()
    {
        #if(DEBUG)
        NavigationManager.NavigateTo("http://localhost:6868/login", true);
        #else
        NavigationManager.NavigateTo("/login", true);
        #endif
    }

    /// <summary>
    /// Redirect result from HTTP helper
    /// </summary>
    /// <param name="location">the location</param>
    private void OnRedirect(string location)
    {
        NavigationManager.NavigateTo(location, true);
    }

    public void ShowSearch()
    {
        SearchVisible = true;
        this.StateHasChanged();
    }

    public void HideSearch()
    {
        SearchVisible = false;
        this.StateHasChanged();
        
    }
}