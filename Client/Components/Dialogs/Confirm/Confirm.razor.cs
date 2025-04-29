using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Confirm dialog that prompts the user for confirmation 
/// </summary>
public partial class Confirm : IModal
{
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    private string lblYes, lblNo;
    private string Message, Title, SwitchMessage;
    /// <summary>
    /// The default value of the button to focus when shown, true for yes, false for no
    /// </summary>
    private bool DefaultValue;
    private bool ShowSwitch;
    private bool SwitchState;
    private bool RequireSwitch;

    private readonly string btnYesUid = Guid.NewGuid().ToString();

    private readonly string btnNoUid = Guid.NewGuid().ToString(); 
    
    private bool focused = false;

    protected override void OnInitialized()
    {
        this.lblYes = Translater.Instant("Labels.Yes");
        this.lblNo = Translater.Instant("Labels.No");
        if (Options is ConfirmOptions options == false)
        {
            Close();
            return;
        }
            
        ShowSwitch = options.ShowSwitch;
        Message = Translater.TranslateIfNeeded(options.Message ?? string.Empty);
        SwitchMessage = Translater.TranslateIfNeeded(options.SwitchMessage ?? string.Empty);
        Title = Translater.TranslateIfNeeded(options.Title?.EmptyAsNull() ?? "Labels.Confirm");;
        DefaultValue = options.DefaultValue;
        RequireSwitch = options.RequireSwitch;
        SwitchState = options.SwitchState;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focused == false)
        {
            focused = true;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{(DefaultValue ? btnYesUid : btnNoUid )}').focus()");
        }
    }

    private void Yes()
    {
        if (ShowSwitch)
        {
            TaskCompletionSource.TrySetResult((true, SwitchState));
        }
        else
        {
            TaskCompletionSource.TrySetResult(true);
        }
    }

    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
        => Cancel();

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        if (ShowSwitch)
        {
            TaskCompletionSource.TrySetResult((false, SwitchState));
        }
        else
        {
            TaskCompletionSource.TrySetResult(false);
        }
    }

}

/// <summary>
/// The confirm options
/// </summary>
public class ConfirmOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets if a switch is shown
    /// </summary>
    public bool ShowSwitch { get; set; }
    
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// Gets or sets the message
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Gets or sets the default value to have highlighted, true for confirm, false for reject<
    /// </summary>
    public bool DefaultValue { get; set; }
    
    /// <summary>
    /// Gets or sets the switch message
    /// </summary>
    public string SwitchMessage { get; set; }
    
    /// <summary>
    /// Gets or sets if the switch is required to be checked for the YES button to become enabled
    /// </summary>
    public bool RequireSwitch { get; set; }
    
    /// <summary>
    /// Gets or sets the switch state
    /// </summary>
    public bool SwitchState { get; set; }
}