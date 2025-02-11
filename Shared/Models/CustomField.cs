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
    /// Gets or sets a condition field that needs to be equal to the value to show this field
    /// </summary>
    public string ConditionField { get; set; }
    /// <summary>
    /// Gets or sets a value for the condition field
    /// </summary>
    public string ConditionValue { get; set; }

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
    /// Text area
    /// </summary>
    TextArea,
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
    OptionGroup,
    /// <summary>
    /// Image resource
    /// </summary>
    Image
}