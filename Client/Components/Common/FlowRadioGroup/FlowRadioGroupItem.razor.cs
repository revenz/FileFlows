using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

public partial class FlowRadioGroupItem<TItem> : ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="FlowRadioGroup"/> component containing this tab.
    /// </summary>
    [CascadingParameter]
    FlowRadioGroup<TItem> RadioGroup { get; set; }

    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    [Parameter]
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the translation label
    /// </summary>
    [Parameter]
    public string TLabel { get; set; }
    
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter] public TItem Value { get; set; }
    
    /// <summary>
    /// Gets or sets any render content
    /// </summary>
    [Parameter]public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Initializes the tab when it is first rendered.
    /// </summary>
    protected override void OnInitialized()
    {
        string label = (string.IsNullOrWhiteSpace(RadioGroup.LabelPrefix) ? string.Empty : RadioGroup.LabelPrefix + ".")
                       + TLabel;
        Title = Translater.Instant(label);
        Description = Translater.TranslateIfHasTranslation(label + "Description", string.Empty);
        RadioGroup.AddItem(this);
    }
}