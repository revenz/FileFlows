using FileFlows.Client.ClientModels;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow Tabs the can be created by a RenderFragment builder
/// </summary>
public partial class FlowTabsBuilder:ComponentBase
{
    /// <summary>
    /// Gets or sets the tabs
    /// </summary>
    [Parameter]
    public List<EditorTab> Tabs { get; set; } = [];
    
    /// <summary>
    /// Gets or sets an event that is called when the editor is saved
    /// </summary>
    [Parameter] public EventCallback OnSubmit { get; set; }
    
    /// <summary>
    /// Gets or sets an event that is called when the editor is closed
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }
    
    [Parameter] public Func<EditorTab, bool> IsVisible { get; set; }

    private FlowTabs FlowTabs;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            FlowTabs.SelectFirstTab();
            foreach (var tab in Tabs)
            {
                tab.HiddenChanged += (sender, value) =>
                {
                    StateHasChanged();
                };
            }
        }
    }
}