using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;

namespace FileFlows.Client.Shared;

public partial class MainLayout : LayoutComponentBase
{
    public NavBar Menu { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    public Blocker Blocker { get; set; }
    public Blocker DisconnectedBlocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor { get; set; }
    [Inject] private ClientService ClientService { get; set; }
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
            
        this.ClientService.Connected += ClientServiceOnConnected;
        this.ClientService.Disconnected += ClientServiceOnDisconnected;
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

    private void ClientServiceOnDisconnected()
    {
        DisconnectedBlocker.Show("Disconnected");
    }

    private void ClientServiceOnConnected()
    {
        DisconnectedBlocker.Hide();
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