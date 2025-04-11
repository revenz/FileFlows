using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Wizards;
using Microsoft.JSInterop;
using ffFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public partial class Flows : ListPage<Guid, FlowListModel>, IDisposable
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
    private List<FlowListModel> DataFileDrop = new();
    private FlowType SelectedType = FlowType.Standard;

    public override string FetchUrl => ApiUrl + "/list-all";
    private string lblFailureFlowDescription, lblDefault, lblReadOnly, lblInUse;

    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.Flows.Title"), "fas fa-sitemap");
        
        Profile = feService.Profile.Profile;
        OnInitialized(false);
        
        lblFailureFlowDescription = Translater.Instant("Pages.Flows.Messages.FailureFlowDescription");
        lblDefault = Translater.Instant("Labels.Default");
        lblReadOnly = Translater.Instant("Labels.ReadOnly");
        lblInUse = Translater.Instant("Labels.InUse");
        
        feService.Flow.FlowsUpdated += FlowOnFlowsUpdated;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // so the skybox exists
            UpdateData();
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    private void FlowOnFlowsUpdated(List<FlowListModel> obj)
        => UpdateData();

    private void UpdateData()
    {
        Data = feService.Flow.Flows.OrderBy(x => x.Name.ToLowerInvariant()).ToList();
        UpdateTypeData();
    }

    /// <inheritdoc />
    protected override string GetAuditTypeName()
        => typeof(ffFlow).FullName;


    /// <summary>
    /// Adds a new flow
    /// </summary>
    private void Add()
    {
        _ = ModalService.ShowModal<NewFlowWizard, Flow>(new NewFlowWizardOptions()
        {
            FileDropFlow = Profile.LicensedFor(LicenseFlags.FileDrop) && Skybox.SelectedItem.Value is FlowType.FileDrop 
        });
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
                feService.Notifications.ShowError(Translater.Instant("Pages.Flows.Messages.FailedToExport"));
                return;
            }

            await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", items[0].Name + ".json", result.Body);
        }
        else
        {
            var result = await HttpHelper.Get<byte[]>(url);
            if (result.Success == false)
            {
                feService.Notifications.ShowError(Translater.Instant("Pages.Flows.Messages.FailedToExport"));
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
                //await this.Refresh();
                feService.Notifications.ShowSuccess(Translater.Instant("Pages.Flows.Messages.FlowImported", new { name = newFlow.Data.Name }));
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
    private async Task Duplicate(bool asFileDropFlow = false)
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
            if(asFileDropFlow)
                url += "?asFileDropFlow=true";
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                //await this.Refresh();
                feService.Notifications.ShowSuccess(Translater.Instant("Pages.Flows.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                feService.Notifications.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private void UpdateTypeData()
    {
        this.DataFailure = this.Data.Where(x => x.Type == FlowType.Failure).ToList();
        this.DataStandard = this.Data.Where(x => x.Type == FlowType.Standard).ToList();
        this.DataSubFlows = this.Data.Where(x => x.Type == FlowType.SubFlow).ToList();
        this.DataFileDrop = this.Data.Where(x => x.Type == FlowType.FileDrop).ToList();
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
            },
            Profile.LicensedFor(LicenseFlags.FileDrop) ? new ()
            {
                Name = Translater.Instant("Pages.Flows.Labels.FileDropFlows"),
                Icon = "fas fa-tint",
                Count = this.DataFileDrop.Count,
                Value = FlowType.FileDrop
            } : null
        }.Where(x => x != null).ToList(), this.SelectedType);
    }

    /// <summary>
    /// Sets the selected skybox item
    /// </summary>
    /// <param name="item">the skybox item</param>
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
            //await this.Refresh();
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
            feService.Notifications.ShowError("Pages.Flows.Messages.DeleteUsed");
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

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Flow.FlowsUpdated -= FlowOnFlowsUpdated;
    }
}
