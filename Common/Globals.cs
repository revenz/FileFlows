using System.Runtime.InteropServices;

namespace FileFlows.Common;

/// <summary>
/// Globals variables
/// </summary>
public class Globals
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    #if(DEBUG)
    public static readonly string Version = DateTime.UtcNow.ToString("yy.MM") + ".9.9999";
    #else
    public const string Version = "23.10.2.2469";
    #endif

    /// <summary>
    /// The minimum supported node version
    /// </summary>
    public static readonly Version MinimumNodeVersion = new (Version);

    /// <summary>
    /// Gets or sets if this is running in development
    /// </summary>
    public static bool IsDevelopment { get; set; }

    /// <summary>
    /// Gets if this is running on Windows
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets if this is running on linux
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets if this is running on Mac
    /// </summary>
    public static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Gets if this is running on FreeBSD
    /// </summary>
    public static bool IsFreeBsd => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    /// <summary>
    /// Gets if this is running on an ARM CPU
    /// </summary>
    public static bool IsArm => RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64;

    /// <summary>
    /// Gets or sets if this node is running inside a docker container
    /// </summary>
    public static bool IsDocker { get; set; }

    /// <summary>
    /// Gets or sets if this is running on the node or the server
    /// </summary>
    public static bool IsNode { get; set; }

    /// <summary>
    /// Gets or sets if this node is running as a systemd service
    /// </summary>
    public static bool IsSystemd { get; set; }

    /// <summary>
    /// Gets or sets if unit testing
    /// </summary>
#if(DEBUG)
    public static bool IsUnitTesting { get; set; }
#else
    public static bool IsUnitTesting => false;
#endif
    
    /// <summary>
    /// Gets or sets if the web view is being used
    /// </summary>
    public static bool UsingWebView { get; set; }

    /// <summary>
    /// Gets or sets the server url
    /// </summary>
    public static string ServerUrl { get; set; } = string.Empty;
    

    /// <summary>
    /// The UID for the flow failure input
    /// </summary>
    public const string FlowFailureInputUid = "FileFlows.BasicNodes.FlowFailure";

    /// <summary>
    /// A custom URL for FileFlows.com
    /// </summary>
    public static string? CustomFileFlowsDotComUrl;

    /// <summary>
    /// The URL for fileflows.com
    /// </summary>
    public static string FileFlowsDotComUrl => (CustomFileFlowsDotComUrl?.EmptyAsNull() ??
                                                Environment.GetEnvironmentVariable("FFURL")?.EmptyAsNull() ??
                                                "https://fileflows.com").TrimEnd('/');

    /// <summary>
    /// The Default permissions for files on a unix like system
    /// </summary>
    public const int DefaultPermissionsFile = 644;
    /// <summary>
    /// The Default permissions for folder on a unix like system
    /// </summary>
    public const int DefaultPermissionsFolder = 755;

    /// <summary>
    /// The base url for Plugin 
    /// </summary>
    public static string PluginBaseUrl => FileFlowsDotComUrl + "/api/plugin";

    /// <summary>
    /// The processing time heat map statistic
    /// </summary>
    public const string STAT_PROCESSING_TIMES_HEATMAP = "PROCESSING_TIMES_HEATMAP";

    /// <summary>
    /// The processing storage saved statistic
    /// </summary>
    public const string STAT_STORAGE_SAVED = "STORAGE_SAVED";
    /// <summary>
    /// The processing storage saved statistic for the month
    /// </summary>
    public const string STAT_STORAGE_SAVED_MONTH = "STORAGE_SAVED_MONTH";

    /// <summary>
    /// The processing total files statistic
    /// </summary>
    public const string STAT_TOTAL_FILES = "TOTAL_FILES";
    
    /// <summary>
    /// Optional URL to use for auto updates
    /// </summary>
    public static string? AutoUpdateUrl { get; set; }
}