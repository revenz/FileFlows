using Microsoft.AspNetCore.Components;
namespace FileFlows.Client.Components.Common;


/// <summary>
/// Represents a tab within a collection of tabs.
/// </summary>
public partial class FlowTab:ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="FlowTabs"/> component containing this tab.
    /// </summary>
    [CascadingParameter] IFlowTabs Tabs { get; set; }
    
    /// <summary>
    /// Gets or sets an optional class name to apply to the tab.
    /// </summary>
    [Parameter] public string? ClassName { get; set; }

    private bool _Visible = true;

#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets a value indicating whether the tab is visible.
    /// </summary>
    [Parameter] public bool Visible
    {
        get => _Visible;
        set
        {
            if(_Visible == value)
                return;
            _Visible = value;
            Tabs?.TabVisibilityChanged();
            this.StateHasChanged();
        }
    }
#pragma warning restore BL0007

    private string _Title;

#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the title of the tab.
    /// </summary>
    [Parameter] public string Title
    {
        get => _Title;
        set => _Title = Translater.TranslateIfNeeded(value);
    }
#pragma warning restore BL0007
    
    /// <summary>
    /// Gets or sets the icon associated with the tab.
    /// </summary>
    [Parameter] public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets an optional unique identifier for the tab.
    /// </summary>
    [Parameter] public string? Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the content of the tab.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }


    /// <summary>
    /// Initializes the tab when it is first rendered.
    /// </summary>
    protected override void OnInitialized()
    {
        Tabs.AddTab(this);
    }
    
    /// <summary>
    /// Determines whether the current tab is active.
    /// </summary>
    /// <returns><c>true</c> if the current tab is active; otherwise, <c>false</c>.</returns>
    private bool IsActive() => this.Tabs.ActiveTab == this;
}
