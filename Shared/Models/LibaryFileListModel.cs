namespace FileFlows.Shared.Models;

/// <summary>
/// A model of the library file used in the UI
/// This model is used to reduce the data sent to the client browser
/// </summary>
public class LibaryFileListModel: IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the library file
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a file drop file
    /// </summary>
    [JsonPropertyName("fdf")]
    public bool FileDropFile { get; set; }
    /// <summary>
    /// Gets or sets the file drop email if this is a file drop file
    /// </summary>
    [JsonPropertyName("fde")]
    public string? FileDropEmail { get; set; }
    /// <summary>
    /// Gets or sets the file drop short name if this is a file drop file
    /// </summary>
    [JsonPropertyName("fdsn")]
    public string? FileDropShortName { get; set; }
    
    /// <summary>
    /// Gets or sets a display name for the file
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library file
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the relative path of the file
    /// This is the fullname of the file without the library path
    /// </summary>
    public string RelativePath { get; set; }
    
    /// <summary>
    /// Gets or sets a name of a file this is a duplicate of
    /// </summary>
    public string? Duplicate { get; set; }
    
    /// <summary>
    /// Gets or sets the final size of the file after processing
    /// </summary>
    public long? FinalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the flow that executed this file
    /// </summary>
    public string? Flow { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library this file belongs
    /// </summary>
    public string? Library { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the processing node that is executing/executed this file
    /// </summary>
    public string? Node { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the original library file
    /// </summary>
    public long? OriginalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the output path of the final file
    /// </summary>
    public string? OutputPath { get; set; }
    
    /// <summary>
    /// Gets or sets the processing time taken of the file
    /// </summary>
    public TimeSpan? ProcessingTime { get; set; }
    
    /// <summary>
    /// Gets or sets a date to show in the list
    /// Could be processed at, could be detected at, or starting processing at
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the status of the file
    /// </summary>
    public FileStatus? Status { get; set; }
    
    /// <summary>
    /// Gets or sets if this file has been marked force processing
    /// </summary>
    public bool Forced { get; set; }
    
    /// <summary>
    /// Gets or sets custom variables to use with this file
    /// </summary>
    public Dictionary<string, object> CustomVariables { get; set; }
    
    /// <summary>
    /// Gets or sets any tags on the file
    /// </summary>
    public List<Guid>? Tags { get; set; }
}