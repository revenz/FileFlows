namespace FileFlows.Plugin.Models;

/// <summary>
/// Tab definition use in flow elements
/// </summary>
public class TabDefinition
{
    /// <summary>
    /// Gets or sets the name of the tab
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the a condition property to check if this tab is shown
    /// </summary>
    public string ConditionProperty { get; set; }

    /// <summary>
    /// Gets or sets a condition value to check if this tab is shown
    /// </summary>
    public object ConditionValue { get; set; }

    /// <summary>
    /// Gets or sets if the condition check should be inversed
    /// </summary>
    public bool ConditionInverse { get; set; }
}