using System.Diagnostics.Tracing;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Dashboard
/// </summary>
public partial class Dashboard : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }

    /// <summary>
    /// The users profile
    /// </summary>
    private Profile Profile;
    
    /// <summary>
    /// The update data
    /// </summary>
    public UpdateInfo? UpdateInfoData;

    /// <summary>
    /// Selected page
    /// </summary>
    private int SelectedPage;

    /// <summary>
    /// The skybox
    /// </summary>
    private FlowSkyBox<int> Skybox;

    /// <summary>
    /// Translations
    /// </summary>
    private string  lblOverview, lblSavings, lblStatistics, lblNodes, lblRunners;

    private bool loaded = false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.Dashboard.Title"), "fas fa-chart-pie", pageClass: "dashboard-page");
        lblOverview = Translater.Instant("Pages.Dashboard.Tabs.Overview");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
        lblStatistics = Translater.Instant("Pages.Dashboard.Tabs.Statistics");
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = 0});
        UpdateInfoData = feService.Dashboard.CurrentUpdatesInfo;
        OnUpdatesUpdateInfo(UpdateInfoData);
        
        feService.Dashboard.UpdateInfoUpdated += OnUpdatesUpdateInfo;
        Profile = feService.Profile.Profile;
        

        if(App.Instance.IsMobile)
            PausedService.OnPausedLabelChanged += OnPausedLabelChanged;
        //await Refresh();
        
        StateHasChanged();
            
        loaded = true;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Skybox != null)
            SetSkyBoxItems();
    }

    private void SetSkyBoxItems()
    {
        if (Skybox == null)
            return;
        bool hasUpdates = feService.Dashboard.CurrentUpdatesInfo?.HasUpdates == true;
        if (SelectedPage == 2 && hasUpdates == false)
            SelectedPage = 0;
        Skybox.SetItems(new List<FlowSkyBoxItem<int>>()
        {
            new()
            {
                Name = lblOverview,
                Icon = "fas fa-chart-pie",
                Value = 0
            },
            new()
            {
                Name = lblSavings,
                Icon = "fas fa-dollar-sign",
                Value = 1
            },
            hasUpdates ? new()
            {
                Name = Translater.Instant("Pages.Dashboard.Widgets.System.Updates",  new { count = 0 }),
                Icon = "fas fa-cloud-download-alt",
                Value = 2
            } : null
        }.Where(x => x != null).ToList(),
            SelectedPage);
        StateHasChanged();
    }

    private void OnPausedLabelChanged(string label)
    {
        StateHasChanged();
    }
    
    /// <summary>
    /// Sets the selected skybox item
    /// </summary>
    /// <param name="item">the skybox item</param>
    private void SetSelected(FlowSkyBoxItem<int> item)
    {
        SelectedPage = item.Value;
        StateHasChanged();
    }

    /// <summary>
    /// Event raised when the update info has bene updated
    /// </summary>
    /// <param name="info">the current info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        UpdateInfoData = info;
        SetSkyBoxItems();
        StateHasChanged();
    }

    
    /// <summary>
    /// Called when Updates are clicked from the status widget
    /// </summary>
    private void Status_OnUpdatesClicked()
    {
        if (UpdateInfoData.HasUpdates)
        {
            // Tabs?.SelectTabByUid("updates");
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Dashboard.UpdateInfoUpdated-= OnUpdatesUpdateInfo;

        if(App.Instance.IsMobile)
            PausedService.OnPausedLabelChanged -= OnPausedLabelChanged;
    }
}