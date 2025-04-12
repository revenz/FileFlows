using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input element that renders the child content
/// </summary>
public partial class InputChild : Input<object>
{
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
}