using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Change password dialog
/// </summary>
public partial class ChangePassword : IModal
{
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the modal
    /// </summary>
    private Modal Modal { get; set; }
    
    private string lblTitle, lblSave, lblCancel, lblOldPassword, lblNewPassword, lblNewPasswordConfirm;
    private readonly string txtOldPasswordUid = Guid.NewGuid().ToString();
    private string oldPassword = string.Empty, newPassword = string.Empty, newPasswordConfirm = string.Empty;

    /// <summary>
    /// Saving the password
    /// </summary>
    private bool saving = false;
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblTitle = Translater.Instant("Dialogs.ChangePassword.Title");
        lblOldPassword = Translater.Instant("Dialogs.ChangePassword.OldPassword");
        lblNewPassword = Translater.Instant("Dialogs.ChangePassword.NewPassword"); 
        lblNewPasswordConfirm = Translater.Instant("Dialogs.ChangePassword.NewPasswordConfirm");
    }

    /// <summary>
    /// After the component is rendered
    /// </summary>
    /// <param name="firstRender">the first render</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{txtOldPasswordUid}').focus()");
    }

    /// <summary>
    /// Saves the password change
    /// </summary>
    private async void Save()
    {
        if(string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(newPasswordConfirm))
            return;

        if (newPassword != newPasswordConfirm)
        {
            feService.Notifications.ShowError("Dialogs.ChangePassword.PasswordMismatch");
            return;
        }

        Modal?.Blocker?.Show();
        try
        {
            saving = true;
            var result = await HttpHelper.Post("/authorize/change-password", new
            {
                oldPassword, newPassword
            });
            saving = false;
            if (result.Success == false)
            {
                feService.Notifications.ShowError(result.Body);
            }
            else
            {
                feService.Notifications.ShowSuccess("Dialogs.ChangePassword.Changed");
                TaskCompletionSource.TrySetResult(true);
            }
        }
        finally
        {
            Modal?.Blocker.Hide();
            StateHasChanged();
        }
    }


    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }
    
    /// <summary>
    /// On key down
    /// </summary>
    /// <param name="args">the keyboard event args</param>
    private void OnKeyDown(KeyboardEventArgs args)
    {
        if(args.Key == "Enter")
        {
            Save();
        }
    }
}