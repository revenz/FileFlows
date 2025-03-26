namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// General model
/// </summary>
public class GeneralModel
{
    /// <summary>
    /// Gets or sets the language code for the system
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets if telemetry should be disabled
    /// </summary>
    public bool DisableTelemetry { get; set; }

    /// <summary>
    /// Gets or sets if library scanning should still occur when the system is paused
    /// </summary>
    public bool ScanWhenPaused { get; set; }

    /// <summary>
    /// Gets or sets the queue capacity for unprocessed files
    /// </summary>
    public int QueueCapacity { get; set; }

    /// <summary>
    /// Gets or sets the maximum page size for completed files
    /// </summary>
    public int MaxPageSize { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files from a failed flow should be kept
    /// </summary>
    public bool KeepFailedFlowTempFiles { get; set; }
    
    /// <summary>
    /// Gets or sets if temporary files should not be used when moving/copying files
    /// </summary>
    public bool DontUseTempFilesWhenMovingOrCopying { get; set; }
    
    /// <summary>
    /// Gets or sets if DockerMods should run on the server on startup/when enabled
    /// </summary>
    public bool DockerModsOnServer { get; set; }
    
    /// <summary>
    /// Gets or sets if this is licensed
    /// </summary>
    public bool IsLicensed { get; set; }
}