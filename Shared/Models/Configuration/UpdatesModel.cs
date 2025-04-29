namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// Updates model
/// </summary>
public class UpdatesModel
{
    /// <summary>
    /// Gets or sets if plugins should automatically be updated when new version are available online
    /// </summary>
    public bool AutoUpdatePlugins { get; set; }

    /// <summary>
    /// Gets or sets if the server should automatically update when a new version is available online
    /// </summary>
    public bool AutoUpdate { get; set; }

    /// <summary>
    /// Gets or sets if nodes should be automatically updated when the server is updated
    /// </summary>
    public bool AutoUpdateNodes { get; set; }
}