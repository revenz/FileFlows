using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// If any files are currently processing
    /// </summary>
    private bool IsProcessing => NumberOfRunners > 0;

    /// <summary>
    /// Gets the number of runners
    /// </summary>
    private int NumberOfRunners { get; set; }

    /// <summary>
    /// The notifications
    /// </summary>
    private List<Notification> Notifications => feService.Notifications.All;
    
    /// <summary>
    /// Gets or sets if the popup is visible
    /// </summary>
    private bool Visible { get; set; }

    /// <summary>
    /// Translations
    /// </summary>
    private string lblHelp, lblChangePassword, lblLogout; 
    
    private bool ShowLogout, ShowChangePassword;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblHelp = Translater.Instant("Labels.Help");
        lblChangePassword = Translater.Instant("Labels.ChangePassword");
        lblLogout = Translater.Instant("Labels.Logout");
        
        NumberOfRunners = feService.Files.Processing.Count;
        feService.Notifications.OnNotificationsUpdated += OnNotificationsUpdated;
        feService.Files.ProcessingUpdated += OnProcessingUpdated;

        ShowChangePassword = feService.Profile.Profile.Security == SecurityMode.Local;
        ShowLogout = feService.Profile.Profile.Security != SecurityMode.Off;
        
        #if(DEBUG)
        ShowChangePassword = true;
        ShowLogout = true;
#endif
    }

    /// <summary>
    /// Called when files processing are updated
    /// </summary>
    /// <param name="runners">the updated runners</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> runners)
    {
        if (NumberOfRunners != runners.Count)
            return;
        NumberOfRunners = runners.Count;
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
    {
        
        Visible = !Visible;
    }

    /// <summary>
    /// Confirms a user log out
    /// </summary>
    /// <returns>a task to await</returns>
    private async Task LogOut()
    {
        if (await Confirm.Show(
                Translater.Instant("Labels.ConfirmLogOutTitle"), 
                Translater.Instant("Labels.ConfirmLogOutMessage")) == false)
            return;

        await jsRuntime.InvokeVoidAsync("ff.logout");
        NavigationManager.NavigateTo("/logout", true);
    }

    /// <summary>
    /// Prompt to change the users password
    /// </summary>
    private void ChangePassword()
    {
        
    }
}