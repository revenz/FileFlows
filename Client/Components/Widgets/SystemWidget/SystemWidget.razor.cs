using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Flow Runners widget
/// </summary>
public partial class SystemWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// The key used to store the selected mode in system widget
    /// </summary>
    private const string LocalStorageKey = "SystemWidget";
    
    /// <summary>
    /// Gets the mode
    /// </summary>
    private int _Mode = 1;
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
            StateHasChanged();
        }
    }

    /// <summary>
    /// The option buttons
    /// </summary>
    private OptionButtons OptionButtons;

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblNodes, lblSavings; // lblRunners

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.System.Title");
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
        Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 1);
    }


    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
    }
}