using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reporting  : ComponentBase
{
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }

    /// <summary>
    /// If scheduled reports is selected
    /// </summary>
    private bool ScheduledReportsSelected;
    
    /// <summary>
    /// The sky box items
    /// </summary>
    private List<FlowSkyBoxItem<bool>> SkyboxItems;
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }
    

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Layout.SetInfo("Reporting", "fas fa-chart-bar");
        var profile = feService.Profile.Profile;
        if (profile.LicensedFor(LicenseFlags.Reporting) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        
        SkyboxItems = new()
        {
            new()
            {
                Name = Translater.Instant("Pages.Reporting.Labels.Reports"),
                Value = false,
                Icon = "fas fa-chart-pie"
            },
            new()
            {
                Name = Translater.Instant("Pages.Reporting.Labels.ScheduledReports"),
                Value = true,
                Icon = "fas fa-clock"
            },
        };
    }
    
    
    private void SetSelected(FlowSkyBoxItem<bool> item)
    {
        ScheduledReportsSelected = item.Value;
        this.StateHasChanged();
    }
}