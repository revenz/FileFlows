using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow radio group
/// </summary>
/// <typeparam name="TItem">The type of value bound to the Value</typeparam>
public partial class FlowRadioGroup<TItem> : ComponentBase
{
    /// <summary>
    /// Gets or sets the selected item
    /// </summary>
    [Parameter]
    public TItem Value { get; set; }

    /// <summary>
    /// Event called when the value changes
    /// </summary>
    [Parameter] 
    public EventCallback<TItem> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label prefix to apply to item labels
    /// </summary>
    [Parameter] public string LabelPrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the wizard contaning this
    /// </summary>
    [CascadingParameter] public FlowWizard FlowWizard { get; set; }
    

    /// <summary>
    /// Represents a collection of tabs.
    /// </summary>
    private List<FlowRadioGroupItem<TItem>> Items = new();
    
    /// <summary>
    /// Adds an item for rendering
    /// </summary>
    public void AddItem(FlowRadioGroupItem<TItem> item)
    {
        if (Items.Contains(item) == false)
        {
            Items.Add(item);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Selects a new value
    /// </summary>
    /// <param name="itemValue">the item value</param>
    private void Select(TItem itemValue)
    {
        Value = itemValue;
        ValueChanged.InvokeAsync(itemValue);
    }

    /// <summary>
    /// Dbl Click a new value
    /// </summary>
    /// <param name="itemValue">the item value</param>
    private void DblClick(TItem itemValue)
    {
        Value = itemValue;
        ValueChanged.InvokeAsync(itemValue);
        if(FlowWizard is { NonWizard: true })
            FlowWizard.Finish();
    }
}