using System.Diagnostics.Tracing;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
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
    /// The users profile
    /// </summary>
    private Profile Profile;
    
    /// <summary>
    /// The update data
    /// </summary>
    public UpdateInfo? UpdateInfoData;

    /// <summary>
    /// The tabs
    /// </summary>
    private IFlowTabs Tabs;

    /// <summary>
    /// Translations
    /// </summary>
    private string lblTitle, lblDashboard, lblSavings, lblUpdates, lblStatistics, lblNodes, lblRunners;

    private bool loaded = false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Title");
        lblDashboard = Translater.Instant("Pages.Dashboard.Tabs.Dashboard");
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
        
        loaded = true;
        StateHasChanged();
    }

    private void OnPausedLabelChanged(string label)
    {
        StateHasChanged();
    }

    /// <summary>
    /// Event raised when the update info has bene updated
    /// </summary>
    /// <param name="info">the current info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        UpdateInfoData = info;
        int count = UpdateInfoData?.NumberOfUpdates ?? 0;
        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.System.Updates",
                 new { count = 0 });
        StateHasChanged();
        if (count == 0 && Tabs?.ActiveTab?.Uid == "updates")
        {
            Tabs.SelectFirstTab(true);
        }
        else
            Tabs?.TriggerStateHasChanged();
    }

    
    /// <summary>
    /// Called when Updates are clicked from the status widget
    /// </summary>
    private void Status_OnUpdatesClicked()
    {
        if (UpdateInfoData.HasUpdates)
        {
            Tabs?.SelectTabByUid("updates");
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