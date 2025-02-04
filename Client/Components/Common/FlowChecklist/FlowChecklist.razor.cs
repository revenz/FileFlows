using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow checklist
/// </summary>
public partial class FlowChecklist : ComponentBase
{
    /// <summary>
    /// Gets or sets the checklist items
    /// </summary>
    [Parameter] public List<FlowChecklistItem> Items { get; set; } = new();
    
    /// <summary>
    /// Gets or set the default icon
    /// </summary>
    [Parameter] public string DefaultIcon { get; set; }
}

/// <summary>
/// Represents an item in a flow checklist.
/// </summary>
public class FlowChecklistItem
{
    /// <summary>
    /// Gets or sets the icon associated with the checklist item.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the checklist item.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the checklist item.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the checklist item is checked.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets the text displayed in the top-right corner of the checklist item.
    /// </summary>
    public string TopRightText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the checklist item is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the checklist item.
    /// </summary>
    public object Value { get; set; }
}
