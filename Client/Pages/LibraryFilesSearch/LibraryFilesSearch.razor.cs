using BlazorDateRangePicker;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Editors;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// A search page for library files
/// </summary>
public partial class LibraryFilesSearch : LibraryFilePageBase
{
    [Inject] private INavigationService NavigationService { get; set; }
    public override string ApiUrl => "/api/library-file";
    private string lblSearch, lblSearching, lblClose, lblAnyTime, lblLimit, lblPeriod, lblStatus, lblLibrary, lblPath;
    private bool Searched = false;
    SearchPane SearchPane { get; set; }
    

    private List<ListOption> StatusOptions, LibraryOptions;

    private FileStatus? SearchedStatus = null;
    
    /// <inheritdoc />
    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty,
        FromDate = DateTime.MinValue,
        ToDate = DateTime.MaxValue,
        Limit = 1000,
        Library = Guid.Empty
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Layout.SetInfo(Translater.Instant("Pages.LibraryFiles.Title"), "fas fa-file", noPadding: true);
        
        await base.OnInitializedAsync();
        
        lblAnyTime = Translater.Instant("Labels.DateRanges.AnyTime");
        lblLimit = Translater.Instant("Labels.Limit");
        lblPeriod = Translater.Instant("Labels.Period");
        lblStatus = Translater.Instant("Labels.Status");
        lblPath = Translater.Instant("Labels.Path");
        lblLibrary = Translater.Instant("Pages.Library.Title");
        
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblClose = Translater.Instant("Labels.Close");
        this.lblSearching = Translater.Instant("Labels.Searching");
        StatusOptions = new List<ListOption>()
        {
            new() { Value = null, Label = Translater.Instant("Enums.FileStatus.Any") }
        }.Union(new List<ListOption>()
        {
            new() { Value = FileStatus.Unprocessed, Label = Translater.Instant("Enums.FileStatus.Unprocessed") },
            new() { Value = FileStatus.Processing, Label = Translater.Instant("Enums.FileStatus.Processing") },
            new() { Value = FileStatus.Processed, Label = Translater.Instant("Enums.FileStatus.Processed") },
            new() { Value = FileStatus.ProcessingFailed, Label = Translater.Instant("Enums.FileStatus.ProcessingFailed") },
            new() { Value = FileStatus.Disabled, Label = Translater.Instant("Enums.FileStatus.Disabled") },
            new() { Value = FileStatus.OnHold, Label = Translater.Instant("Enums.FileStatus.OnHold") },
            new() { Value = FileStatus.OutOfSchedule, Label = Translater.Instant("Enums.FileStatus.OutOfSchedule") },
        }) //.OrderBy(x => x.Label!.ToLowerInvariant()))
        .ToList();

        LibraryOptions = [
            new () { Label = Translater.Instant("Labels.Any"), Value = Guid.Empty }
        ];
        var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/library-file/find-all-libraries");
        if (librariesResult.Success && librariesResult.Data.Count > 0)
            LibraryOptions.AddRange(librariesResult.Data.Select(x => new ListOption()
            {
                Value = x.Key,
                Label = x.Value
            }).OrderBy(x => x.Label));

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('lfs-path').focus()");
    }

    async Task Search()
    {
        this.Searched = true;
        SearchedStatus = SearchModel.Status;
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
        SearchModel.FromDate = range.Start.DateTime.ToUniversalTime();
        SearchModel.ToDate = range.End.DateTime.ToUniversalTime();
    }

    public override Task Load(Guid selectedUid, bool showBlocker = true)
    {
        if (Searched == false)
            return Task.CompletedTask;
        return base.Load(selectedUid, showBlocker: showBlocker);
    }

    protected override Task<RequestResult<List<LibraryFileMinimal>>> FetchData()
    {
        return HttpHelper.Post<List<LibraryFileMinimal>>($"{ApiUrl}/search", SearchModel);
    }
}