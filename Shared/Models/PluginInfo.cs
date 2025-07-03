using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

using System.Collections.Generic;


/// <summary>
/// Information about a plugin in FileFlows
/// </summary>
public class PluginInfo : FileFlowObject
{
    /// <summary>
    /// Gets or sets the plugin version
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets if the plugin is deleted
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets if this plugin has settings
    /// </summary>
    public bool HasSettings { get; set; }

    /// <summary>
    /// Gets or sets the URL of this plugin
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// Gets or sets the authors of the plugin
    /// </summary>
    public string Authors { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the plugin
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum version of FileFlows this plugin requires
    /// </summary>
    public string MinimumVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the package of this plugin.
    /// The packages name is the .ffplugin file
    /// </summary>
    public string PackageName { get; set; }
    
    /// <summary>
    /// Gets or sets a list of settings elements used to construct the settings form
    /// </summary>
    public List<ElementField> Settings { get; set; }
    
    /// <summary>
    /// Gets or sets a list of elements/flowparts/nodes this plugin has
    /// </summary>
    public List<FlowElement> Elements { get; set; }
    
    /// <summary>
    /// Gets or sets the icon for the plugins
    /// </summary>
    public string Icon { get; set; }
}

/// <summary>
/// Gets or sets the model for plugin info
/// </summary>
public class PluginInfoModel : PluginInfo, IInUse
{
    /// <summary>
    /// Gets or sets the latest version of this plugin
    /// </summary>
    public string LatestVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the number of elements
    /// </summary>
    public int NumberOfElements { get; set; }

    /// <summary>
    /// Gets if there is an update available for this plugin
    /// </summary>
    public bool UpdateAvailable
    {
        get
        {
            if (string.IsNullOrEmpty(LatestVersion) || System.Version.TryParse(LatestVersion, out Version? latest) == false)
                return false;
            if (string.IsNullOrEmpty(Version) || System.Version.TryParse(Version, out Version? current) == false)
                return false;
            return current < latest;
        }
    }

    /// <summary>
    /// Gets or sets if the plugin is in use
    /// </summary>
    public List<ObjectReference> UsedBy { get; set; }
}