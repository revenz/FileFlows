using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Information used during the flow execution 
/// </summary>
public class FlowExecutorInfo
{
    /// <summary>
    /// Gets or sets the library file being processed
    /// </summary>
    public LibraryFile LibraryFile { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the node execution this flow
    /// </summary>
    public Guid NodeUid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the Node executing this flow
    /// </summary>
    public string NodeName { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a remote file from the server
    /// </summary>
    public bool IsRemote { get; set; }
    
    /// <summary>
    /// Gets or sets the path of the library
    /// </summary>
    public string LibraryPath { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the original file being processed
    /// </summary>
    public long InitialSize { get; set; }

    /// <summary>
    /// Gets or sets the file that is currently being worked on/executed
    /// </summary>
    public string WorkingFile { get; set; }
    
    /// <summary>
    /// Gets or sets if the working file is actually a directory
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the total parts in the flow that is executing
    /// </summary>
    public int TotalParts { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the flow part that is currently executing
    /// </summary>
    public int CurrentPart { get; set; }

    /// <summary>
    /// Gets or sets the name of the flow part that is currently executing
    /// </summary>
    public string CurrentPartName { get; set; }

    /// <summary>
    /// Gets or sets the current percent of the executing flow part
    /// </summary>
    public float CurrentPartPercent { get; set; }

    /// <summary>
    /// Gets or sets when the last update was reported to the server
    /// </summary>
    public DateTime LastUpdate { get; set; }
    
    /// <summary>
    /// Gets or sets when the flow execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets the total processing time of the flow
    /// </summary>
    public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.UtcNow.Subtract(StartedAt) : new TimeSpan();
    
    /// <summary>
    /// Gets or sets any additional info to pass to the runner
    /// </summary>
    public Dictionary<string, AdditionalInfo> AdditionalInfos { get; set; }
    
    /// <summary>
    /// Gets or sets if the file has a thumbnail
    /// </summary>
    public bool HasThumbnail { get; set; }

    /// <summary>
    /// Gets or sets if the file has been aborted
    /// </summary>
    public bool Aborted { get; set; }
}



/// <summary>
/// Minified data for the flow executors that is sent to the client via websockets
/// </summary>
public class FlowExecutorInfoMinified
{
    /// <summary>
    /// Gets or sets the UID of the flow
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the display name of the file being executed
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the file being executed
    /// </summary>
    [JsonPropertyName("dir")]
    public bool IsDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets any traits on the file
    /// </summary>
    public List<string> Traits { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the Node executing this flow
    /// </summary>
    public string NodeName { get; set; }

    /// <summary>
    /// Gets or sets the library file UID being executed 
    /// </summary>
    public Guid LibraryFileUid { get; set; }

    /// <summary>
    /// Gets or sets the library file name being executed 
    /// </summary>
    public string LibraryFileName { get; set; }

    /// <summary>
    /// Gets or sets the relative file being executed
    /// </summary>
    public string RelativeFile { get; set; }
    
    /// <summary>
    /// Gets or sets the current working file
    /// </summary>
    public string WorkingFile { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library
    /// that the library file belongs
    /// </summary>
    public string LibraryName { get; set; }
    
    /// <summary>
    /// Gets or sets the UID fo the library
    /// that the library file belongs
    /// </summary>
    public Guid LibraryUid { get; set; }

    /// <summary>
    /// Gets or sets the total parts in the flow that is executing
    /// </summary>
    public int TotalParts { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the flow part that is currently executing
    /// </summary>
    public int CurrentPart { get; set; }

    /// <summary>
    /// Gets or sets the name of the flow part that is currently executing
    /// </summary>
    public string CurrentPartName { get; set; }

    /// <summary>
    /// Gets or sets the current percent of the executing flow part
    /// </summary>
    public float CurrentPartPercent { get; set; }

    /// <summary>
    /// Gets or sets when the last update was reported to the server
    /// </summary>
    public DateTime LastUpdate { get; set; }
    
    /// <summary>
    /// Gets or sets when the flow execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets the total processing time of the flow
    /// </summary>
    public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.UtcNow.Subtract(StartedAt) : new TimeSpan();
    
    /// <summary>
    /// Gets or sets a special variable for frames per second
    /// </summary>
    public int? FramesPerSecond { get; set; }
    
    /// <summary>
    /// Gets or sets any additional info to pass to the runner
    /// </summary>
    public object[][] Additional { get; set; }
    
    /// <summary>
    /// Gets or sets if the file has a thumbnail
    /// </summary>
    public bool HasThumbnail { get; set; }

    /// <summary>
    /// Gets or sets if the file has been aborted
    /// </summary>
    public bool Aborted { get; set; }
}

/// <summary>
/// Additional info to pass to the runner 
/// </summary>
public class AdditionalInfo
{
    /// <summary>
    /// Gets the timestamp when the item was recorded in UTC.
    /// </summary>
    public readonly DateTime RecordedAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the item has expired.
    /// </summary>
    public bool Expired => DateTime.UtcNow > RecordedAtUtc.Add(Expiry);
    
    /// <summary>
    /// Gets or sets the expiry of the info
    /// </summary>
    public TimeSpan Expiry { get; set; }

    /// <summary>
    /// Gets or sets the value of the item.
    /// </summary>
    public object Value { get; set; }
    
    /// <summary>
    /// Gets or sets how many steps to keep this info around for
    /// </summary>
    public int Steps { get; set; }
}