namespace FileFlows.Shared.Models;

/// <summary>
/// The system status of FileFlows
/// </summary>
public class FileFlowsStatus
{
    /// <summary>
    /// Gets or sets the configuration status of FileFlows
    /// </summary>
    public ConfigurationStatus ConfigurationStatus { get; set; }

    /// <summary>
    /// Gets or sets if FileFlows is using an external database
    /// </summary>
    public bool ExternalDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets if FileFlows is licensed
    /// </summary>
    public bool Licensed { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for custom dashboards
    /// </summary>
    public bool LicenseDashboards { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for revisions
    /// </summary>
    public bool LicenseRevisions { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for external databases
    /// </summary>
    public bool LicenseExternalDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for tasks
    /// </summary>
    public bool LicenseTasks { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for webhooks
    /// </summary>
    public bool LicenseWebhooks { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for user security
    /// </summary>
    public bool LicenseUserSecurity { get; set; }
    
    /// <summary>
    /// Gets or sets the security
    /// </summary>
    public SecurityMode Security { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for auto updates
    /// </summary>
    public bool LicenseAutoUpdates { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for library processing order
    /// </summary>
    public bool LicenseProcessingOrder { get; set; }
    
    /// <summary>
    /// Gets or sets if license allowed for file server
    /// </summary>
    public bool LicenseFileServer { get; set; }
    
    /// <summary>
    /// Gets or sets if the user has a enterprise license
    /// </summary>
    public bool LicenseEnterprise { get; set; }

    /// <summary>
    /// Gets or sets if this is a windows based system
    /// </summary>
    public bool IsWindows { get; set; }
    /// <summary>
    /// Gets or sets if this is a windows based system
    /// </summary>
    public bool IsLinux { get; set; }
    /// <summary>
    /// Gets or sets if this is a windows based system
    /// </summary>
    public bool IsMacOS { get; set; }
    /// <summary>
    /// Gets or sets if this is a windows based system
    /// </summary>
    public bool IsDocker { get; set; }
    /// <summary>
    /// Gets or sets if this is a web view 
    /// </summary>
    public bool IsWebView { get; set; }
    
    /// <summary>
    /// Gets or sets if the user is an admin
    /// </summary>
    public bool IsAdmin { get; set; }
    
    /// <summary>
    /// Gets or sets if the change password should be shown
    /// </summary>
    public bool ShowChangePassword { get; set; }
    
    /// <summary>
    /// Gets or sets if the logout link should be shown
    /// </summary>
    public bool ShowLogout { get; set; }
}

/// <summary>
/// The configuration status of the system
/// </summary>
[Flags]
public enum ConfigurationStatus
{
    /// <summary>
    /// Initial configuration done
    /// </summary>
    InitialConfig = 1,
    /// <summary>
    /// The EULA has been accepted
    /// </summary>
    EulaAccepted = 2,
    /// <summary>
    /// Flows are configured
    /// </summary>
    Flows = 4,
    /// <summary>
    /// Libraries are configured
    /// </summary>
    Libraries = 8,
    /// <summary>
    /// Users are configured
    /// </summary>
    Users = 16
}