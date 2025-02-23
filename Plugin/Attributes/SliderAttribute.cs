namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to add a slider to a form
/// </summary>
public class SliderAttribute : FormInputAttribute
{
    /// <summary>
    /// Constructs a slider for a form
    /// </summary>
    /// <param name="order">the order in the UI the input will appear</param>
    /// <param name="inverse">if the range should be inversed with the maximum on the left and the minimum on the right</param>
    /// <param name="hideValue">if the value should be hidden</param>
    public SliderAttribute(int order, bool inverse = false, bool hideValue = false) : base(FormInputType.Slider, order)
    {
        this.Inverse = inverse;
        this.HideValue = hideValue;
    }
    
    /// <summary>
    /// Gets or sets if the range should be inversed with the maximum on the left and the minimum on the right
    /// </summary>
    public bool Inverse { get; set; }
    
    /// <summary>
    /// Gets or sets if the value should be hidden
    /// </summary>
    public bool HideValue { get; set; }
}