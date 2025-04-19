using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Editors;
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
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]private FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the message service
    /// </summary>
    [Inject] MessageService Message { get; set; }
    
    /// <summary>
    /// Gets or sets the local storage service
    /// </summary>
    [Inject]
    private FFLocalStorageService LocalStorageService { get; set; }
    
    /// <summary>
    /// Gets or sets the click outside serice
    /// </summary>
    [Inject] private ClickOutsideService ClickOutside { get; set; }
    
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
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
    private int NumberOfRunners;

    /// <summary>
    /// The notifications
    /// </summary>
    private List<Notification> Notifications => feService.Notifications.All;
    
    /// <summary>
    /// Gets or sets if the popup is visible
    /// </summary>
    private bool Visible { get; set; }

    /// <summary>
    /// If the connection to the server has been lost
    /// </summary>
    private bool ConnectionLost = false;
    
    /// <summary>
    /// If the system is upgrading
    /// </summary>
    private bool Upgrading;
    
    /// <summary>
    /// If the system has an upgrade pending
    /// </summary>
    private bool UpgradePending;

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblDisconnected;

    /// <summary>
    /// Translations
    /// </summary>
    private string lblHelp, lblChangePassword, lblLogout, lblPaused, lblIdle, lblPause, lblResume, lblDismissAll;

    private List<Guid> SeenNotifications = [];

    private bool ShowLogout, ShowChangePassword;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblHelp = Translater.Instant("Labels.Help");
        lblChangePassword = Translater.Instant("Labels.ChangePassword");
        lblLogout = Translater.Instant("Labels.Logout");
        lblPaused = Translater.Instant("Labels.Paused");
        lblIdle = Translater.Instant("Labels.Idle");
        lblPause = Translater.Instant("Labels.Pause");
        lblResume = Translater.Instant("Labels.Resume");
        lblDismissAll = Translater.Instant("Labels.DismissAll");
        
        SeenNotifications = await LocalStorageService.GetItemAsync<List<Guid>>("SeenNotifications") ?? [];

        
        NumberOfRunners = feService.Files.Processing.Count;
        feService.Notifications.OnNotification += OnNotification;
        feService.Notifications.OnNotificationsUpdated += OnNotificationsUpdated;
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
        ClickOutside.OnClickOutside += HidePopup;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;

        ShowChangePassword = feService.Profile.Profile.Security == SecurityMode.Local;
        ShowLogout = feService.Profile.Profile.Security != SecurityMode.Off;
        
#if(DEBUG)
        ShowChangePassword = true;
        ShowLogout = true;
