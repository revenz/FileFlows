namespace FileFlows.Client.Components.Inputs;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

/// <summary>
/// An input slider with a minimum and maximum
/// </summary>
public partial class InputSlider : Input<int>
{
    /// <summary>
    /// Gives focus to this control
    /// </summary>
    /// <returns>if successful</returns>
    public override bool Focus() => FocusUid();

    private int _Min = 0, _Max = int.MaxValue;
    
    /// <summary>
    /// Gets or sets if the value should be hidden
    /// </summary>
    [Parameter] public bool HideValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum value of the slider
    /// </summary>
    [Parameter]
    public int Min { get => _Min; set => _Min = value; }
    
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the maximum value of the slider
    /// </summary>
    [Parameter]
    public int Max { get => _Max; set => _Max = value == 0 ? int.MaxValue : value; }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets if the range should be inversed with the maximum on the left and the minimum on the right
    /// </summary>
    [Parameter]
    public bool Inverse { get; set; }
}