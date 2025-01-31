namespace FileFlows.Shared.Models;

using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;
using FileFlows.Plugin;

/// <summary>
/// A flow element/node is a element that is a part of the flow
/// </summary>
public class FlowElement
{
    /// <summary>
    /// Gets or sets the UID of the element
    /// </summary>
    public string Uid { get; set; }
    /// <summary>
    /// Gets or sets the name of the element
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets if a flow element is read only
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a description for this element, only used by scripts where the description is user defined 
    /// </summary>
    public string Description { get; set; }

    static readonly Regex rgxFormatLabel = new Regex("(?<=[A-Za-z])(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[0-9]?[A-Z])");

    private string _DisplayName;
    /// <summary>
    /// Gets or sets the display name of the element
    /// </summary>
    public string DisplayName
    {
        get {
            if (_DisplayName == null && Translater.InitDone)
            {
                _DisplayName = FormatName(this.Name);
            }
            return _DisplayName;
        }
    }

    /// <summary>
    /// Formats the name and translated if needed
    /// </summary>
    /// <param name="name">the name to format</param>
    /// <returns>the formatted name</returns>
    public static string FormatName(string name)
    {
        if (Translater.CanTranslate($"Flow.Parts.{name}.Label", out var translation))
            return translation;

        name = name[(name.LastIndexOf('.') + 1)..];
        string dn = name.Replace("_", " ");
        dn = rgxFormatLabel.Replace(dn, " ");
        return dn;
    }

    /// <summary>
    /// Gets or sets the icon of the element
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets any variables this element exports
    /// </summary>
    public Dictionary<string, object> Variables { get; set; }

    /// <summary>
    /// Gets or sets if no editor should be shown to the user when first adding this element
    /// </summary>
    public bool NoEditorOnAdd { get; set; }

    /// <summary>
    /// Gets or sets the number of inputs this element has
    /// </summary>
    public int Inputs { get; set; }
    /// <summary>
    /// Gets or sets the number of outputs this element has
    /// </summary>
    public int Outputs { get; set; }
    /// <summary>
    /// Gets or sets the url to the the help page for this node
    /// </summary>
    public string HelpUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the type of flow element
    /// </summary>
    public FlowElementType Type { get; set; }

    /// <summary>
    /// Gets or sets if this element is available to failure flows
    /// </summary>
    public bool FailureNode { get; set; }

    /// <summary>
    /// Gets or sets the group this element belongs to
    /// </summary>
    public string Group { get; set; }
    /// <summary>
    /// Gets or sets the output labels for this element
    /// </summary>

    public List<string> OutputLabels { get; set; }

    /// <summary>
    /// Gets or sets the fields this element has
    /// </summary>
    public List<ElementField> Fields { get; set; }

    /// <summary>
    /// Gets or sets the model for this element
    /// </summary>
    public ExpandoObject? Model { get; set; }

    /// <summary>
    /// Gets or sets if this node is obsolete and should be phased out
    /// </summary>
    public virtual bool Obsolete { get; set; }

    /// <summary>
    /// Gets or sets a message to show when the user tries to use this obsolete node
    /// </summary>
    public virtual string ObsoleteMessage { get; set; }
    
    /// <summary>
    /// Gets or sets if the license required by this flow element
    /// </summary>
    public virtual LicenseLevel LicenseLevel { get; set; }
    
    /// <summary>
    /// Gets an optional custom color to show
    /// </summary>
    public virtual string CustomColor { get; set; }
}