using BlazorContextMenu;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Nodes : ListPage<Guid, NodeStatusSummary>, IDisposable
{
    public override string ApiUrl => "/api/node";
    const string FileFlowsServer = "FileFlowsServer";

    // private NodeStatusSummary EditingItem = null;

    private string lblInternal, lblAddress, lblRunners, lblVersion, lblDownloadNode, lblUpgradeRequired, 
        lblUpgradeRequiredHint, lblRunning, lblDisconnected, lblPossiblyDisconnected, lblPriority;

    private List<Guid> sortOrder;
     
#if(DEBUG)
    string DownloadUrl = "http://localhost:6868/download";
#else
    string DownloadUrl = "/download";
#endif
    protected override void OnInitialized()
    {
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");
        lblInternal= Translater.Instant("Pages.Nodes.Labels.Internal");
        lblAddress = Translater.Instant("Pages.Nodes.Labels.Address");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblVersion = Translater.Instant("Pages.Nodes.Labels.Version");
        lblDownloadNode = Translater.Instant("Pages.Nodes.Labels.DownloadNode");
        lblUpgradeRequired = Translater.Instant("Pages.Nodes.Labels.UpgradeRequired");
        lblUpgradeRequiredHint = Translater.Instant("Pages.Nodes.Labels.UpgradeRequiredHint");
        lblPriority = Translater.Instant("Pages.ProcessingNode.Fields.Priority");

        lblRunning = Translater.Instant("Labels.Running");
        lblPossiblyDisconnected = Translater.Instant("Pages.Nodes.Labels.PossiblyDisconnected");
        lblDisconnected = Translater.Instant("Pages.Nodes.Labels.Disconnected");

        Data = feService.Node.NodeStatusSummaries.OrderBy(x => x.Enabled ? 0 : 1)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
        sortOrder = Data.Select(x => x.Uid).ToList();
        feService.Node.NodeStatusUpdated += NodeOnNodeStatusUpdated;
    }

    /// <summary>
    /// Called when the node statuses are updated
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void NodeOnNodeStatusUpdated(List<NodeStatusSummary> obj)
    {
        Data = obj.OrderBy(x =>
            {
                int index = sortOrder.IndexOf(x.Uid);
                return index >= 0 ? index : int.MaxValue;
            }).ThenBy(x => x.Enabled ? 0 : 1)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
        StateHasChanged();
    }

    public override Task Refresh(bool showBlocker = true)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;

    /// <summary>
    /// The highest and lowest priorities in the data
    /// </summary>
    private int HighestPriority, LowestPriority;
    
    /// <inheritdoc />
    public override Task PostLoad()
    {
        var serverNode = this.Data?.Where(x => x.Address == FileFlowsServer).FirstOrDefault();
        HighestPriority = Data?.Max(x => x.Priority) ?? 0;
        LowestPriority = Data?.Min(x => x.Priority) ?? 0;
        if(serverNode != null)
        {
            serverNode.Name = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");                
        }

        if (initialSortOrder == null)
        {
            Data = Data?.OrderByDescending(x => x.Enabled)?.ThenByDescending(x => x.Priority).ThenBy(x => x.Name)
                ?.ToList();
            initialSortOrder = Data?.Select(x => x.Uid)?.ToList();
        }
        else
        {
            Data = Data?.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenByDescending(x => x.Priority).ThenBy(x => x.Name)
                ?.ToList();
        }

        return base.PostLoad();
    }
    
    /// <summary>
    /// Opens the help page
    /// </summary>
    void OpenHelp()
        => _ = App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/configuration/nodes");

    /// <summary>
    /// if currently enabling, this prevents double calls to this method during the updated list binding
    /// </summary>
    private bool enabling = false;
    new EventCallback Enable(bool enabled, NodeStatusSummary node)
    {
        if(enabling || node.Enabled == enabled)
            return EventCallback.Empty;
        _ = Task.Run(async () =>
        {
            Blocker.Show();
            enabling = true;
            try
            {
                await HttpHelper.Put<ProcessingNode>($"{ApiUrl}/state/{node.Uid}?enable={enabled}");
                await Refresh();
            }
            finally
            {
                enabling = false;
                Blocker.Hide();
            }
        });
        return EventCallback.Empty;
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<ProcessingNode>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            // int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            // if (index < 0)
            //     this.Data.Add(saveResult.Data);
            // else
            //     this.Data[index] = saveResult.Data;
            // await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    public async Task DeleteItem(ProcessingNode item)
    {
        if (await Confirm.Show("Labels.Delete",
                Translater.Instant("Pages.Nodes.Messages.DeleteNode", new { name = item.Name })) == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete(DeleteUrl, new ReferenceModel<Guid> { Uids = new [] { item.Uid } });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }
            //this.Data.Remove(item);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Checks if two versions are the same
    /// </summary>
    /// <param name="versionA">the first version</param>
    /// <param name="versionB">the second version</param>
    /// <returns>true if same, otherwise false</returns>
    private bool VersionsAreSame(string versionA, string versionB)
    {
        if (versionA == versionB)
            return true;
        if(versionA == null || versionB == null)
            return false;
        if (string.Equals(versionA, versionB, StringComparison.InvariantCultureIgnoreCase))
            return true;
        if (Version.TryParse(versionA, out var va) == false)
            return false;
        if (Version.TryParse(versionB, out var vb) == false)
            return false;
        return va == vb;
    }

    /// <summary>
    /// Gets the default icon for a node
    /// </summary>
    /// <param name="node">the node</param>
    /// <returns>the default icon</returns>
    private string GetDefaultIcon(ProcessingNode node)
    {
        if (node.OperatingSystem == OperatingSystemType.Mac)
            return "svg:apple";
        if (node.OperatingSystem == OperatingSystemType.Docker)
            return "svg:docker";
        if (node.OperatingSystem == OperatingSystemType.Windows)
            return "svg:windows";
        if (node.OperatingSystem == OperatingSystemType.Linux)
            return "svg:linux";
        return "fas fa-desktop";
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Node.NodeStatusUpdated -= NodeOnNodeStatusUpdated;
    }
}