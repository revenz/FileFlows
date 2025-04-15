using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A file browser that lets a user picks a file or folder
/// </summary>
public partial class FileBrowser : IModal
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
    
    private string lblSelect, lblCancel;
    private string Title;

    private bool DirectoryMode = false;
    private string[] Extensions = new string[] { };
    private bool ShowHidden = false;
    private FileBrowserItem Selected;
    List<FileBrowserItem> Items = new List<FileBrowserItem>();
    
    /// <summary>
    /// The API url to call
    /// </summary>
    private const string API_URL = "/api/file-browser";
    /// <summary>
    /// The label for show hidden
    /// </summary>
    private string lblShowHidden;
    /// <summary>
    /// If the server is windows or not
    /// </summary>
    private bool IsWindows = false;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (Options is FileBrowserOptions fileBrowserOptions == false)
        {
            Cancel();
            return;
        }
        this.Title = Translater.TranslateIfNeeded("Dialogs.FileBrowser.FileTitle");
        
         _ = LoadPath(fileBrowserOptions.Start ?? string.Empty);

        Extensions = fileBrowserOptions.Extensions;
        DirectoryMode = fileBrowserOptions.Directory;
        
        //IsWindows = (await ProfileService.Get()).ServerOS == OperatingSystemType.Windows;
        this.lblSelect = Translater.Instant("Labels.Select");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        lblShowHidden = Translater.Instant("Labels.ShowHidden");
        await Task.CompletedTask;
    }


    private void Select()
    {
        if (Selected == null)
            return;
        TaskCompletionSource.TrySetResult(Selected.IsParent ? Selected.Name : Selected.FullName);
    }

    private async Task SetSelected(FileBrowserItem item)
    {
        if (DirectoryMode == false && (item.IsPath || item.IsDrive || item.IsParent))
            return;
        if (this.Selected == item)
            this.Selected = null;
        else
            this.Selected = item;
        await Task.CompletedTask;
    }

    private async Task DblClick(FileBrowserItem item)
    {
        if (item.IsParent || item.IsPath || item.IsDrive)
            await LoadPath(item.FullName);
        else
        {
            this.Selected = item;
            this.Select();
        }
    }

    private async Task LoadPath(string path)
    {
        var result = await GetPathData(path);
        if (result.Success)
        {
            this.Items = result.Data;
            var parent = this.Items.Where(x => x.IsParent).FirstOrDefault();
            if (parent != null)
                this.Title = parent.Name;
            else
                this.Title = "Root";
            this.StateHasChanged();
        }
    }

    private async Task<RequestResult<List<FileBrowserItem>>> GetPathData(string path)
    {
        return await HttpHelper.Get<List<FileBrowserItem>>($"{API_URL}?includeFiles={DirectoryMode == false}" +
        $"&start={Uri.EscapeDataString(path)}" +
        string.Join("", Extensions?.Select(x => "&extensions=" + Uri.EscapeDataString(x))?.ToArray() ?? new string[] { }));
    }
}

/// <summary>
/// The options for the file browser
/// </summary>
public class FileBrowserOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets an optional starting path
    /// </summary>
    public string Start { get; set; }
    
    /// <summary>
    /// Gets or sets if asking for a folder
    /// </summary>
    public bool Directory { get; set; }

    /// <summary>
    /// Gets or sets the allowed extensions
    /// </summary>
    public string[] Extensions { get; set; } = [];
}