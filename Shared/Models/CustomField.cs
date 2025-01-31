using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Custom field used by reseller flows and possible templates in the future
/// </summary>
public abstract class CustomField
{
    /// <summary>
    /// Gets or sets the name of the field
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the field type
    /// </summary>
    public abstract CustomFieldType Type { get; }
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
    Select
}

/// <summary>
/// Select custom field
/// </summary>
public class SelectCustomField : CustomField
{
    /// <inheritdoc />
    public override CustomFieldType Type => CustomFieldType.Select;
    
    /// <summary>
    /// Gets or sets the options to show
    /// </summary>
    public List<ListOption> Options { get; set; } = new List<ListOption>();
}