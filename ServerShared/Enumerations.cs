namespace FileFlows.ServerShared;

/// <summary>
/// Result of the update
/// </summary>
public enum NodeStatusUpdateResult
{
    /// <summary>
    /// Success 
    /// </summary>
    Success,
    /// <summary>
    /// Node needs to update its configuration
    /// </summary>
    UpdateConfiguration,
    /// <summary>
    /// Called if the update include an invalid model
    /// </summary>
    InvalidModel,
    /// <summary>
    /// An exception occurred while updating
    /// </summary>
    Exception
}

/// <summary>
/// Represents the result of checking whether a file can be processed.
/// </summary>
public enum FileCheckResult
{
    /// <summary>
    /// The file can be processed.
    /// </summary>
    CanProcess = 1,

    /// <summary>
    /// The file cannot be processed.
    /// </summary>
    CannotProcess = 2,

    /// <summary>
    /// An unknown error occurred during the file check.
    /// </summary>
    UnknownError = 3,

    /// <summary>
    /// The file requires processing on a different node.
    /// </summary>
    RequiresDifferentNode = 4,

    /// <summary>
    /// The flow definition for processing the file was not found.
    /// </summary>
    FlowNotFound = 5,

    /// <summary>
    /// The file exceeds the maximum allowed size for processing.
    /// </summary>
    ExceedMaxFileSize = 6,

    /// <summary>
    /// The file belongs to a library that cannot be processed.
    /// </summary>
    CannotProcessLibrary = 7,
    
    /// <summary>
    /// The pre-check on the node failed to execute/pass
    /// </summary>
    PreCheckFailed = 8,
    
    /// <summary>
    /// The node could not find the temp path
    /// </summary>
    FailedToLocateTempPath = 8,

    // Values over 100 indicate the node should be removed from checking

    /// <summary>
    /// The node is disconnected and cannot be used.
    /// </summary>
    Disconnected = 101,

    /// <summary>
    /// The configuration version of the node does not match.
    /// </summary>
    ConfigVersionMismatch = 102,

    /// <summary>
    /// The application version of the node does not match.
    /// </summary>
    VersionMismatch = 103,

    /// <summary>
    /// The node has reached the maximum number of active runners.
    /// </summary>
    AtMaximumRunners = 104,
}
