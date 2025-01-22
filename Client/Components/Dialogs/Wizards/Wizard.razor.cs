using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs.Wizards;

/// <summary>
/// A wizard popup
/// </summary>
public partial class Wizard : ComponentBase
{
    /// <summary>
    /// Gets or sets the body content
    /// </summary>
    [Parameter] public RenderFragment Body { get; set; }
    
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    [Parameter] public string Title { get; set; }

    /// <summary>
    /// Gets or sets optional styling
    /// </summary>
    [Parameter] public string Styling { get; set; }
    
    /// <summary>
    /// Gets or sets if this is visible or note
    /// </summary>
    [Parameter] public bool Visible { get; set; }
}