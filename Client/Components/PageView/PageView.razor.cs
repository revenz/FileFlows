using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Shared;
using Microsoft.CodeAnalysis.CSharp;

namespace FileFlows.Client.Components;

/// <summary>
/// Page view 
/// </summary>
public partial class PageView : IDisposable, IFlowTabs
{
    /// <summary>
    /// Gets or sets the nav menu component
    /// </summary>
    [CascadingParameter] public NavBar Menu { get; set; }

    /// <summary>
    /// Gets or sets any head fragment to render
    /// </summary>
    [Parameter] public RenderFragment Head { get; set; }
    
    /// <summary>
    /// Gets or sets the left head fragment to render
    /// </summary>
    [Parameter] public RenderFragment HeadLeft { get; set; }

    /// <summary>
    /// Gets or sets the body fragment to render
    /// </summary>
    [Parameter] public RenderFragment Body { get; set; }

    /// <summary>
    /// Gets or sets if the page view is full width
    /// </summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the title of the page view
    /// </summary>
    [Parameter] public string Title { get; set; }

    /// <summary>
    /// Gets or sets if this page view should add the flex class
    /// </summary>
    [Parameter] public bool Flex { get; set; }

    /// <summary>
    /// Gets or sets additional class names to add to the ViContainer
    /// </summary>
    [Parameter] public string ClassName { get; set; }
    
    /// <summary>
    /// Gets or sets the icon to use, if not set the icon from the nav menu will be used
    /// </summary>
    [Parameter] public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a form page
    /// </summary>
    [Parameter] public bool FormPage { get; set; }

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets if this is using tabs
    /// </summary>
    [Parameter] public bool TabView { get; set; }
    
    private string GetClassName()
        => $"{(ClassName ?? string.Empty)} {(FormPage ? "form-page" : "")} {(TabView ? "tab-view" : "")} ";

    /// <summary>
    /// If the connection to the server has been lost
    /// </summary>
    private bool ConnectionLost = false;
    
    /// <summary>
    /// If the system is upgrading
    /// </summary>
    private bool Upgrading;
    
    /// <summary>
    /// If the system has an upgrade pending
    /// </summary>
    private bool UpgradePending;

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblDisconnected;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblDisconnected = Translater.Instant("Labels.Disconnected");
        ConnectionLost = feService.ConnectionLost;
        feService.OnConnectionLost += OnConnectionLost; 
        feService.System.OnUpdatePending += OnUpgradePending; 
        feService.System.OnUpgrading += OnUpgrading; 
    }

    /// <summary>
    /// Called when the system is upgrading
    /// </summary>
    /// <param name="upgrading">if the system is upgraing</param>
    private void OnUpgrading(bool upgrading)
    {
        Upgrading = upgrading;
        UpgradePending = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called when an upgrade is pending
    /// </summary>
    /// <param name="pending">if the upgrade is pending</param>
    private void OnUpgradePending(bool pending)
    {
        UpgradePending = pending;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the server connection is lost
    /// </summary>
    /// <param name="connectionLost">true if the connection is lost, false if the connection is restablished</param>
    private void OnConnectionLost(bool connectionLost)
    {
        ConnectionLost = connectionLost;
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.OnConnectionLost -= OnConnectionLost;
        feService.System.OnUpdatePending -= OnUpgradePending; 
        feService.System.OnUpgrading -= OnUpgrading; 
        
    }

    /// <inheritdoc />
    public FlowTab ActiveTab { get; set; }

    /// <summary>
    /// Gets or sets if the title should only be shown on the active tab
    /// </summary>
    [Parameter] public bool TitleOnlyOnActive { get; set; }
    
    /// <inheritdoc />
    public void TabVisibilityChanged()
        => StateHasChanged();

    /// <summary>
    /// Represents a collection of tabs.
    /// </summary>
    private List<FlowTab> Tabs = new();

    /// <inheritdoc />
    public void TriggerStateHasChanged()
        => StateHasChanged();

    /// <inheritdoc />
    public void AddTab(FlowTab tab)
    {
        if (Tabs.Contains(tab) == false)
        {
            Tabs.Add(tab);
            if (ActiveTab == null)
                ActiveTab = tab;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a tab.
    /// </summary>
    /// <param name="tab">The tab to select.</param>
    private void SelectTab(FlowTab tab)
    {
        ActiveTab = tab;
    }
    

    /// <inheritdoc />
    public void SelectFirstTab(bool forced = false)
    {
        if (ActiveTab == null || forced)
        {
            ActiveTab = Tabs.FirstOrDefault(x => x.Visible);
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public void SelectTabByUid(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return;
        
        var tab = Tabs.FirstOrDefault(x => x.Uid == uid);
        if (tab != null)
        {
            SelectTab(tab);
            StateHasChanged();
        }
    }

}