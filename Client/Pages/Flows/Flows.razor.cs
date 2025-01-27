using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Dialogs.Wizards;
using Microsoft.JSInterop;
using ffFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public partial class Flows : ListPage<Guid, FlowListModel>
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    private string TableIdentifier => "Flows-" + this.SelectedType;

    public override string ApiUrl => "/api/flow";

    private FlowSkyBox<FlowType> Skybox;

    private List<FlowListModel> DataStandard = new();
    private List<FlowListModel> DataSubFlows = new();
    private List<FlowListModel> DataFailure = new();
    private FlowType SelectedType = FlowType.Standard;

    public override string FetchUrl => ApiUrl + "/list-all";
    private string lblFailureFlowDescription, lblDefault, lblReadOnly, lblInUse;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblFailureFlowDescription = Translater.Instant("Pages.Flows.Messages.FailureFlowDescription");
        lblDefault = Translater.Instant("Labels.Default");
        lblReadOnly = Translater.Instant("Labels.ReadOnly");
        lblInUse = Translater.Instant("Labels.InUse");
    }

    /// <inheritdoc />
    protected override string GetAuditTypeName()
        => typeof(ffFlow).FullName;


    /// <summary>
    /// Adds a new flow
    /// </summary>
    private void Add()
    {
        //NavigationManager.NavigateTo("flows/" + Guid.Empty);
        _ = ModalService.ShowModal<NewFlowWizard, Flow>(new NewVideoFlowWizardOptions());
    }

    public override async Task<bool> Edit(FlowListModel item)
    {
        if(item != null)
            NavigationManager.NavigateTo("flows/" + item.Uid);
        return await Task.FromResult(false);
    }

    private async Task Export()
    {
        var items = Table.GetSelected()?.ToList() ?? new (); 
        if (items.Any() != true)
            return;
        string url = $"/api/flow/export?{string.Join("&", items.Select(x => "uid=" + x.Uid))}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif

        if (items.Count == 1)
        {
            var result = await HttpHelper.Get<string>(url);
            if (result.Success == false)
            {
                Toast.ShowError(Translater.Instant("Pages.Flows.Messages.FailedToExport"));
                return;
            }

            await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", items[0].Name + ".json", result.Body);
        }
        else
        {
            var result = await HttpHelper.Get<byte[]>(url);
            if (result.Success == false)
            {
                Toast.ShowError(Translater.Instant("Pages.Flows.Messages.FailedToExport"));
                return;
            }
            await jsRuntime.InvokeVoidAsync("ff.saveByteArrayAsFile", "Flows.zip", result.Data);
        }
    }

    private async Task Import()
    {
        var idResult = await ImportDialog.Show();
        string json = idResult.content;
        if (string.IsNullOrEmpty(json))
            return;

        Blocker.Show();
        try
        {
            var newFlow = await HttpHelper.Post<ffFlow>("/api/flow/import", json);
            if (newFlow != null && newFlow.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.FlowImported", new { name = newFlow.Data.Name }));
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    /// <summary>
    /// Duplicate the selected item
    /// </summary>
    private async Task Duplicate()
    {
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/flow/duplicate/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    protected override Task PostDelete() => Refresh();

    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFailure = this.Data.Where(x => x.Type == FlowType.Failure).ToList();
        this.DataStandard = this.Data.Where(x => x.Type == FlowType.Standard).ToList();
        this.DataSubFlows = this.Data.Where(x => x.Type == FlowType.SubFlow).ToList();
        this.Skybox.SetItems(new List<FlowSkyBoxItem<FlowType>>()
        {
            new ()
            {
                Name = Translater.Instant("Pages.Flows.Labels.StandardFlows"),
                Icon = "fas fa-sitemap",
                Count = this.DataStandard.Count,
                Value = FlowType.Standard
            },
            new ()
            {
                Name = Translater.Instant("Pages.Flows.Labels.SubFlows"),
                Icon = "fas fa-subway",
                Count = this.DataSubFlows.Count,
                Value = FlowType.SubFlow
            },
            new ()
            {
                Name = Translater.Instant("Pages.Flows.Labels.FailureFlows"),
                Icon = "fas fa-exclamation-circle",
                Count = this.DataFailure.Count,
                Value = FlowType.Failure
            }
        }.Where(x => x != null).ToList(), this.SelectedType);
    }

    private void SetSelected(FlowSkyBoxItem<FlowType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task SetDefault()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        
        Blocker.Show();
        try
        {
            await HttpHelper.Put($"/api/flow/set-default/{item.Uid}?default={(!item.Default)}");
            await this.Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Flows.Messages.DeleteUsed");
            return;
        }
        await base.Delete();
    }

    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
}
