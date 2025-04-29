namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// License model
/// </summary>
public class LicenseModel
{
    /// <summary>
    /// Gets or sets the license email
    /// </summary>
    public string LicenseEmail { get; set; }

    /// <summary>
    /// Gets or sets the licensed key
    /// </summary>
    public string LicenseKey { get; set; }
    
    /// <summary>
    /// Gets or sets the number of they can process
    /// </summary>
    public string LicenseFiles { get; set; }

    /// <summary>
    /// Gets the license flags for the user
    /// </summary>
    public LicenseFlags LicenseFlags { get; set; }
    /// <summary>
    /// Gets the license level for the user
    /// </summary>
    public LicenseLevel LicenseLevel { get; set; }
    /// <summary>
    /// Gets or sets the number of file drop users they are licensed for
    /// </summary>
    public int LicensedFileDropUsers { get; set; }
    /// <summary>
    /// Gets the license expiry date for the user
    /// </summary>
    public DateTime LicenseExpiryDate { get; set; }
    /// <summary>
    /// Gets the licensed processing nodes for the user
    /// </summary>
    public int LicenseProcessingNodes { get; set; }
    /// <summary>
    /// Gets the licensed status for the user
    /// </summary>
    public string LicenseStatus { get; set; }
}