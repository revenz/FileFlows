using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog for choosing a script language
/// </summary>
public partial class ScriptLanguagePicker :  IModal
{
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
    private string lblNext, lblCancel, lblJavaScriptDescription, lblBatchDescription, lblCSharpDescription, 
        lblPowerShellDescription, lblShellDescription;
    private string Title;

    /// <summary>
    /// Gets or sets the language
    /// </summary>
    private ScriptLanguage Language { get; set; } = ScriptLanguage.JavaScript;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblNext = Translater.Instant("Labels.Next");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblJavaScriptDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.JavaScriptDescription");
        lblBatchDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.BatchDescription");
        lblCSharpDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.CSharpDescription");
        lblPowerShellDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.PowerShellDescription");
        lblShellDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.ShellDescription");
        Title = Translater.Instant("Dialogs.ScriptLanguage.Title");
    }

    /// <summary>
    /// Language is choosen
    /// </summary>
    private async void Next()
    {
        TaskCompletionSource.TrySetResult(Language);
        await Task.CompletedTask;
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
    
    private void SetLanguage(ScriptLanguage language, bool close = false)
    {
        Language = language;
        if(close)
            Next();
    }
}