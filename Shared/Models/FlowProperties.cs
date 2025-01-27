using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Advanced flow properties
/// </summary>
public class FlowProperties
{
    private List<FlowField> _Fields = new();
    
    // /// <summary>
    // /// Gets or sets the author or this flow
    // /// </summary>
    // public string Author { get; set; }
    //
    // /// <summary>
    // /// Gets or sets the the minimum version required for this template
    // /// </summary>
    // public string MinimumVersion { get; set; }
    //
    // /// <summary>
    // /// Gets or sets tags for this flow
    // /// </summary>
    // public string[] Tags { get; set; }
    //
    /// <summary>
    /// Gets or sets the outputs, only used for sub flows
    /// </summary>
    public List<KeyValuePair<int, string>> Outputs { get; set; }

    /// <summary>
    /// Gets or sets the fields
    /// </summary>
    public List<FlowField> Fields
    {
        get => _Fields;
        set => _Fields = value ?? new();
    }
    private Dictionary<string, object> _Variables = new();

    /// <summary>
    /// Gets or sets variables that can be used in this flow
    /// </summary>
    public Dictionary<string, object> Variables
    {
        get => _Variables;
        set => _Variables = value ?? new();
    }
}

/// <summary>
/// A property of the flow that can be used to generate a template
/// </summary>
public class FlowField
{
    /// <summary>
    /// Gets or sets the name of the property
    /// </summary>
    public string Name { get; set; }= string.Empty;
    /// <summary>
    /// Gets or sets the type of the property
    /// </summary>
    public FlowFieldType Type { get; set; }
    /// <summary>
    /// Gets or sets an optional description for this field
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the strong name of the flow element field this value will set
    /// </summary>
    public string FlowElementField { get; set; }
    /// <summary>
    /// Gets or sets list options for a select input
    /// </summary>
    public string[] Options { get; set; }
    /// <summary>
    /// Gets or sets if this is required
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// Gets or sets the default value
    /// </summary>
    public object DefaultValue { get; set; }
    /// <summary>
    /// Gets or sets an optional suffix
    /// </summary>
    public string Suffix { get; set; }
    /// <summary>
    /// Gets or sets the minimum value for int values
    /// </summary>
    public int IntMinimum { get; set; }
    /// <summary>
    /// Gets or sets the maximum value for int values
    /// </summary>
    public int IntMaximum { get; set; }

    /// <summary>
    /// Gets or sets if the value input should be inversed
    /// </summary>
    public bool Inverse { get; set; }
    
    /// <summary>
    /// Gets or sets if this property is only shown if the If condition matches
    /// </summary>
    public string IfName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value of the if condition
    /// </summary>
    public string IfValue { get; set; }= string.Empty;
    /// <summary>
    /// Gets or sets if the if condition is inversed
    /// </summary>
    public bool IfNot { get; set; }
}