namespace FileFlows.Shared.Models;

using System;
using System.Collections.Generic;
using System.Dynamic;
using FileFlows.Plugin;

/// <summary>
/// A flow part is a part/node of a flow that executes
/// </summary>
public class FlowPart 
{
    /// <summary>
    /// Gets or sets the UID of the flow part
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the flow part
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets a user overriden color for this flow part
    /// </summary>
    public string Color { get; set; }
    
    /// <summary>
    /// Gets or sets if this flow part is read only
    /// </summary>
    [DbIgnore]
    public bool ReadOnly { get; set; }
    
    /// <summary>
    /// Gets or sets the FlowElementUid this flow part is an instance of
    /// This is the full name of the flow element, Namespace.TypeName
    /// </summary>
    public string FlowElementUid { get; set; }
    
    /// <summary>
    /// Gets or sets the x coordinate where this part appears on the canvas
    /// </summary>
    public float xPos { get; set; }
    
    /// <summary>
    /// Gets or sets the y coordinate where this part appears on the canvas
    /// </summary>
    public float yPos { get; set; }

    /// <summary>
    /// Gets or sets the icon of the flow part
    /// </summary>
    [DbIgnore]
    public string Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the label of this flow part
    /// </summary>
    public string Label { get; set; }
    
    /// <summary>
    /// Gets or sets the number of inputs this part has
    /// </summary>
    public int Inputs { get; set; }
    
    /// <summary>
    /// Gets or sets the number of outputs this part has
    /// </summary>
    public int Outputs { get; set; }
    
    /// <summary>
    /// Gets or sets an optional custom color for the flow part
    /// </summary>
    [DbIgnore]
    public string? CustomColor { get; set; }

    /// <summary>
    /// Gets or sets the output connections of this flow part
    /// </summary>
    public List<FlowConnection> OutputConnections { get; set; }
    
    /// <summary>
    /// Gets or sets teh error connection to process if this flow element returns a -1
    /// </summary>
    public FlowConnection? ErrorConnection { get; set; }

    /// <summary>
    /// Gets or sets the type of the flow part
    /// </summary>
    public FlowElementType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the model of this flow part
    /// </summary>
    public ExpandoObject Model { get; set; }
}

/// <summary>
/// A flow connection connects the input of a node to the output of another node
/// </summary>
public class FlowConnection
{
    /// <summary>
    /// Gets or sets the Input number of the connecting node
    /// </summary>
    public int Input { get; set; }
    
    /// <summary>
    /// Gets or sets the Output number of the connecitng node
    /// </summary>
    public int Output { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of input node this connection connects to
    /// </summary>
    public Guid InputNode { get; set; }
}