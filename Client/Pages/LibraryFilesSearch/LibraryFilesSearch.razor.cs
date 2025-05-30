using BlazorDateRangePicker;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Editors;
using FileFlows.Client.Shared;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// A search page for library files
/// </summary>
public partial class LibraryFilesSearch : ListPage<Guid, LibraryFile>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    [Inject] private INavigationService NavigationService { get; set; }
    public override string ApiUrl => "/api/library-file";
    private string lblSearch, lblSearching, lblClose;
    private string NameMinWidth = "20ch";
    private bool Searched = false;
    [Inject] private IJSRuntime jsRuntime { get; set; }
    SearchPane SearchPane { get; set; }

    private List<ListOption> StatusOptions;
    
    /// <inheritdoc />
    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty,
        FromDate = DateTime.MinValue,
        ToDate = DateTime.MaxValue,
        Limit = 1000,
        LibraryName = string.Empty
    };
    
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.LibraryFiles.Title"), "fas fa-file", noPadding: true);
        
        base.OnInitialized();
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblClose = Translater.Instant("Labels.Close");
        this.lblSearching = Translater.Instant("Labels.Searching");
        StatusOptions = new List<ListOption>()
        {
            new() { Value = null, Label = Translater.Instant("Enums.FileStatus.Any") }
        }.Union(new List<ListOption>()
        {
            new() { Value = FileStatus.Processed, Label = Translater.Instant("Enums.FileStatus.Processed") },
            new() { Value = FileStatus.Processing, Label = Translater.Instant("Enums.FileStatus.Processing") },
            new() { Value = FileStatus.Unprocessed, Label = Translater.Instant("Enums.FileStatus.Unprocessed") },
            new()
            {
                Value = FileStatus.ProcessingFailed, Label = Translater.Instant("Enums.FileStatus.ProcessingFailed")
            }
        }.OrderBy(x => x.Label!.ToLowerInvariant())).ToList();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('lfs-path').focus()");
    }

    public override async Task<bool> Edit(LibraryFile item)
    {
        await ModalService.ShowModal<FileViewer>(new ModalEditorOptions()
        {
            Uid = item.Uid
        });
        return false;
    }
    async Task Search()
    {
        this.Searched = true;
        Blocker.Show(lblSearching);
        await Refresh();
        Blocker.Hide();
    }

    async Task Close()
    {
        await NavigationService.NavigateTo("/library-files");
    }

    public void OnRangeSelect(DateRange range)
    {
        SearchModel.FromDate = range.Start.Date;
        SearchModel.ToDate = range.End.Date;
    }

    public override Task Load(Guid selectedUid, bool showBlocker = true)
    {
        if (Searched == false)
            return Task.CompletedTask;
        return base.Load(selectedUid, showBlocker: showBlocker);
    }

    protected override Task<RequestResult<List<LibraryFile>>> FetchData()
    {
        return HttpHelper.Post<List<LibraryFile>>($"{ApiUrl}/search", SearchModel);
    }
}