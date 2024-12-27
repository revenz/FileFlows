namespace FileFlows.Shared.Models;

/// <summary>
/// Reseller Settings
/// </summary>
public class ResellerSettings : FileFlowObject
{
    /// <summary>
    /// Gets or sets the custom port to run the reseller app on
    /// </summary>
    public int? CustomPort { get; set; }
    /// <summary>
    /// Gets or sets the client secret for Google single sign on
    /// </summary>
    [Encrypted]
    public string GoogleClientId { get; set; }
    /// <summary>
    /// Gets or sets the client secret for Google single sign on
    /// </summary>
    [Encrypted]
    public string GoogleClientSecret { get; set; }
    /// <summary>
    /// Gets or sets the client id for Microsoft single sign on
    /// </summary>
    [Encrypted]
    public string MicrosoftClientId { get; set; }
    /// <summary>
    /// Gets or sets the client secret for Microsoft single sign on
    /// </summary>
    [Encrypted]
    public string MicrosoftClientSecret { get; set; }
    
    /// <summary>
    /// Gets or sets the custom authentication authority address
    /// </summary>
    public string? CustomProviderName { get; set; }
    /// <summary>
    /// Gets or sets the custom authentication authority address
    /// </summary>
    public string? CustomProviderAuthority { get; set; }
    /// <summary>
    /// Gets or sets the custom authentication client id
    /// </summary>
    [Encrypted]
    public string CustomProviderClientId { get; set; }
    /// <summary>
    /// Gets or sets the custom authentication client secret
    /// </summary>
    [Encrypted]
    public string? CustomProviderClientSecret { get; set; }
}