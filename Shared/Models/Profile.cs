using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A users profile
/// </summary>
public class Profile
{
    /// <summary>
    /// Gets or sets the users Uid
    /// </summary>
    public Guid? Uid { get; set; }
    /// <summary>
    /// Gets or sets the users name
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the users email
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Gets or sets the users role
    /// </summary>
    public UserRole Role { get; set; }
    /// <summary>
    /// Gets or sets the language
    /// </summary>
    public string Language { get; set; }
    
    /// <summary>
    /// Gets or sets the license
    /// </summary>
    public LicenseFlags License { get; set; }
    
    /// <summary>
    /// Gets or sets the license level
    /// </summary>
    public FileFlows.Common.LicenseLevel LicenseLevel { get; set; }
    
    /// <summary>
    /// Gets or sets if in a webview
    /// </summary>
    public bool IsWebView { get; set; }
    
    /// <summary>
    /// Gets or sets the server operating system type
    /// </summary>
    public OperatingSystemType ServerOS { get; set; }

    /// <summary>
    /// Gets or sets the security mode currently in use
    /// </summary>
    public SecurityMode Security { get; set; }
    
    /// <summary>
    /// Gets or sets the current configuration status
    /// </summary>
    public ConfigurationStatus ConfigurationStatus { get; set; }

    /// <summary>
    /// Gets or sets the number of unread notifications
    /// </summary>
    public UnreadNotifications UnreadNotifications { get; set; }

    /// <summary>
    /// Gets or sets the language options
    /// </summary>
    public List<ListOption> LanguageOptions { get; set; } = new();
    
    

    /// <summary>
    /// Checks if this profile has a role
    /// </summary>
    /// <param name="role">the role to check</param>
    /// <returns>true if they have this role</returns>
    public bool HasRole(UserRole role)
        => (Role & role) == role;

    /// <summary>
    /// Gets if the user is licensed for a feature
    /// </summary>
    /// <param name="flag">the flag</param>
    /// <returns>true if licensed, otherwise false</returns>
    public bool LicensedFor(LicenseFlags flag)
        => (License & flag) == flag;

    /// <summary>
    /// Gets if this user is an administrator
    /// </summary>
    [JsonIgnore]
    public bool IsAdmin => Role == UserRole.Admin;

    /// <summary>
    /// Gets or sets if users are enabled and in use
    /// </summary>
    public bool UsersEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets if there are any Docker instances in this setup
    /// </summary>
    public bool HasDockerInstances { get; set; }
    
    /// <summary>
    /// Gets or sets if the French language should be used
    /// </summary>
    public bool UseFrench => string.IsNullOrWhiteSpace(Language) && string.Equals(Language, "fr", StringComparison.InvariantCultureIgnoreCase);
    /// <summary>
    /// Gets or sets if the German language should be used
    /// </summary>
    public bool UseGerman => string.IsNullOrWhiteSpace(Language) && string.Equals(Language, "de", StringComparison.InvariantCultureIgnoreCase);

}
/// <summary>
/// Represents the unread notifications containing critical, error, and warning counts.
/// </summary>
public class UnreadNotifications
{
    /// <summary>
    /// Gets the count of critical notifications.
    /// </summary>
    public int Critical { get; init; }

    /// <summary>
    /// Gets the count of error notifications.
    /// </summary>
    public int Error { get; init; }

    /// <summary>
    /// Gets the count of warning notifications.
    /// </summary>
    public int Warning { get; init; }

    /// <summary>
    /// Gets the total number of unread notifications.
    /// </summary>
    [JsonIgnore]
    public int Total => Critical + Error + Warning;
}
