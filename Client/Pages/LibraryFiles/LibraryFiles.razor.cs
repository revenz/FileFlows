using System.Text.RegularExpressions;
using System.Timers;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Library Files Page
/// </summary>
public partial class LibraryFiles : ListPage<Guid, LibaryFileListModel>, IDisposable
{
    private string filter = string.Empty;
    private FileStatus? filterStatus;
    /// <inheritdoc />
    public override string ApiUrl => "/api/library-file";
    [Inject] private INavigationService NavigationService { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Gets or sets the add file dialog
    /// </summary>
    private AddFileDialog AddDialog { get; set; }
    
    /// <summary>
    /// Gets or sets the reprocess dialog
    /// </summary>
    private ReprocessDialog ReprocessDialog { get; set; }

    /// <summary>
    /// The skybox to select the current file status
    /// </summary>
    private FlowSkyBox<FileStatus> Skybox;

    private FileFlows.Shared.Models.FileStatus SelectedStatus;

    /// <summary>
    /// The current page
    /// </summary>
    private int PageIndex;

    private Guid? SelectedNode, SelectedLibrary, SelectedFlow, SelectedTag;
    private FilesSortBy? SelectedSortBy;

    private string lblMoveToTop = "";

    private int Count;
    private string lblSearch, lblDeleteSwitch, lblForcedProcessing, lblSortBy, lblNode, lblFlow, lblLibrary, lblTag;

    private string TableIdentifier => "LibraryFiles_" + this.SelectedStatus; 

    SearchPane SearchPane { get; set; }
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty
    };

