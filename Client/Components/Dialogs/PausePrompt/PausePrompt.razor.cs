using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A prompt for the user to select how long to pause execution for
/// </summary>
public partial class PausePrompt : IModal
{
    private string lblOk, lblCancel, lblAbortFiles;
    private string Message, Title;

    private Dictionary<int, string> Durations = new()
    {
        { int.MaxValue, "Indefinite" },
        { 5, "5 minutes" },
        { 30, "30 minutes" },
        { 60, "1 hour" },
        { 120, "2 hours" },
        { 180, "3 hours" },
        { 240, "4 hours" },
        { 300, "5 hours" },
        { 360, "6 hours" },
        { 720, "12 hours" },
    };
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    
    private int Value { get; set; }

    private string Uid = System.Guid.NewGuid().ToString();

    private bool Focus;
    private bool AbortFiles;
    private bool ProcessingFiles;

    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblOk = Translater.Instant("Labels.Ok");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.Title = Translater.Instant("Dialogs.PauseDialog.Title");
        this.Message = Translater.Instant("Dialogs.PauseDialog.Message");
        this.lblAbortFiles = Translater.Instant("Dialogs.PauseDialog.AbortFiles");
        ProcessingFiles = feService.Files.Processing.Count > 0;
        Value = int.MaxValue;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Focus)
        {
            Focus = false;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        }
    }


    /// <summary>
    /// Accepts the pause
    /// </summary>
    private async void Accept()
    {
        TaskCompletionSource.TrySetResult(new PauseResult(Value, ProcessingFiles && AbortFiles));
        await Task.CompletedTask;
    }
}

public record PauseResult(int Duration, bool Abort);
