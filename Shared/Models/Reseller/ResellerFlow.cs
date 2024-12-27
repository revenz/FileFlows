using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A reseller flow
/// </summary>
public class ResellerFlow : FileFlowObject
{
    /// <summary>
    /// Gets or sets the description of the flow
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the flow this library uses
    /// </summary>
    public ObjectReference Flow { get; set; }
    
    /// <summary>
    /// Gets or sets the Icon of the flow
    /// </summary>
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the extensions allowed for this flow
    /// </summary>
    public string[] Extensions { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the maximum file size allowed for this flow
    /// </summary>
    public long MaxFileSize { get; set; }
    
    /// <summary>
    /// Gets or sets how many tokens this cost to run
    /// </summary>
    public int Tokens { get; set; }
    
    /// <summary>
    /// Gets or sets if this flow is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Preview mode 
    /// </summary>
    public ResellerPreviewMode PreviewMode { get; set; }
}

/// <summary>
/// Reseller preview mode
/// </summary>
public enum ResellerPreviewMode
{
    /// <summary>
    /// Standard list
    /// </summary>
    List = 0,
    /// <summary>
    /// Image list
    /// </summary>
    Images = 1,
}