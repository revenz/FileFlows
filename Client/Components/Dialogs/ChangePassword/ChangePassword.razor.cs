using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Change password dialog
/// </summary>
public partial class ChangePassword: VisibleEscapableComponent
{
    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    private string lblTitle, lblSave, lblCancel, lblOldPassword, lblNewPassword, lblNewPasswordConfirm;
    TaskCompletionSource ShowTask;

    private string txtOldPasswordUid;
    private string oldPassword, newPassword, newPasswordConfirm;

    /// <summary>
    /// Saving the password
    /// </summary>
    private bool saving = false;
    /// <summary>
    /// if the input requires a focus event
    /// </summary>
    private bool focused = false;

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
    /// Shows the password change dialog
    /// </summary>
    /// <returns>the task to await</returns>
    public Task Show()
    {
        oldPassword = string.Empty;
        newPassword = string.Empty;
        newPasswordConfirm = string.Empty;
        Task.Run(async () =>
        {
            // wait a short delay this is in case a "Close" from an escape key is in the middle
            // of processing, and if we show this confirm too soon, it may automatically be closed
            await Task.Delay(5);
            txtOldPasswordUid = Guid.NewGuid().ToString();
            focused = false;
            Visible = true;
            StateHasChanged();
        });

        ShowTask = new TaskCompletionSource();
        return ShowTask.Task;
    }

    /// <summary>
    /// After the component is rendered
    /// </summary>
    /// <param name="firstRender">the first render</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Visible && focused == false)
        {
            focused = true;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{txtOldPasswordUid}').focus()");
        }
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
            Visible = false;
            ShowTask.TrySetResult();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Cancels the password change
    /// </summary>
    public override void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult();
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