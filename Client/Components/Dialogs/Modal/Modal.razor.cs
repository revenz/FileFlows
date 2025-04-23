using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A modal dialog
/// </summary>
public partial class Modal : ComponentBase
{
    /// <summary>
    /// Gets or sets the footer content
    /// </summary>
    [Parameter] public RenderFragment Footer { get; set; }
    
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
    
    
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    public Blocker Blocker { get; private set; }

    private Editor _Editor;
    
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    private Editor Editor
    {
        get => _Editor;
        set
        {
            _Editor = value;
            StateHasChanged();
        }
    }
}