    private string Title;
    private string lblLibraryFiles, lblFileFlowsServer;
    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    private int TotalItems;
    private List<FlowExecutorInfo> WorkerStatus = new ();
    private Timer AutoRefreshTimer;
    private Dictionary<string, NodeInfo> Nodes = new();
    private Dictionary<Guid, string> Libraries = new();
    private Dictionary<Guid, string>  Flows = new();
    private List<DropDownOption> optionsLibraries, optionsNodes, optionsFlows, optionsSortBy, optionsTags;

    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private async Task SetSelected(FlowSkyBoxItem<FileStatus> status)
    {
        SelectedStatus = status.Value;
        this.PageIndex = 0;
        Title = lblLibraryFiles;// + ": " + status.Name;
        await this.Refresh();
        this.StateHasChanged();
    }

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}/list-all?status={(int)SelectedStatus}&page={PageIndex}&pageSize={App.PageSize}" +
                                       $"&filter={Uri.EscapeDataString(filterStatus == SelectedStatus ? filter ?? string.Empty : string.Empty)}" +
                                       (SelectedNode == null ? "" : $"&node={SelectedNode}") + 
                                       (SelectedFlow == null ? "" : $"&flow={SelectedFlow}") + 
                                       (SelectedSortBy == null ? "" : $"&sortBy={(int)SelectedSortBy}") + 
                                       (SelectedLibrary == null ? "" : $"&library={SelectedLibrary}") +
                                       (SelectedTag == null ? "" : $"&tag={System.Web.HttpUtility.UrlEncode(SelectedTag.ToString())}");

    public override async Task PostLoad()
    {
        await jsRuntime.InvokeVoidAsync("ff.scrollTableToTop");
    }

    protected override async Task PostDelete()
    {
        await RefreshStatus();
    }

    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        this.SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;
        lblForcedProcessing = Translater.Instant("Labels.ForceProcessing");
        lblMoveToTop = Translater.Instant("Pages.LibraryFiles.Buttons.MoveToTop");
        lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
        lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        Title = lblLibraryFiles;// + ": " + Translater.Instant("Enums.FileStatus." + FileStatus.Unprocessed);
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblDeleteSwitch = Translater.Instant("Labels.DeleteLibraryFilesPhysicallySwitch");
        lblSortBy = Translater.Instant("Labels.SortBy");
        lblNode = Translater.Instant("Labels.Node");
        lblFlow = Translater.Instant("Labels.Flow");
        lblLibrary = Translater.Instant("Pages.Library.Title");
        lblTag = Translater.Instant("Labels.Tag");

        if (Profile.LicenseLevel != FileFlows.Common.LicenseLevel.Free)
        {
            optionsTags = feService.Dashboard.Tags.Select(x => new DropDownOption()
            {
                Icon = x.Icon?.EmptyAsNull() ?? "fas fa-tag",
                Value = x.Uid,
                Label = x.Name
            }).ToList();
        }
        
        optionsSortBy = new ()
        {
            new () { Icon = "fas fa-sort-numeric-up", Label = Translater.Instant("Enums.FilesSortBy.Size"), Value = FilesSortBy.Size },
            new () { Icon = "fas fa-sort-numeric-down-alt", Label = Translater.Instant("Enums.FilesSortBy.SizeDesc"), Value = FilesSortBy.SizeDesc },
            new () { Icon = "fas fa-sort-amount-down-alt", Label = Translater.Instant("Enums.FilesSortBy.Savings"), Value = FilesSortBy.Savings },
            new () { Icon = "fas fa-sort-amount-up", Label = Translater.Instant("Enums.FilesSortBy.SavingsDesc"), Value = FilesSortBy.SavingsDesc },
            new () { Icon = "fas fa-hourglass-start", Label = Translater.Instant("Enums.FilesSortBy.Time"), Value = FilesSortBy.Time },
            new () { Icon = "fas fa-hourglass-end", Label = Translater.Instant("Enums.FilesSortBy.TimeDesc"), Value = FilesSortBy.TimeDesc }
        };

        var nodesResult = await HttpHelper.Get<List<NodeInfo>>("/api/library-file/node-list");
        if (nodesResult.Success)
        {
            Nodes = nodesResult.Data.DistinctBy(x => x.Name.ToLowerInvariant())
                .ToDictionary(x => x.Name.ToLowerInvariant(), x => x);
            optionsNodes = nodesResult.Data.OrderBy(x => x.Name.ToLowerInvariant()).Select(x => new DropDownOption()
            {
                Icon = x.OperatingSystem switch
                {
                    OperatingSystemType.Docker => "/icons/docker.svg",
                    OperatingSystemType.Linux => "/icons/linux.svg",
                    OperatingSystemType.Mac => "/icons/apple.svg",
                    OperatingSystemType.Windows => "/icons/windows.svg",
                    _ => ""
                },
                Value = x.Uid,
                Label = x.Name == "FileFlowsServer" ? "Internal Node" : x.Name
            }).ToList();
        }
        
        var libraryResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/library/basic-list");
        if (libraryResult.Success)
        {
            Libraries = libraryResult.Data;
            optionsLibraries = libraryResult.Data.OrderBy(x => x.Value.ToLowerInvariant()).Select(x => new DropDownOption()
            {
                Icon = "fas fa-folder",
                Value = x.Key,
                Label = x.Value
            }).ToList();
        }
        var flowResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/flow/basic-list");
        if (flowResult.Success)
        {
            Flows = flowResult.Data;
            optionsFlows = flowResult.Data.OrderBy(x => x.Value.ToLowerInvariant()).Select(x => new DropDownOption()
            {
                Icon = "fas fa-sitemap",
                Value = x.Key,
                Label = x.Value
            }).ToList();
        }

        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed!;
        AutoRefreshTimer.Interval = 10_000;
        AutoRefreshTimer.AutoReset = false;
        AutoRefreshTimer.Start();
    }

    /// <summary>
    /// Auto refresh timer elapsed
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event args</param>
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if(SelectedStatus == FileStatus.Processing)
            _ = Refresh(false);
    }
        

    /// <summary>
    /// Refreshes the page
    /// </summary>
    /// <param name="showBlocker">if the blocker should be shown or not</param>
    public override async Task Refresh(bool showBlocker = true)
    {
        AutoRefreshTimer?.Stop();
        try
        {
            await base.Refresh(showBlocker);
        }
        finally
        {
            AutoRefreshTimer?.Start();
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed!;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }

    private Task<RequestResult<List<LibraryStatus>>> GetStatus() => HttpHelper.Get<List<LibraryStatus>>(ApiUrl + "/status");

    /// <summary>
    /// Refreshes the top status bar
    /// This is needed when deleting items, as the list will not be refreshed, just items removed from it
    /// </summary>
    /// <returns></returns>
    private async Task RefreshStatus()
    {
        var result = await GetStatus();
        if (result.Success)
            RefreshStatus(result.Data.ToList());
    }
    
    private void RefreshStatus(List<LibraryStatus> data)
    {
       var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
       foreach (var s in order)
       {
           if (data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
               data.Add(new LibraryStatus { Status = s });
       }

       foreach (var s in data)
           s.Name = Translater.Instant("Enums.FileStatus." + s.Status.ToString());

       var sbItems = new List<FlowSkyBoxItem<FileStatus>>();
       foreach (var status in data.OrderBy(x =>
                {
                    int index = order.IndexOf(x.Status);
                    return index >= 0 ? index : 100;
                }))
       {
           string icon = status.Status switch
           {
               FileStatus.Unprocessed => "far fa-hourglass",
               FileStatus.Disabled => "fas fa-toggle-off",
               FileStatus.Processed => "far fa-check-circle",
               FileStatus.Processing => "fas fa-file-medical-alt",
               FileStatus.FlowNotFound => "fas fa-exclamation",
               FileStatus.ProcessingFailed => "far fa-times-circle",
               FileStatus.OutOfSchedule => "far fa-calendar-times",
               FileStatus.Duplicate => "far fa-copy",
               FileStatus.MappingIssue => "fas fa-map-marked-alt",
               FileStatus.MissingLibrary => "fas fa-trash",
               FileStatus.OnHold => "fas fa-hand-paper",
               FileStatus.ReprocessByFlow => "fas fa-redo",
               _ => ""
           };
           if (status.Status != FileStatus.Unprocessed && status.Status != FileStatus.Processing && status.Status != FileStatus.Processed && status.Count == 0)
               continue;
           sbItems.Add(new ()
           {
               Count = status.Count,
               Icon = icon,
               Name = status.Name,
               Value = status.Status
           });
        }

        Skybox.SetItems(sbItems, SelectedStatus);
        this.Count = sbItems.Where(x => x.Value == SelectedStatus).Select(x => x.Count).FirstOrDefault();
        this.StateHasChanged();
    }

    // /// <summary>
    // /// Refreshes the worker status
    // /// </summary>
    // private async Task RefreshWorkerStatus()
    // {
    //     this.WorkerStatus = await ClientService.GetExecutorInfo();
    //     if(this.SelectedStatus == FileStatus.Processing)
    //         this.StateHasChanged();
    // }

    public override async Task<bool> Edit(LibaryFileListModel item)
    {
        await Helpers.LibraryFileEditor.Open(Blocker, Editor, item.Uid, Profile, feService);
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
        if (selected.Length == 0)
            return; // nothing to cancel

        if (await Confirm.Show("Labels.Cancel",
            Translater.Instant("Labels.CancelItems", new { count = selected.Length })) == false)
            return; // rejected the confirmation

        Blocker.Show();
        this.StateHasChanged();
        try
        {
            foreach (var item in selected)
                await HttpHelper.Delete($"/api/worker/by-file/{item.Uid}");

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
    public async Task Reprocess()
    {
        var selected = Table.GetSelected().ToList();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to reprocess

        bool processOptions = SelectedStatus is FileStatus.Unprocessed or FileStatus.OnHold or FileStatus.Duplicate;

        var nodes = Nodes.ToDictionary(x => x.Value.Uid, x => x.Value.Name);
        var result = await ReprocessDialog.Show(Flows, nodes, selected, processOptions);
        if(result)
            await Refresh();
    }

    /// <inheritdoc />
    protected override async Task<RequestResult<List<LibaryFileListModel>>> FetchData()
    {
        var request = await HttpHelper.Get<LibraryFileDatalistModel>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<LibaryFileListModel>>
            {
                Body = request.Body,
                Success = request.Success
            };
        }

        // await RefreshWorkerStatus();
        // RefreshStatus(request.Data?.Status?.ToList() ?? new List<LibraryStatus>());

        if (request.Headers.ContainsKey("x-total-items") &&
            int.TryParse(request.Headers["x-total-items"], out int totalItems))
        {
            Logger.Instance.ILog("### Total items from header: " + totalItems);
            this.TotalItems = totalItems;
        }
        else
        {
            var status = Skybox.SelectedItem;
            this.TotalItems = status?.Count ?? 0;
        }
        
        var result = new RequestResult<List<LibaryFileListModel>>
        {
            Body = request.Body,
            Success = request.Success,
            Data = request.Data.LibraryFiles.ToList()
        };
        Logger.Instance.ILog("FetchData: " + result.Data.Count);
        return result;
    }

    /// <summary>
    /// Changes to a specific page
    /// </summary>
    /// <param name="index">the page to change to</param>
    private async Task PageChange(int index)
    {
        PageIndex = index;
        await this.Refresh();
    }

    /// <summary>
    /// Updates the number of items shown on a page
    /// </summary>
    /// <param name="size">the number of items</param>
    private async Task PageSizeChange(int size)
    {
        this.PageIndex = 0;
        await this.Refresh();
    }

    private async Task OnFilter(FilterEventArgs args)
    {
        if (this.filter?.EmptyAsNull() == args.Text?.EmptyAsNull())
        {
            this.filter = string.Empty;
            this.filterStatus = null;
            return;
        }

        int totalItems = Skybox.SelectedItem.Count;
        if (totalItems <= args.PageSize)
            return;
        this.filterStatus = this.SelectedStatus;
        // need to filter on the server side
        args.Handled = true;
        args.PageIndex = 0;
        this.PageIndex = 0;
        this.filter = args.Text;
        await this.Refresh();
        this.filter = args.Text; // ensures refresh didnt change the filter
    }

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected override void SetTableData(List<LibaryFileListModel> data)
    {
        if(string.IsNullOrWhiteSpace(this.filter) || SelectedStatus  != filterStatus)
            Table.SetData(data);
        else
            Table.SetData(data, filter: this.filter); 
    }

    private async Task Rescan()
    {
        this.Blocker.Show("Scanning Libraries");
        try
        {
            await HttpHelper.Post("/api/library/rescan-enabled");
            await Refresh();
            Toast.ShowSuccess(Translater.Instant("Pages.LibraryFiles.Labels.ScanTriggered"));
        }
        finally
        {
            this.Blocker.Hide();   
        }
    }

    private async Task Unhold()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to unhold

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/unhold", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    private async Task ToggleForce()
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
    
    Task Search() => NavigationService.NavigateTo("/library-files/search");


    async Task DeleteFile()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var msg = Translater.Instant("Labels.DeleteLibraryFilesPhysicallyMessage", new { count = uids.Length });
        if ((await Confirm.Show("Labels.Delete", msg, switchMessage: lblDeleteSwitch, switchState: false, requireSwitch:true)).Confirmed == false)
            return; // rejected the confirm
        
        
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete("/api/library-file/delete-files", new ReferenceModel<Guid> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
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

    async Task DownloadFile()
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
            Toast.ShowError(apiResult.Body?.EmptyAsNull() ?? apiResult.Data?.EmptyAsNull() ?? "Failed to download.");
            return;
        }
        
        string name = file.Name.Replace("\\", "/");
        name = name[(name.LastIndexOf('/') + 1)..];
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", url, name);
    }

    async Task SetStatus(FileStatus status)
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
                    Toast.ShowError( Translater.Instant(apiResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.SetFileStatus"));
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
    /// Gets the icon to show for the node
    /// </summary>
    /// <param name="node">the node</param>
    /// <returns>the node icon</returns>
    private string GetNodeIcon(string node)
    {
        if(Nodes.TryGetValue(node.ToLowerInvariant(), out var n) == false)
            return "fas fa-desktop";
        if (n.OperatingSystem == OperatingSystemType.Docker)
            return "fab fa-docker";
        if (n.OperatingSystem == OperatingSystemType.Windows)
            return "fab fa-windows";
        if (n.OperatingSystem == OperatingSystemType.Mac)
            return "fab fa-apple";
        if (n.OperatingSystem == OperatingSystemType.Linux)
            return "fab fa-linux";
        return "fas fa-desktop";
    }

    /// <summary>
    /// Sets the selected node
    /// </summary>
    /// <param name="node">the node</param>
    private void SelectNode(object? node)
    {
        if (node is string str)
        {
            var n = Nodes?.Values?.FirstOrDefault(x => x.Name?.ToLowerInvariant() == str.ToLowerInvariant());
            if (n == null)
                return;
            SelectedNode = n.Uid;
        }
        else
            SelectedNode = node as Guid?;
        _ = Refresh();
    }

    /// <summary>
    /// Sets the sort by
    /// </summary>
    /// <param name="sortBy">the sort by</param>
    private void SelectSortBy(object? sortBy)
    {
        SelectedSortBy = sortBy as FilesSortBy?;
        _ = Refresh();
    }
    /// <summary>
    /// Sets the selected library
    /// </summary>
    /// <param name="library">the library</param>
    private void SelectLibrary(object? library)
    {
        if (library is string str)
        {
            var l = Libraries?.FirstOrDefault(x => (x.Value?.ToLowerInvariant()).Equals(str, StringComparison.InvariantCultureIgnoreCase));
            if (l == null)
                return;
            SelectedLibrary = l.Value.Key;
        }
        else
            SelectedLibrary = library as Guid?;
        _ = Refresh();
    }

    private void SelectTag(object? tag)
    {
        if (tag is string str)
        {
            var t = optionsTags?.FirstOrDefault(x => (x.Label?.ToLowerInvariant()).Equals(str, StringComparison.InvariantCultureIgnoreCase));
            if (t == null)
                return;
            SelectedTag = t.Value as Guid?;
        }
        else
            SelectedTag = tag as Guid?;
        _ = Refresh();
    }

    /// <summary>
    /// Sets the selected flow
    /// </summary>
    /// <param name="flow">the flow</param>
    private void SelectFlow(object? flow)
    {
        if (flow is string str)
        {
            var f = Flows?.FirstOrDefault(x => x.Value?.ToLowerInvariant() == str.ToLowerInvariant());
            if (f == null)
                return;
            SelectedFlow = f.Value.Key;
        }
        else
            SelectedFlow = flow as Guid?;
        _ = Refresh();
    }

    /// <summary>
    /// THe manual add button was clicked
    /// </summary>
    private async Task Add()
    {
        var nodes = Nodes.ToDictionary(x => x.Value.Uid, x => x.Value.Name);
        var result = await AddDialog.Show(Blocker, Flows, nodes);
        // if (result.Files?.Any() != true)
        //     return;
        // Blocker.Show();
        // try
        // {
        //     await HttpHelper.Post(ApiUrl + $"/manually-add", result);
        // }
        // finally
        // {
        //     Blocker.Hide();
        // }
        if(result)
            await Refresh();
    }
}