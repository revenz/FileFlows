using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

public partial class ImportScript : IModal
{
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

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
    
    private string lblImport, lblCancel;
    private string Title;
    private List<ListOption> AvailableScript;

    private List<object> CheckedItems = new();

    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblImport = Translater.Instant("Labels.Import");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.Title = Translater.Instant("Dialogs.ImportScript.Title");

        if (Options is ImportScriptOptions isOptions == false || isOptions.AvailableScripts == null ||
            isOptions.AvailableScripts.Count == 0)
        {
            Close();
            return;
        }

        AvailableScript = isOptions.AvailableScripts.OrderBy(x => x.ToLowerInvariant()).Select(x => new ListOption()
        {
            Label = x,
            Value = x
        }).ToList();
    }


    private void OnChange(ChangeEventArgs args, ListOption opt)
    {
        bool @checked = args.Value as bool? == true;
        if (@checked && this.CheckedItems.Contains(opt.Value) == false)
            this.CheckedItems.Add(opt.Value);
        else if (@checked == false && this.CheckedItems.Contains(opt.Value))
            this.CheckedItems.Remove(opt.Value);
    }

    private async void Accept()
    {
        TaskCompletionSource.TrySetResult(CheckedItems.Select(x => x.ToString()!).ToArray());
        await Task.CompletedTask;
    }
}

/// <summary>
/// Options for the File Browser
/// </summary>
public class ImportScriptOptions : IModalOptions
{
    /// <summary>
    /// Gets the available scripts
    /// </summary>
    public List<string> AvailableScripts { get; init; }
}