using FileFlows.Plugin.Models;

namespace FileFlows.Plugin;

/// <summary>
/// A FileFlows Node that can be used in a Flow
/// </summary>
public class Node
{
    /// <summary>
    /// Gets the type of node 
    /// </summary>
    public virtual FlowElementType Type { get; }

    /// <summary>
    /// Gets the license level required for this plugin
    /// </summary>
    public virtual LicenseLevel LicenseLevel => LicenseLevel.Free;

    /// <summary>
    /// Gets if this node is obsolete and should be phased out
    /// </summary>
    public virtual bool Obsolete => false;

    /// <summary>
    /// Gets a message to show when the user tries to use this obsolete node
    /// </summary>
    public virtual string ObsoleteMessage => null;
    
    /// <summary>
    /// Gets the number of inputs this node has
    /// </summary>
    public virtual int Inputs { get; }
    /// <summary>
    /// Gets the number of outputs this node has
    /// </summary>
    public virtual int Outputs { get; }

    /// <summary>
    /// Gets the name of this node
    /// </summary>
    public string Name => base.GetType().Name;

    /// <summary>
    /// Gets if this node can be used in a failure node
    /// </summary>
    public virtual bool FailureNode => false;

    /// <summary>
    /// Get the help URL for this node, will show a help button if set
    /// </summary>
    public virtual string HelpUrl => string.Empty;

    /// <summary>
    /// Get variables that can be used in other nodes such as a renamer node using variables to create a filename using variables from a previous node
    /// </summary>
    public virtual Dictionary<string, object> Variables => new Dictionary<string, object>();

    /// <summary>
    /// Gets the fontawesome icon to use in the flow 
    /// </summary>
    public virtual string Icon => string.Empty;

    /// <summary>
    /// Gets if no editor should be shown when adding this node to a flow
    /// </summary>
    public virtual bool NoEditorOnAdd => false;

    /// <summary>
    /// Gets an optional custom color to show
    /// </summary>
    public virtual string CustomColor => null;

    /// <summary>
    /// Gets the group this node belongs to
    /// </summary>
    public virtual string Group
    {
        get
        {
            var type = base.GetType();
            if (type == null || type.FullName == null) return string.Empty;
            string group = type.FullName.Substring(0, type.FullName.LastIndexOf("."));
            return group.Substring(group.LastIndexOf(".") + 1);
        }
    }

    /// <summary>
    /// Called directly before Execute method
    /// </summary>
    /// <param name="args">the node parameter args</param>
    /// <returns>true if pre-execute is successful, otherwise will exit the flow</returns>
    public virtual bool PreExecute(NodeParameters args) => true;

    /// <summary>
    /// Executes the node
    /// </summary>
    /// <param name="args">the arguments passed into the node</param>
    /// <returns>the number of the output node to call next, this is 1 based</returns>
    public virtual int Execute(NodeParameters args)
    {
        if (Outputs > 0)
            return 1;
        return 0;
    }

    /// <summary>
    /// Cancels the node
    /// </summary>
    /// <returns>cancels the node</returns>
    public virtual Task Cancel() => Task.CompletedTask;

    /// <summary>
    /// Gets tab definitions for this flow element
    /// </summary>
    public virtual TabDefinition[]? Tabs => null;
}