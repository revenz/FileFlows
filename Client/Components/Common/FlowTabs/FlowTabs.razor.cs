using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Interface for tabs
/// </summary>
public interface IFlowTabs
{
    /// <summary>
    /// Called when the visibility of a tab has changed
    /// </summary>
    void TabVisibilityChanged();

    /// <summary>
    /// Adds a tab to the collection.
    /// </summary>
    /// <param name="tab">The tab to add.</param>
    void AddTab(FlowTab tab);
    
    /// <summary>
    /// Gets or set the active tab
    /// </summary>
    FlowTab ActiveTab { get; set; }

    /// <summary>
    /// Sets the first visible tab as the active tab.
    /// </summary>
    /// <param name="forced">If we are forcing the first tab to be active</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    void SelectFirstTab(bool forced = false);

    /// <summary>
    /// Selects a tab by its unique identifier.
    /// </summary>
    /// <param name="uid">the tabs UID</param>
    void SelectTabByUid(string uid);

    /// <summary>
    /// Triggers the state has changed
    /// </summary>
    void TriggerStateHasChanged();
}

/// <summary>
/// Represents a collection of tabs.
/// </summary>
public partial class FlowTabs : ComponentBase, IFlowTabs
{
    /// <summary>
    /// Gets or sets the content of the tabs.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the style of the tabs
    /// </summary>
    [Parameter]
    public TabStyle Style { get; set; }
    
    /// <inheritdoc />
    public FlowTab ActiveTab { get; set; }
    
    /// <summary>
    /// Gets or sets if the title should only be shown on the active tab
    /// </summary>
    [Parameter] public bool TitleOnlyOnActive { get; set; }
    
    /// <summary>
    /// Gets or sets if the tabs should be contained with contain-tabs css
    /// Used by the flow editor to contain the flow parts
    /// </summary>
    [Parameter] public bool ContainTabs { get; set; }
    
    /// <summary>
    /// Gets or sets an event when a tab changes
    /// </summary>
    [Parameter] public EventCallback<int> OnTabChanged { get; set; }
    
    /// <summary>
    /// Gets or sets if the tabs cannot be changed
    /// </summary>
    [Parameter] public bool DisableChanging { get; set; }

    /// <summary>
    /// Represents a collection of tabs.
    /// </summary>
    private List<FlowTab> Tabs = new();

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
        if (DisableChanging) return;
        ActiveTab = tab;
        OnTabChanged.InvokeAsync(Tabs.IndexOf(tab));
    }

    /// <inheritdoc />
    public void TabVisibilityChanged()
        => StateHasChanged();

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        SelectFirstTab();
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public void SelectFirstTab(bool forced = false)
    {
        if (DisableChanging) return;
        
        if (ActiveTab == null || forced)
        {
            ActiveTab = Tabs.FirstOrDefault(x => x.Visible);
            OnTabChanged.InvokeAsync(Tabs.IndexOf(ActiveTab));
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

    /// <inheritdoc />
    public void TriggerStateHasChanged()
        => StateHasChanged();

    /// <summary>
    /// Removes the current tab
    /// </summary>
    public void RemoveCurrentTab()
    {
        if (ActiveTab != null)
        {
            Tabs.Remove(ActiveTab);
            ActiveTab = null;
            SelectFirstTab(true);
        }
    }
}

/// <summary>
/// Tab style
/// </summary>
public enum TabStyle
{
    /// <summary>
    /// New style
    /// </summary>
    NewStyle = 0,
    /// <summary>
    /// Standard style
    /// </summary>
    OldStyle = 1,
    /// <summary>
    /// Minimal style
    /// </summary>
    Minimal = 2
}
