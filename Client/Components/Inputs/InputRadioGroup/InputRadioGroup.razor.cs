using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// An radio button group input
/// </summary>
public partial class InputRadioGroup<TItem> : Input<TItem>
{
    /// <summary>
    /// Gets or sets the options
    /// </summary>
    [Parameter]
    public IEnumerable<ListOption> Options { get; set; }

    /// <summary>
    /// Sets the value
    /// </summary>
    /// <param name="option">the option being set</param>
    private void SetValue(ListOption option)
    {
        if (option.Value is TItem value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// Gets if the option is the active value
    /// </summary>
    /// <param name="option">the option</param>
    /// <returns>true if active, otherwise false</returns>
    private bool IsActive(ListOption option)
    {
        if(option.Value is TItem value)
            return value.Equals(this.Value);
        return false;
    }
}