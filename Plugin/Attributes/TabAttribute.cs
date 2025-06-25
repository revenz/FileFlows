namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to indicate a property should be on a specific tab
/// </summary>
public class TabAttribute
{
    /// <summary>
    /// Gets or sets the name of the tab
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Constructs a new instance of the tab attribute
    /// </summary>
    /// <param name="name">the name of the tab</param>
    public TabAttribute(string name)
    {
        Name = name;
    }
}