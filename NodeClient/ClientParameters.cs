using FileFlows.ServerShared.Models;

namespace FileFlows.NodeClient;


/// <summary>
/// The client paraemeters
/// </summary>
public class ClientParameters
{
    /// <summary>
    /// Gets or sets the Server URL
    /// </summary>
    public string ServerUrl { get; set; }
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string Hostname  { get; set; }
    /// <summary>
    /// Gets or sets the access token
    /// </summary>
    public string AccessToken { get; set; }
    
    /// <summary>
    /// Gets or sets a forced temporary path
    /// </summary>
    public string? ForcedTempPath { get; set; }

    /// <summary>
    /// Gets or sets mappings passed in via enviromental values
    /// </summary>
    public List<RegisterModelMapping>? EnvironmentalMappings { get; set; }
}


/// <summary>
/// Connection state
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Disconnected from server
    /// </summary>
    Disconnected,
    /// <summary>
    /// Connected to server
    /// </summary>
    Connected,
    /// <summary>
    /// Connection is connecting
    /// </summary>
    Connecting,
}