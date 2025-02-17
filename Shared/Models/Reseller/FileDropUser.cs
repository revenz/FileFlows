namespace FileFlows.Shared.Models;

/// <summary>
/// File Drop User
/// </summary>
public class FileDropUser : FileFlowObject
{
    /// <summary>
    /// Gets or sets the provider of this user
    /// </summary>
    public string Provider { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of this user from the provider
    /// </summary>
    public string ProviderUid { get; set; }
    
    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the password hash, used by Forms users
    /// </summary>
    public string PasswordHash { get; set; }
    
    /// <summary>
    /// Gets or sets if this user is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets how many tokens this user currently has
    /// </summary>
    public int Tokens { get; set; }
    
    /// <summary>
    /// Gets or sets the users pictures from the source
    /// </summary>
    public string Picture { get; set; }
    
    /// <summary>
    /// Gets or sets the users picture as a base64 encoded image
    /// </summary>
    public string PictureBase64 { get; set; }

    /// <summary>
    /// Gets or sets the last time auto tokens were given to this user
    /// </summary>
    public DateTime LastAutoTokensUtc { get; set; }
}