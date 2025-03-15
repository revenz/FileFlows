using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A skybox
/// </summary>
public partial class FlowPageTabs<TItem>
{
    private readonly List<FlowPageTabItem> _Items = new();
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]  protected FrontendService feService { get; set; }

    /// <summary>
    /// Gets or set the items to display in the skybox
    /// </summary>
    public List<FlowPageTabItem> Items
    {
        get => _Items;
        set
        {
            _Items.Clear();
            if (value?.Any() == true)
                _Items.AddRange(value);
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Gets or sets if this sky box does not show a count
    /// </summary>
    [Parameter]
    public bool NoCount { get; set; }

    /// <summary>
    /// Gets or sets the selected item
    /// </summary>
    public FlowPageTabItem SelectedItem { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Items?.Any() == true)
            SelectedItem = Items.First();
    }

    /// <summary>
    /// Sets the selected item
    /// </summary>
    /// <param name="item">the item to select</param>
    void SetSelected(FlowPageTabItem item)
    {
        this.SelectedItem = item;
        AfterItemSelected(item);
    }
    
    /// <summary>
    /// Called after SetSelected
    /// </summary>
    /// <param name="item">the newly selected item</param>
    protected virtual void AfterItemSelected(FlowPageTabItem item) {}

    /// <summary>
    /// An item displayed in a page tab
    /// </summary>
    public class FlowPageTabItem
    {
        /// <summary>
        /// Gets or sets the icon
        /// </summary>
        public string Icon { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the class name
        /// </summary>
        public string ClassName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the count 
        /// </summary>
        public int Count { get; set; }
    }
}