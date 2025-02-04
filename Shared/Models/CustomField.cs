using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Custom field used by reseller flows and possible templates in the future
/// </summary>
public class CustomField
{
    /// <summary>
    /// Gets or sets the name of the field
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the variable this value binds to
    /// </summary>
    public string Variable { get; set; }
    
    /// <summary>
    /// Gets or sets a description for this field
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the field type
    /// </summary>
    public CustomFieldType Type { get; set; }

    /// <summary>
    /// Gets or sets Data for this custom field
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = [];
}

/// <summary>
/// A list of custom field types
/// </summary>
public enum CustomFieldType
{
    /// <summary>
    /// Text field
    /// </summary>
    Text,
    /// <summary>
    /// Boolean field
    /// </summary>
    Boolean,
    /// <summary>
    /// Integer field
    /// </summary>
    Integer,
    /// <summary>
    /// Select field
    /// </summary>
    Select,
    /// <summary>
    /// Slider
    /// </summary>
    Slider,
    /// <summary>
    /// Option group
    /// </summary>
    OptionGroup
}