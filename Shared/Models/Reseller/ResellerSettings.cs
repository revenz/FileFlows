namespace FileFlows.Shared.Models;

/// <summary>
/// Reseller Settings
/// </summary>
public class ResellerSettings : FileFlowObject
{
    /// <summary>
    /// Gets or sets the URL to purchase tokens from
    /// </summary>
    public string TokenPurchaseUrl { get; set; }
    
    /// <summary>
    /// Gets or set the home page HTML for logged in users
    /// </summary>
    public string HomePageHtml { get; set; }
    
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

    /// <summary>
    /// Gets or sets if the token URL should open in a popup or a new tab
    /// </summary>
    public bool OpenUrlInPopup { get; set; }
    
    /// <summary>
    /// Gets or sets if auto tokens will be given to users
    /// </summary>
    public bool AutoTokens { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tokens to give to each user
    /// </summary>
    public int AutoTokensAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum amount of tokens a user can have after receiving the auto tokens
    /// </summary>
    public int AutoTokensMaximum { get; set; }
    
    /// <summary>
    /// Gets or sets the auto tokens period
    /// </summary>
    public int AutoTokensPeriodMinutes { get; set; }
}