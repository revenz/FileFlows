using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Editors;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Base page for library file pages
/// </summary>
public abstract class LibraryFilePageBase : ListPage<Guid, LibraryFileMinimal>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] protected IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }

    protected string lblDeleteSwitch;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.lblDeleteSwitch = Translater.Instant("Labels.DeleteLibraryFilesPhysicallySwitch");
        
    }

    public override async Task<bool> Edit(LibraryFileMinimal item)
    {
        await ModalService.ShowModal<FileViewer>(new ModalEditorOptions()
        {
            Uid = item.Uid
        });
        return false;
    }

    public async Task MoveToTop()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/move-to-top", new ReferenceModel<Guid> { Uids = uids });                
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    public async Task Cancel()
    {
        var selected = Table.GetSelected().ToArray();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        if (await Message.Confirm("Labels.Cancel",
            Translater.Instant("Labels.CancelItems", new { count = uids.Length })) == false)
            return; // rejected the confirmation

        Blocker.Show();
        this.StateHasChanged();
        try
        {
            await HttpHelper.Delete($"/api/library-file/abort",  new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        await Refresh();
    }


    public async Task ForceProcessing()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to reprocess

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/force-processing", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    /// <summary>
    /// Reprocess the selected files
    /// </summary>
    /// <param name="selectedStatus">the current status</param>
    public async Task Reprocess(FileStatus selectedStatus)
    {
        var selected = Table.GetSelected().ToList();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? [];
        if (uids.Length == 0)
            return; // nothing to reprocess
        bool processOptions = selectedStatus is FileStatus.Unprocessed or FileStatus.OnHold;

        List<LibraryFile> files = [];
        Blocker.Show();
        try
        {
            var result = await HttpHelper.Post<List<LibraryFile>>(ApiUrl + "/get-files",  new ReferenceModel<Guid> { Uids = uids });
            if(result.Success == false)
                return;
            files = result.Data ?? [];
            if (files.Count == 0)
                return;
        }
        finally
        {
            Blocker.Hide();
        }

        await ModalService.ShowModal<ReprocessDialog>(new ReprocessOptions()
        {
            Files = files,
            ProcessOptionsMode = processOptions
        });
    }

    protected async Task Rescan()
    {
        this.Blocker.Show("Scanning Libraries");
        try
        {
            var result = await HttpHelper.Post("/api/library/rescan-enabled");
            if(result.Success == false)
                feService.Notifications.ShowWarning(Translater.TranslateIfNeeded(result.Body));
        }
        finally
        {
            this.Blocker.Hide();   
        }
    }

    protected async Task Unhold()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to unhold

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/unhold", new ReferenceModel<Guid> { Uids = uids });
            await Task.Delay(500);
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    protected async Task ToggleForce()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing 

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/toggle-force", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }

    protected async Task DeleteFile()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var msg = Translater.Instant("Labels.DeleteLibraryFilesPhysicallyMessage", new { count = uids.Length });
        if ((await Message.Confirm("Labels.Delete", msg, switchMessage: lblDeleteSwitch, switchState: false, requireSwitch:true)).Confirmed == false)
            return; // rejected the confirm
        
        
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete("/api/library-file/delete-files", new ReferenceModel<Guid> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    feService.Notifications.ShowError( Translater.Instant(deleteResult.Body));
                else
                    feService.Notifications.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }
            
            this.Data = this.Data.Where(x => uids.Contains(x.Uid) == false).ToList();

            await PostDelete();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    protected async Task DownloadFile()
    {
        var file = Table.GetSelected()?.FirstOrDefault();
        if (file == null)
            return; // nothing to delete
        
        string url = "/api/library-file/download/" + file.Uid;
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        
        var apiResult = await HttpHelper.Get<string>($"{url}?test=true");
        if (apiResult.Success == false)
        {
            feService.Notifications.ShowError(apiResult.Body?.EmptyAsNull() ?? apiResult.Data?.EmptyAsNull() ?? "Failed to download.");
            return;
        }
        
        string name = file.DisplayName.Replace("\\", "/");
        name = name[(name.LastIndexOf('/') + 1)..];
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", url, name);
    }

    protected async Task SetStatus(FileStatus status)
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to mark
        
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var apiResult = await HttpHelper.Post($"/api/library-file/set-status/{status}", new ReferenceModel<Guid> { Uids = uids });
            if (apiResult.Success == false)
            {
                if(Translater.NeedsTranslating(apiResult.Body))
                    feService.Notifications.ShowError( Translater.Instant(apiResult.Body));
                else
                    feService.Notifications.ShowError( Translater.Instant("ErrorMessages.SetFileStatus"));
                return;
            }
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
    
    /// <summary>
    /// THe manual add button was clicked
    /// </summary>
    protected async Task Add()
        => await ModalService.ShowModal<AddFileDialog>(new ModalEditorOptions());
}