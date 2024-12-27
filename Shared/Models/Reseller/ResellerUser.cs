namespace FileFlows.Shared.Models;

/// <summary>
/// Reseller User
/// </summary>
public class ResellerUser : FileFlowObject
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
    /// Gets or sets the email address of the user
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Gets or sets how many tokens this user currently has
    /// </summary>
    public int Tokens { get; set; }
    
    /// <summary>
    /// Gets or sets the users pictures
    /// </summary>
    public string Picture { get; set; }
}