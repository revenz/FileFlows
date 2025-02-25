using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

public class FlowWizardBase : InputRegister, IModal
{
    protected Editor _Editor;
    protected WizardOutput Output;
    
    public static string[] Extensions_Audio = new[]
        {
            "aac", "ac3", "aif", "aiff", "flac", "m4a",
            "mp3", "ogg", "opus", "wav", "wma"
        }
        .OrderBy(x => x)
        .ToArray();
    public static string[] Extensions_Comic = new[] { "cbz", "cbr", "zip", "pdf" }
        .OrderBy(x => x)
        .ToArray();
    public static string[] Extensions_Image =  new[]
            { "jpeg", "jpe", "jpg", "png", "bmp", "gif", "heic", "tiff", "tif", "webp" }
        .OrderBy(x => x)
        .ToArray();
    public static string[] Extensions_Video = new[]
        {
            "3gp", "asf", "avi", "divx", "flv", "m2ts", "m4v", "mkv", "mov",
            "mp4", "mpeg", "mpg", "mts", "ogv", "ts", "vob", "webm", "wmv"
        }
        .OrderBy(x => x)
        .ToArray();
        
    
    /// <summary>
    /// The new flow name
    /// </summary>
    protected string FlowName { get; set; } = string.Empty;
    /// <summary>
    /// The new description
    /// </summary>
    protected string Description { get; set; } = string.Empty;
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor
    {
        get => _Editor;
        set
        {
            if (_Editor != value && value != null)
            {
                _Editor = value;
                StateHasChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
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