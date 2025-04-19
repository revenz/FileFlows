using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FileFlows.Shared;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;

namespace FileFlows.Client.Components.Dialogs;

public partial class ImportDialog : IModal
{
    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }
    
    private string lblImport, lblCancel, lblBrowse;
    private string Message, Title;

    private string FileName { get; set; }
    private bool HasFile { get; set; }
    private string Value { get; set; }

    private string[] Extensions = new[] { "json" };
    private string AcceptedTypes;

    private string Uid = System.Guid.NewGuid().ToString();


    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblImport = Translater.Instant("Labels.Import");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.lblBrowse = Translater.Instant("Labels.Browse");
        this.Title = Translater.Instant("Dialogs.Import.Title");
        this.Message = Translater.Instant("Dialogs.Import.Message");
        
        if(Options is ImportDialogOptions options)
            Extensions = options.Extensions;
        Extensions = Extensions?.Any() == true ? Extensions : new[] { "json" };
        AcceptedTypes = string.Join(", ", Extensions.Select(x => "." + x));
    }


    private void Accept()
    {
        TaskCompletionSource.TrySetResult(new ImportDialogResult(FileName, Value)); // Set result when closing
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
    
    private async Task LoadFile(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0)
        {
            FileName = string.Empty;
            Value = string.Empty;
            HasFile = false;
            return;
        }
        FileName = e.File.Name;
        using var reader = new StreamReader(e.File.OpenReadStream());
        this.Value = await reader.ReadToEndAsync();
        this.HasFile = string.IsNullOrWhiteSpace(this.Value) == false;
        this.StateHasChanged();
    }
}

/// <summary>
/// Import dialog options
/// </summary>
public class ImportDialogOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the extension to show
    /// </summary>
    public string[] Extensions { get; set; }
}

/// <summary>
/// Import dialog result
/// </summary>
/// <param name="FileName">the filename</param>
/// <param name="Content">the content of the file</param>
public record ImportDialogResult(string FileName, string Content);