#endif

        lblDisconnected = Translater.Instant("Labels.Disconnected");
        ConnectionLost = feService.ConnectionLost;
        feService.OnConnectionLost += OnConnectionLost; 
        feService.System.OnUpdatePending += OnUpgradePending; 
        feService.System.OnUpgrading += OnUpgrading; 
        
        foreach (var notification in feService.Notifications.Notifications)
        {
            if (notification.Severity is NotificationSeverity.Information or NotificationSeverity.Success)
                continue; // dont care about these ones
            if (SeenNotifications.Contains(notification.Uid))
                continue;
            _ = jsRuntime.InvokeVoidAsync("showToast",
                notification.Severity.ToString().ToLower(), notification.Title, notification.Message);
        }
    }

    /// <summary>
    /// Called when the system is upgrading
    /// </summary>
    /// <param name="upgrading">if the system is upgraing</param>
    private void OnUpgrading(bool upgrading)
    {
        Upgrading = upgrading;
        UpgradePending = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called when an upgrade is pending
    /// </summary>
    /// <param name="pending">if the upgrade is pending</param>
    private void OnUpgradePending(bool pending)
    {
        UpgradePending = pending;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the server connection is lost
    /// </summary>
    /// <param name="connectionLost">true if the connection is lost, false if the connection is restablished</param>
    private void OnConnectionLost(bool connectionLost)
    {
        ConnectionLost = connectionLost;
        StateHasChanged();
    }

    /// <summary>
    /// Gets if the paused label has changed
    /// </summary>
    /// <param name="label">the updated label</param>
    private void PausedServiceOnOnPausedLabelChanged(string label)
    {
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if(firstRender)
            ClickOutside.Watch(".popup-panel");
        
        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Called when a notification is received
    /// </summary>
    /// <param name="notification">the notification</param>
    /// <param name="isToast">if its a toast notification from the client side</param>
    private void OnNotification(Notification notification, bool isToast)
    {
        if (Visible)
            return;
        _ = jsRuntime.InvokeVoidAsync("showToast",
            notification.Severity.ToString().ToLower(), notification.Title, notification.Message);
        if(isToast == false)
            _ = SetAsSeen(notification.Uid);
    }

    /// <summary>
    /// Called when files processing are updated
    /// </summary>
    /// <param name="runners">the updated runners</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> runners)
    {
        if (NumberOfRunners == runners.Count)
            return;
        NumberOfRunners = runners.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Hides the popup
    /// </summary>
    void HidePopup()
    {
        if (!Visible)
            return;
        Visible = false;
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
        feService.Notifications.OnNotification -= OnNotification;
        feService.Notifications.OnNotificationsUpdated -= OnNotificationsUpdated;
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
        ClickOutside.OnClickOutside -= HidePopup;
        feService.OnConnectionLost -= OnConnectionLost;
        feService.System.OnUpdatePending -= OnUpgradePending; 
        feService.System.OnUpgrading -= OnUpgrading; 
        _ = ClickOutside.DisposeAsync();
    }

    /// <summary>
    /// Toggles the visibility of the popup
    /// </summary>
    private async Task TogglePopup()
    {
        if (!Visible)
        {
            await CloseAllToasts();
            await Task.Delay(20);
        }

        Visible = !Visible;
    }

    /// <summary>
    /// Confirms a user log out
    /// </summary>
    /// <returns>a task to await</returns>
    private async Task LogOut()
    {
        Visible = false;
        
        if (await Message.Confirm(
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
        Visible = false;

        _ = ModalService.ShowModal<ChangePassword>(new ModalEditorOptions());
    }
    
    /// <summary>
    /// Close all the toast
    /// </summary>
    private async Task CloseAllToasts()
    {
        await jsRuntime.InvokeVoidAsync("closeAllToasts");
    }

    /// <summary>
    /// Dismisses a notification
    /// </summary>
    /// <param name="notification">the notification to dismiss</param>
    private async Task Dismiss(Notification notification)
    {
        feService.Notifications.Dismiss(notification);
        if(SeenNotifications.Remove(notification.Uid))
            await LocalStorageService.SetItemAsync("SeenNotifications", SeenNotifications);
    }

    /// <summary>
    /// Resume processing
    /// </summary>
    private void ResumeProcessing()
    {
        PausedService.Resume();
        Visible = false;
    }
    
    /// <summary>
    /// Pause processing
    /// </summary>
    private void PauseProcessing()
    {
        _ = PausedService.Pause();
        Visible = false;
    }

    /// <summary>
    /// Dismisses all notifications
    /// </summary>
    private async Task DismissAll()
    {
        feService.Notifications.DismissAll();
        SeenNotifications = [];
        await LocalStorageService.SetItemAsync("SeenNotifications", SeenNotifications);
    }

    /// <summary>
    /// Sets a notification as seen
    /// </summary>
    /// <param name="uid">the UID of the notification</param>
    private async Task SetAsSeen(Guid uid)
    {
        SeenNotifications.Add(uid);
        while(SeenNotifications.Count > 30)
            SeenNotifications.RemoveAt(0);
        await LocalStorageService.SetItemAsync("SeenNotifications", SeenNotifications);
    }
}