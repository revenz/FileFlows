using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Message box popup with an OK button 
/// </summary>
public partial class MessageBox : IModal
{
    /// <summary>
    /// Gets or sets th e javascript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    private string lblOk;
    private string Message, Title;
    private string btnOkUid; 

    private static MessageBox Instance { get; set; }

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        if (Options is MessageBoxOptions options == false)
        {
            Close();
            return;
        }
        
        lblOk = Translater.Instant("Labels.Ok");
        btnOkUid = Guid.NewGuid().ToString();
        Title = Translater.TranslateIfNeeded(options.Title?.EmptyAsNull() ?? "Labels.Message");
        Message = Translater.TranslateIfNeeded(options.Message ?? string.Empty);
    }
    

    /// <summary>
    /// Called after rendering the message box
    /// </summary>
    /// <param name="firstRender">true if its the first render or not</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{this.btnOkUid}').focus()");
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
}

/// <summary>
/// Message box options
/// </summary>
public class MessageBoxOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Gets or sets the message
    /// </summary>
    public string Message { get; set; }
}