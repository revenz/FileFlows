namespace FileFlows.Common;

/// <summary>
/// The License
/// </summary>
public class License
{
    /// <summary>
    /// Gets or sets the license status
    /// </summary>
    public LicenseStatus Status { get; set; }
    /// <summary>
    /// Gets or sets the license expiry date in UTC time
    /// </summary>
    public DateTime ExpirationDateUtc { get; init; }
    /// <summary>
    /// Gets or sets the license flags
    /// </summary>
    public LicenseFlags Flags { get; init; }
    /// <summary>
    /// Gets or sets the license level
    /// </summary>
    public LicenseLevel Level { get; init; }
    /// <summary>
    /// Gets or sets the number of files this license can process
    /// </summary>
    public int Files { get; init; }
    /// <summary>
    /// Gets or sets the number of processing nodes
    /// </summary>
    public int ProcessingNodes { get; init; }

    /// <summary>
    /// Gets the default license
    /// </summary>
    /// <returns>the default license</returns>
    internal static License DefaultLicense() => new License
    {
        Status = LicenseStatus.Unlicensed,
        ProcessingNodes = 2
    };
}