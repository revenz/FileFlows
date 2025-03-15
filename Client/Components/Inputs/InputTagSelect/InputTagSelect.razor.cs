using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for selecting tags
/// </summary>
public partial class InputTagSelect : Input<List<Guid>>
{
    List<Tag> Tags { get; set; } = new List<Tag>();
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitializedAsync();
        Tags = feService.Dashboard.Tags;
        Value ??= [];
    }

    /// <summary>
    /// Toggles a tag on or off
    /// </summary>
    /// <param name="tag">the tag value</param>
    private void ToggleSelection(Tag tag)
    {
        var list = Value?.ToList() ?? [];
        if (list.Contains(tag.Uid))
            list.Remove(tag.Uid);
        else
            list.Add(tag.Uid);
        Value = list;
    }
}