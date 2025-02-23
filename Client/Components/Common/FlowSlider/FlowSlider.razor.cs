using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A Slider input control with a minimum and maximum range
/// </summary>
public partial class FlowSlider:ComponentBase
{
    private readonly string Uid = Guid.NewGuid().ToString();

    protected int _Value;
    
    /// <summary>
    /// Gets or sets the minimum value of the slider
    /// </summary>
    [Parameter] public int Min { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the maximum value of the slider
    /// </summary>
    [Parameter] public int Max { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets if the value should be hidden
    /// </summary>
    [Parameter] public bool HideValue { get; set; }
    
    /// <summary>
    /// Gets or sets if the slider is inversed, with minimum on right and maximum on left
    /// </summary>
    [Parameter] public bool Inverse { get; set; }

    /// <summary>
    /// Gets or sets a prefix to show on the left side of the slider
    /// </summary>
    [Parameter] public string Prefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a suffix to show on the right side of the slider
    /// </summary>
    [Parameter] public string Suffix { get; set; } = string.Empty;
    
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the value of slider
    /// </summary>
    [Parameter]
    public int Value
    {
        get => _Value;
        set
        {
            if (_Value != value)
            {
                _Value = value;
                ValueChanged.InvokeAsync(value);
            }
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets an event that is called when the value changes
    /// </summary>
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }
}
