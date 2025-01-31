using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A model for a flow templates
/// </summary>
public class FlowTemplateModel
{
    /// <summary>
    /// Gets or sets the path of the script
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets an optional icon
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revision of the script
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the author of this object
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// Gets or sets the minimum version of FileFlows required for this object
    /// </summary>
    public string MinimumVersion { get; set; }

    /// <summary>
    /// Gets or sets tags for this object
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets plugins used by this object
    /// </summary>
    public List<string> Plugins { get; set; }

    /// <summary>
    /// Gets or sets scripts used by this object
    /// </summary>
    public List<string> Scripts { get; set; }

    /// <summary>
    /// Gets or sets the type of flow
    /// </summary>
    public FlowType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the fields in this template
    /// </summary>
    public List<TemplateField> Fields { get; set; }
    
    /// <summary>
    /// Gets or sets the actual flow
    /// </summary>
    public Flow Flow { get; set; }
    
    /// <summary>
    /// Gets or sets a list of missing dependencies for this template
    /// </summary>
    public List<string> MissingDependencies { get; set; }
}

/// <summary>
/// A field used in templates
/// </summary>
public class TemplateField
{
    /// <summary>
    /// Gets or sets the UID of the target to set for this template field
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the field
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// Gets or sets if this field is required
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// Gets or sets the name of this field
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the label for this field
    /// </summary>
    public string? Label { get; set; }
    
    /// <summary>
    /// Gets or sets the help text for this field
    /// </summary>
    public string? Help { get; set; }
    
    /// <summary>
    /// Gets or sets an optional suffix
    /// </summary>
    public string? Suffix { get; set; }
    
    /// <summary>
    /// Gets or sets the default value for this field
    /// </summary>
    public object? Default { get; set; }
    
    /// <summary>
    /// Gets or sets the value of this field
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the parameters for this field
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// Gets or sets thw conditions of the field
    /// </summary>
    public List<Condition>? Conditions { get; set; }
}

/// <summary>
/// Model used in the flow list page
/// </summary>
public class FlowListModel: IInUse, IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of flow
    /// </summary>
    public FlowType Type { get; set; }

    /// <summary>
    /// Gets or sets if this is the default failure flow
    /// </summary>
    public bool Default { get; set; }
    
    /// <summary>
    /// Gets or sets what is using this model
    /// </summary>
    public List<ObjectReference> UsedBy { get; set; }
    
    /// <summary>
    /// Gets or sets if this flow is read only
    /// </summary>
    public bool ReadOnly { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the flow
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the Icon of the flow
    /// </summary>
    public string Icon { get; set; }
}