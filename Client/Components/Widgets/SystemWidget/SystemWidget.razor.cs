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
    private int _Mode = 0;
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            if(App.Instance.IsMobile) // only do this on mobile, on desktop we calculate the best mode to show
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
    private string lblTitle, lblRunners, lblNodes, lblSavings;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.System.Title");
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = 0});
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
        if(App.Instance.IsMobile)
            Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 2);
        OnProcessingUpdated(feService.Files.Processing ?? []);
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
    }


    /// <summary>
    /// Raised when the executors are updated
    /// </summary>
    /// <param name="info">the executors</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> info)
    {
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = info.Count});
        OptionButtons?.TriggerStateHasChanged();
        StateHasChanged();
    }


    /// <summary>
    /// Select nodes if no runners are running on load
    /// </summary>
    private void SelectNodes()
    {
        if (App.Instance.IsMobile)
            return;
        Mode = 2;
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
    }
}