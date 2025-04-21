using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Shared;

public partial class MainLayout : LayoutComponentBase
{
    /// <summary>
    /// Gets or sets the menu
    /// </summary>
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
    
    private string _Title, _Icon, _PageClass;
    private bool _NoPadding;
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }

    public static MainLayout Instance { get; private set; }

    public MainLayout()
    {
        Instance = this;
    }

    /// <summary>
    /// Sets the info
    /// </summary>
    /// <param name="title">the page title</param>
    /// <param name="icon">the page icon</param>
    /// <param name="noPadding">if no padding is applied to the content</param>
    public void SetInfo(string title, string icon, bool noPadding = false, string? pageClass = null)
    {
        _Title = title;
        _Icon = icon;
        _NoPadding = noPadding;
        _PageClass = pageClass;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        HttpHelper.On401 = On401;
        HttpHelper.OnRedirect = OnRedirect;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if(firstRender)
            StateHasChanged();
        
        base.OnAfterRender(firstRender);
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
}