namespace FileFlows.Shared.Models;

/// <summary>
/// Settings for FileFlows
/// </summary>
public class Settings : FileFlowObject
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
    
    /// <summary>
    /// Gets or sets the language code for the system
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets if telemetry should be disabled
    /// </summary>
    public bool DisableTelemetry { get; set; }
    
    /// <summary>
    /// Gets or sets the number of seconds to check for a new file to process
    /// </summary>
    public int ProcessFileCheckInterval { get; set; }

    /// <summary>
    /// Gets or sets if temporary files from a failed flow should be kept
    /// </summary>
    public bool KeepFailedFlowTempFiles { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files should not be used when moving/copying files
    /// </summary>
    public bool DontUseTempFilesWhenMovingOrCopying { get; set; }

    /// <summary>
    /// Gets or sets if the Queue messages should be logged
    /// </summary>
    public bool LogQueueMessages { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for file added should be shown
    /// </summary>
    public bool ShowFileAddedNotifications { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for processing started added should not be shown
    /// </summary>
    public bool HideProcessingStartedNotifications { get; set; }
    
    /// <summary>
    /// Gets or sets if the notifications for processing finished added should not be shown
    /// </summary>
    public bool HideProcessingFinishedNotifications { get; set; }

    /// <summary>
    /// Gets or sets the revision of the configuration
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets if this is running on Windows
    /// </summary>
    public bool IsWindows { get; set; }
    
    /// <summary>
    /// Gets or sets if this is running inside Docker
    /// </summary>
    public bool IsDocker { get; set; }

    /// <summary>
    /// Gets or sets if when the system is paused until
    /// </summary>
    public DateTime PausedUntil { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets if the system is paused
    /// </summary>
    public bool IsPaused => DateTime.UtcNow < PausedUntil;

    /// <summary>
    /// Gets or sets the number of log files to keep
    /// </summary>
    public int LogFileRetention { get; set; }
    
    /// <summary>
    /// Gets or sets if every request to the server should be logged
    /// </summary>
    public bool LogEveryRequest { get; set; }
    
    /// <summary>
    /// Gets or sets if the file server is disabled
    /// </summary>
    public bool FileServerDisabled { get; set; }
    
    /// <summary>
    /// Gets or sets the file permissions to set on the file
    /// Only used on Unix based systems
    /// </summary>
    public int FileServerFilePermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the folder permissions to set on the folders
    /// Only used on Unix based systems
    /// </summary>
    public int FileServerFolderPermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the owner group to use
    /// </summary>
    public string FileServerOwnerGroup { get; set; }
    
    /// <summary>
    /// Gets or sets the allowed paths for the file server
    /// </summary>
    public string[] FileServerAllowedPaths { get; set; }
    
    /// <summary>
    /// Gets or sets the API token processing nodes will use to connect
    /// </summary>
    [Encrypted]
    public string AccessToken { get; set; }
    
    /// <summary>
    /// Gets or sets the email server address
    /// </summary>
    [Encrypted]
    public string SmtpServer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the email server port
    /// </summary>
    public int SmtpPort { get; set; }
    /// <summary>
    /// Gets or sets the email server security
    /// </summary>
    public EmailSecurity SmtpSecurity { get; set; }

    /// <summary>
    /// Gets or sets the name this is sent from
    /// </summary>
    [Encrypted]
    public string SmtpFrom { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address this is sent from
    /// </summary>
    [Encrypted]
    public string SmtpFromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email server user
    /// </summary>
    [Encrypted]
    public string SmtpUser { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the email server password
    /// </summary>
    [Encrypted]
    public string SmtpPassword { get; set; } = string.Empty;
    
    
    /// <summary>
    /// Gets or sets the open ID connect authority
    /// </summary>
    [Encrypted]
    public string OidcAuthority { get; set; }
    /// <summary>
    /// Gets or sets the open ID connect client ID
    /// </summary>
    [Encrypted]
    public string OidcClientId { get; set; }
    /// <summary>
    /// Gets or sets the open ID connect client secret
    /// </summary>
    [Encrypted]
    public string OidcClientSecret { get; set; }
    /// <summary>
    /// Gets or sets an optional open ID connect client callback address, if this is not set the originating address will be used
    /// </summary>
    [Encrypted]
    public string OidcCallbackAddress { get; set; }
    
    /// <summary>
    /// Gets or sets the token expiry in minutes
    /// </summary>
    public int TokenExpiryMinutes { get; set; }
    
    /// <summary>
    /// Gets or sets the max login attempts before locking out the user
    /// </summary>
    public int LoginMaxAttempts { get; set; }
    
    /// <summary>
    /// Gets or sets the duration to lockout the user for
    /// </summary>
    public int LoginLockoutMinutes { get; set; }

    /// <summary>
    /// Gets or sets if the initial configuration is done
    /// </summary>
    public bool InitialConfigDone { get; set; }

    /// <summary>
    /// Gets or sets if the EULA has been accepted
    /// </summary>
    public bool EulaAccepted { get; set; }

    /// <summary>
    /// Gets the delay between requesting a new file if a file can be processed instantly
    /// </summary>
    public int DelayBetweenNextFile { get; set; }
}

/// <summary>
/// The types of Databases supported
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQLite Database
    /// </summary>
    Sqlite = 0,
    /// <summary>
    /// MySql / MariaDB
    /// </summary>
    MySql = 1,
    /// <summary>
    /// Postgres
    /// </summary>
    Postgres = 2,
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer = 3,
    /// <summary>
    /// SQLite but without a cached connection
    /// </summary>
    SqliteNonCached = 11,
    /// <summary>
    /// SQLite but with a new connection each time
    /// </summary>
    SqlitePooledConnection = 12,
}

/// <summary>
/// The types of email security
/// </summary>
public enum EmailSecurity
{
    /// <summary>
    /// no security
    /// </summary>
    None = 0,
    /// <summary>
    /// Auto security
    /// </summary>
    Auto = 1,
    /// <summary>
    /// SSL security
    /// </summary>
    SSL = 2,
    /// <summary>
    /// TlS security
    /// </summary>
    TLS = 3,
}