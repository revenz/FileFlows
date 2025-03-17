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
    InvalidModel
}