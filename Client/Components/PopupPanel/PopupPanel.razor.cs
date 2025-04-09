using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Popup panel
/// </summary>
public partial class PopupPanel : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]private FrontendService feService { get; set; }

    /// <summary>
    /// If any files are currently processing
    /// </summary>
    private bool IsProcessing = false;

    /// <summary>
    /// The notifications
    /// </summary>
    private List<Notification> Notifications => feService.Notifications.All;
    
    /// <summary>
    /// Gets or sets if the popup is visible
    /// </summary>
    private bool Visible { get; set; }
    
    protected override void OnInitialized()
    {
        IsProcessing = feService.Files.Processing.Count > 0;
        feService.Notifications.OnNotificationsUpdated += OnNotificationsUpdated;
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
    }

    /// <summary>
    /// Called when files processing are updated
    /// </summary>
    /// <param name="runners">the updated runners</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> runners)
    {
        if (IsProcessing == runners.Count > 0)
            return;
        IsProcessing = runners.Count > 0;
        StateHasChanged();
    }

    /// <summary>
    /// When a notification is received
    /// </summary>
    /// <param name="data">the updated notifications</param>
    private void OnNotificationsUpdated()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Notifications.OnNotificationsUpdated -= OnNotificationsUpdated;
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
    }

    /// <summary>
    /// Toggles the visiblity of the popup
    /// </summary>
    private void TogglePopup()
        => Visible = !Visible;
}