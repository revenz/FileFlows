using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Confirm dialog that prompts the user for confirmation 
/// </summary>
public partial class UsedByDialog : IModal
{
    private string lblTitle, lblClose, lblName, lblType;
    private List<ObjectReference> UsedBy;
    
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }


    protected override void OnInitialized()
    {
        if (Options is UsedByDialogOptions options == false)
        {
            Close();
            return;
            
        }
        this.lblClose = Translater.Instant("Labels.Close");
        this.lblTitle = Translater.TranslateIfNeeded("Labels.UsedBy");
        this.lblName = Translater.TranslateIfNeeded("Labels.Name");
        this.lblType = Translater.TranslateIfNeeded("Labels.Type");
        UsedBy = options.UsedBy;
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

    private string GetTypeName(string type)
    {
        type = type.Substring(type.LastIndexOf(".") + 1);
        return Translater.Instant($"Pages.{type}.Title");
    }
}

/// <summary>
/// Options for the used by dialog
/// </summary>
public class UsedByDialogOptions : IModalOptions 
{
    /// <summary>
    /// Gets or sets the items that are using a specific object
    /// </summary>
    public List<ObjectReference> UsedBy { get; set; } = new();
}