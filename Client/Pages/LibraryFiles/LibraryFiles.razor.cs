using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Services.Frontend.Handlers;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlows.Client.Pages;

/// <summary>
/// Library Files Page
/// </summary>
public partial class LibraryFiles : ListPage<Guid, LibraryFileMinimal>, IDisposable
{
    private string filter = string.Empty;
    private FileStatus? filterStatus;
    /// <inheritdoc />
    public override string ApiUrl => "/api/library-file";
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation service
    /// </summary>
    [Inject] private INavigationService NavigationService { get; set; }
    /// <summary>
    /// Gets or sets the local storage
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] private NavigationManager Navigation { get; set; }
    
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

    private FileFlows.Shared.Models.FileStatus SelectedStatus;

    /// <summary>
    /// The current page
    /// </summary>
    private int PageIndex;

    private Guid? SelectedNode, SelectedLibrary, SelectedFlow, SelectedTag;
    private FilesSortBy? SelectedSortBy;

    private string lblSearch, lblDeleteSwitch, lblForcedProcessing, lblReprocessByFlow, lblSortBy, lblNode, lblFlow, lblLibrary, lblTag;

    private string TableIdentifier => "LibraryFiles_" + this.SelectedStatus; 

    private string lblLibraryFiles, lblFileFlowsServer;
    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    private int TotalItems;
    private Dictionary<string, NodeInfo> Nodes = new();
    private Dictionary<Guid, string> Libraries = new();
    private Dictionary<Guid, string>  Flows = new();
    private List<DropDownOption> optionsLibraries, optionsNodes, optionsFlows, optionsSortBy, optionsTags;

    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private void OnStatusChanged(FileStatus status)
    {
        SelectedStatus = status;
        PageIndex = 0;
        LoadServiceData();
    }

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}/list-all?status={(int)SelectedStatus}&page={PageIndex}" +
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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.LibraryFiles.Title"), "fas fa-file");
            
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");
        // do not call base here! we dont want to load the data via a call
        // we load the initial data from the upcoming files
        
        SelectedStatus = FileStatus.Unprocessed;
        
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        var token = query.Get("status"); // Example: ?status=Processed

        if (string.IsNullOrEmpty(token) == false)
        {
            // Use the token here...
            _ = Enum.TryParse(token, out SelectedStatus);

            // Now remove the query params
            var newUri = uri.GetLeftPart(UriPartial.Path);
            Navigation.NavigateTo(newUri, replace: true);
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        lblForcedProcessing = Translater.Instant("Labels.ForceProcessing");
        lblReprocessByFlow = Translater.Instant("Enums.FileStatus.ReprocessByFlow");
        lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
        lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblDeleteSwitch = Translater.Instant("Labels.DeleteLibraryFilesPhysicallySwitch");
        lblSortBy = Translater.Instant("Labels.SortBy");
        lblNode = Translater.Instant("Labels.Node");
        lblFlow = Translater.Instant("Labels.Flow");
        lblLibrary = Translater.Instant("Pages.Library.Title");
        lblTag = Translater.Instant("Labels.Tag");

        Profile ??= feService.Profile.Profile;

        if (Profile.LicenseLevel != FileFlows.Common.LicenseLevel.Free)
        {
            optionsTags = feService.Tag.Tags.Select(x => new DropDownOption()
            {
                Icon = x.Icon?.EmptyAsNull() ?? "fas fa-tag",
                Value = x.Uid,
                Label = x.Name
            }).ToList();
        }

        optionsSortBy = new()
        {
            new()
            {
                Icon = "fas fa-sort-numeric-up", Label = Translater.Instant("Enums.FilesSortBy.Size"),
                Value = FilesSortBy.Size
            },
            new()
            {
                Icon = "fas fa-sort-numeric-down-alt", Label = Translater.Instant("Enums.FilesSortBy.SizeDesc"),
                Value = FilesSortBy.SizeDesc
            },
            new()
            {
                Icon = "fas fa-sort-amount-down-alt", Label = Translater.Instant("Enums.FilesSortBy.Savings"),
                Value = FilesSortBy.Savings
            },
            new()
            {
                Icon = "fas fa-sort-amount-up", Label = Translater.Instant("Enums.FilesSortBy.SavingsDesc"),
                Value = FilesSortBy.SavingsDesc
            },
            new()
            {
                Icon = "fas fa-hourglass-start", Label = Translater.Instant("Enums.FilesSortBy.Time"),
                Value = FilesSortBy.Time
            },
            new()
            {
                Icon = "fas fa-hourglass-end", Label = Translater.Instant("Enums.FilesSortBy.TimeDesc"),
                Value = FilesSortBy.TimeDesc
            }
        };

        Nodes = feService.Node.NodeStatusSummaries.ToDictionary(x => x.Name.ToLowerInvariant(), x => new NodeInfo()
        {
            Name = x.Name,
            Uid = x.Uid,
            OperatingSystem = x.OperatingSystem,
        });
        optionsNodes = Nodes.Values.OrderBy(x => x.Name.ToLowerInvariant()).Select(x => new DropDownOption()
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

        Libraries = feService.Library.LibraryList;
        optionsLibraries = Libraries.OrderBy(x => x.Value.ToLowerInvariant()).Select(x => new DropDownOption()
        {
            Icon = "fas fa-folder",
            Value = x.Key,
            Label = x.Value
        }).ToList();

        Flows = feService.Flow.FlowList;
        optionsFlows = Flows.OrderBy(x => x.Value.ToLowerInvariant()).Select(x => new DropDownOption()
        {
            Icon = "fas fa-sitemap",
            Value = x.Key,
            Label = x.Value
        }).ToList();

        LoadServiceData();
        
        feService.Files.UnprocessedUpdated += DataUpdated;
        feService.Files.FailedFilesUpdated += DataUpdated2;
        feService.Files.SuccessfulUpdated += DataUpdated2;
        feService.Files.OutOfScheduleUpdated += DataUpdated;
        feService.Files.DisabledUpdated += DataUpdated;
        feService.Files.OnHoldUpdated += DataUpdated;
        feService.Files.ProcessingUpdated += DataUpdated;
    }

    private void DataUpdated(List<ProcessingLibraryFile> obj)
    {
        if (PageIndex > 0)
            return;
        LoadServiceData();
    }

    private void DataUpdated2(FileHandler.ListAndCount<LibraryFileMinimal> obj)
    {
        if (PageIndex > 0)
            return;
        LoadServiceData();
    }

    private void DataUpdated(List<LibraryFileMinimal> obj)
    {
        if (PageIndex > 0)
            return;
        LoadServiceData();
    }

    /// <summary>
    /// Checks if a filter is being used, and if so, the list will be forcible fetched
    /// </summary>
    /// <returns>true if using a filter</returns>
    private bool HasFilter()
        => SelectedFlow != null || SelectedNode != null || SelectedLibrary != null || SelectedTag != null || 
           (SelectedStatus == FileStatus.Processed && SelectedSortBy != null) || PageIndex > 0;

    public override async Task Load(Guid selectedUid, bool showBlocker = true)
    {
        if (HasFilter() == false)
        {
            LoadServiceData();
            return;
        }

        await base.Load(selectedUid, showBlocker);
    }

    void LoadServiceData()
    {
        this.Data = SelectedStatus == FileStatus.Unprocessed ? feService.Files.Unprocessed :
            SelectedStatus == FileStatus.OnHold ? feService.Files.OnHold :
            SelectedStatus == FileStatus.ProcessingFailed ? feService.Files.FailedFiles :
            SelectedStatus == FileStatus.Processed ? feService.Files.Processed :
            SelectedStatus == FileStatus.OutOfSchedule ? feService.Files.OutOfSchedule :
            SelectedStatus == FileStatus.Disabled ? feService.Files.Disabled :
            SelectedStatus == FileStatus.Processing ? feService.Files.Processing.Cast<LibraryFileMinimal>().ToList() : [];
            
        TotalItems =
            SelectedStatus == FileStatus.ProcessingFailed ? feService.Files.FailedFilesTotal :
            SelectedStatus == FileStatus.Processed ? feService.Files.ProcessedTotal :
            this.Data.Count;
            
        // if (Table != null)
        // {
        //     SetTableData(this.Data);
        //     var item = this.Data.FirstOrDefault(x => x.Uid.Equals(selectedUid));
        //     if (item != null)
        //     {
        //         _ = Task.Run(async () =>
        //         {
        //             // need a delay here since setdata and the inner works of FlowTable will clear this without it
        //             await Task.Delay(50);
        //             Table.SelectItem(item);
        //         });
        //     }
        // }

        HasData = this.Data?.Any() == true;
        this.Loaded = true;
        this.StateHasChanged();
    }


    /// <inheritdoc />
    protected override async Task<RequestResult<List<LibraryFileMinimal>>> FetchData()
    {
        var request = await HttpHelper.Get<List<LibraryFileMinimal>>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<LibraryFileMinimal>>
            {
                Body = request.Body,
                Success = request.Success
            };
        }
        if (request.Headers.ContainsKey("x-total-items") &&
            int.TryParse(request.Headers["x-total-items"], out int totalItems))
        {
            Logger.Instance.ILog("### Total items from header: " + totalItems);
            this.TotalItems = totalItems;
        }
        
        var result = new RequestResult<List<LibraryFileMinimal>>
        {
            Body = request.Body,
            Success = request.Success,
            Data = request.Data
        };
        Logger.Instance.ILog("FetchData: " + result.Data.Count);
        return result;
    }

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected override void SetTableData(List<LibraryFileMinimal> data)
    {
        if(string.IsNullOrWhiteSpace(this.filter) || SelectedStatus  != filterStatus)
            Table.SetData(data);
        else
            Table.SetData(data, filter: this.filter); 
    }
    
    Task Search() => NavigationService.NavigateTo("/library-files/search");
    
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
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.UnprocessedUpdated -= DataUpdated;
        feService.Files.FailedFilesUpdated -= DataUpdated2;
        feService.Files.SuccessfulUpdated -= DataUpdated2;
        feService.Files.OutOfScheduleUpdated -= DataUpdated;
        feService.Files.DisabledUpdated -= DataUpdated;
        feService.Files.OnHoldUpdated -= DataUpdated;
        feService.Files.ProcessingUpdated -= DataUpdated;
    }
}