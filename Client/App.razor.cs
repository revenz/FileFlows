using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client;

/// <summary>
/// The main Application
/// </summary>
public partial class App : ComponentBase
{
    /// <summary>
    /// The instance of the application
    /// </summary>
    public static App Instance { get; private set; }
    public delegate void DocumentClickDelegate();
    public event DocumentClickDelegate OnDocumentClick;
    public delegate void WindowBlurDelegate();
    public event WindowBlurDelegate OnWindowBlur;

    [Inject] public HttpClient Client { get; set; }
    [Inject] public IJSRuntime jsRuntime { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }

    public int DisplayWidth { get; private set; }
    public int DisplayHeight { get; private set; }

    /// <summary>
    /// Gets if being viewed on a mobile device
    /// </summary>
    public bool IsMobile => DisplayWidth is > 0 and <= 850;
    /// <summary>
    /// Gets if being viewed on a small mobile device
    /// </summary>
    public bool IsSmallMobile => DisplayWidth is > 0 and <= 600;

    /// <summary>
    /// Delegate for the on escape event
    /// </summary>
    public delegate void EscapePushed(OnEscapeArgs args);

    /// <summary>
    /// Instance of the escape event publisher.
    /// </summary>
    public EscapeEventPublisher OnEscapePublisher { get; } = new();
    /// <summary>
    /// A list of the already subscribed listeners
    /// </summary>
    private readonly HashSet<EscapePushedEventHandler> _subscribedHandlers = new();
    /// <summary>
    /// Expose the OnEscapePushed event.
    /// </summary>
    public event EscapePushedEventHandler OnEscapePushed
    {
        add
        {
            // Only add the handler if it's not already in the set
            if (_subscribedHandlers.Add(value))
            {
                OnEscapePublisher.OnEscapePushed += value;
            }
        }
        remove
        {
            if (_subscribedHandlers.Remove(value))
            {
                OnEscapePublisher.OnEscapePushed -= value;
            }
        }
    }
    
    /// <summary>
    /// Gets or sets if the nav menu is collapsed
    /// </summary>
    public bool NavMenuCollapsed { get; set; }

    //
    // /// <summary>
    // /// Loads the language files from the server
    // /// </summary>
    // /// <param name="language">Optional language to load</param>
    // public async Task LoadLanguage(string? language = null)
    // {
    //     var langFile = await LoadLanguageFile($"/api/language?version={Globals.Version}&t={DateTime.Now.Ticks}&language={language}");;
    //     Translater.Init(langFile);
    // }

    //
    // private async Task<string> LoadLanguageFile(string url)
    // {
    //     return (await HttpHelper.Get<string>(url)).Data ?? "";
    // }

    protected override async Task OnInitializedAsync()
    {
        Instance = this;
        ClientConsoleLogger.jsRuntime = jsRuntime;
        new ClientConsoleLogger();
        HttpHelper.Client = Client;

        var dimensions = await jsRuntime.InvokeAsync<Dimensions>("ff.deviceDimensions");
        DisplayWidth = dimensions.width;
        DisplayHeight = dimensions.height;
        var dotNetObjRef = DotNetObjectReference.Create(this);
        _ = jsRuntime.InvokeVoidAsync("ff.onEscapeListener", new object[] { dotNetObjRef });
        _ = jsRuntime.InvokeVoidAsync("ff.attachEventListeners", new object[] { dotNetObjRef });
        _ = jsRuntime.InvokeVoidAsync("ff.setCSharp",  new object[] { dotNetObjRef });
        
        feService.OnInitialized += FrontendServiceOnOnInitialized;
        var authToken = await LocalStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(authToken) == false)
        {
            HttpHelper.Client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", authToken);
            
        }
#if(DEBUG)
        feService.StartListening("http://localhost:6868/api/sse", authToken);
#else
        feService.StartListening("/api/sse", authToken);
#endif
    }

    private void FrontendServiceOnOnInitialized()
    {
        StateHasChanged();
    }

    record Dimensions(int width, int height);

    /// <summary>
    /// Method called by javascript for events we listen for
    /// </summary>
    /// <param name="eventName">the name of the event</param>
    [JSInvokable]
    public void EventListener(string eventName)
    {
        if(eventName == "WindowBlur")
            OnWindowBlur?.Invoke();
        else if(eventName == "DocumentClick")
            OnDocumentClick?.Invoke(); ;
    }

    /// <summary>
    /// Escape was pushed
    /// </summary>
    [JSInvokable]
    public void OnEscape(OnEscapeArgs args)
    {
        OnEscapePublisher.RaiseEscapePushed();
    }
    
    

    /// <summary>
    /// Navigates to a route
    /// </summary>
    [JSInvokable]
    public void NavigateTo(string url)
    {
        NavigationManager.NavigateTo(url);
        StateHasChanged();
    }

    /// <summary>
    /// Opens a url
    /// </summary>
    [JSInvokable]
    public async Task<bool> OpenUrl(string url)
    {
        if(feService.Profile.Profile.IsWebView)
            return false;
        await HttpHelper.Post("/api/system/open-url?url=" + HttpUtility.UrlEncode(url));
        return true;
    }

    public async Task OpenHelp(string url)
    {
        if (feService.Profile.Profile.IsWebView == false)
            await jsRuntime.InvokeVoidAsync("ff.open", url, true);
        else
            await OpenUrl(url);
    }

}


/// <summary>
/// Args for on escape event
/// </summary>
public class OnEscapeArgs
{
    /// <summary>
    /// Gets if there is a modal visible
    /// </summary>
    public bool HasModal { get; init; }

    /// <summary>
    /// Gets if the log partial viewer is open 
    /// </summary>
    public bool HasLogPartialViewer { get; init; }
    
    /// <summary>
    /// Gets or sets if propagation should be stopped
    /// </summary>
    public bool StopPropagation { get; set; }
}