using BlazorContextMenu;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
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

    private string lblTitle;


    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;
    
    protected override void OnInitialized()
    {
        Profile ??= feService.Profile.Profile;
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");
        lblTitle = Translater.Instant("Pages.Nodes.Title");

        Data = feService.Node.NodeStatusSummaries
            .OrderBy(x => x.Status is ProcessingNodeStatus.Offline ? 1 : 0)
            .ThenBy(x => x.Enabled ? 0 : 1)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
        initialSortOrder = Data.Select(x => x.Uid).ToList();
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
                int index = initialSortOrder.IndexOf(x.Uid);
                return index >= 0 ? index : int.MaxValue;
            })
            .ThenBy(x => x.Status is ProcessingNodeStatus.Offline ? 1 : 0)
            .ThenBy(x => x.Enabled ? 0 : 1)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
        StateHasChanged();
        Table?.TriggerStateHasChanged();
    }

    // public override Task Refresh(bool showBlocker = true)
    // {
    //     return Task.CompletedTask;
    // }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        
        // fixes an issue with the table column heade rnot shown
        if(firstRender)
            Table.TriggerStateHasChanged();
        
    }

    /// <summary>
    /// Opens the help page
    /// </summary>
    void OpenHelp()
        => _ = App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/configuration/nodes");
    
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
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Node.NodeStatusUpdated -= NodeOnNodeStatusUpdated;
    }
}