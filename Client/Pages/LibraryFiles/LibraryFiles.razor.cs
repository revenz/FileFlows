using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;

namespace FileFlows.Client.Pages;

/// <summary>
/// Library Files Page
/// </summary>
public partial class LibraryFiles : ListPage<Guid, LibraryFileMinimal>
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
    private FlowSkyBox<FileStatus> Skybox { get; set; }

    private FileFlows.Shared.Models.FileStatus SelectedStatus;

    /// <summary>
    /// The current page
    /// </summary>
    private int PageIndex;

    private Guid? SelectedNode, SelectedLibrary, SelectedFlow, SelectedTag;
    private FilesSortBy? SelectedSortBy;

    private string lblSearch, lblDeleteSwitch, lblForcedProcessing, lblSortBy, lblNode, lblFlow, lblLibrary, lblTag;

    private string TableIdentifier => "LibraryFiles_" + this.SelectedStatus; 

    private string Title;
    private string lblLibraryFiles, lblFileFlowsServer;
    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    private int TotalItems;
    private List<FlowExecutorInfo> WorkerStatus = new ();
    private Dictionary<string, NodeInfo> Nodes = new();
    private Dictionary<Guid, string> Libraries = new();
    private Dictionary<Guid, string>  Flows = new();
    private List<DropDownOption> optionsLibraries, optionsNodes, optionsFlows, optionsSortBy, optionsTags;

    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private async Task SetSelected(FlowSkyBoxItem<FileStatus> status)
    {
        SelectedStatus = status.Value;
        PageIndex = 0;
        Title = lblLibraryFiles;// + ": " + status.Name;
        await Refresh();
        StateHasChanged();
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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        this.SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;
        lblForcedProcessing = Translater.Instant("Labels.ForceProcessing");
        lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
        lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        Title = lblLibraryFiles; // + ": " + Translater.Instant("Enums.FileStatus." + FileStatus.Unprocessed);
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
            optionsTags = feService.Dashboard.Tags.Select(x => new DropDownOption()
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

        
        RefreshStatus(feService.Files.LibraryFileCounts);
        SetTableData(feService.Files.UpcomingFiles);
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
           s.Name = Translater.Instant("Enums.FileStatus." + s.Status);

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

       if (Skybox == null)
       {
           _ = Task.Run(async () =>
           {
               await WaitForRender();
               Skybox.SetItems(sbItems, SelectedStatus);
               StateHasChanged();
           });
       }
       else
       {
           Skybox.SetItems(sbItems, SelectedStatus);
           StateHasChanged();
           
       }
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
}