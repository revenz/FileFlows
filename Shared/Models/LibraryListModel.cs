using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A library that FileFlows will monitor for files to process
/// </summary>
public class LibraryListModel : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the library 
    /// </summary>
    [JsonPropertyName("u")]
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library
    /// </summary>
    [JsonPropertyName("n")]
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets if this library is enabled
    /// </summary>
    [JsonPropertyName("e")]
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the path of the library
    /// </summary>
    [JsonPropertyName("l")]
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the library
    /// </summary>
    [JsonPropertyName("d")]
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the flow this library uses
    /// </summary>
    [JsonPropertyName("f")]
    public ObjectReference? Flow { get; set; }

    /// <summary>
    /// When the library was last scanned
    /// </summary>
    [JsonPropertyName("ls")]
    public DateTime LastScanned { get; set; }
    
    /// <summary>
    /// Gets or sets the processing priority of this library
    /// </summary>
    [JsonPropertyName("p")]
    public ProcessingPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the order this library will process its files
    /// </summary>
    [JsonPropertyName("o")]
    public ProcessingOrder ProcessingOrder { get; set; }
    
    /// <summary>
    /// Gets or sets the number of files inthe library
    /// </summary>
    [JsonPropertyName("tf")]
    public int Files { get; set; }
    
    /// <summary>
    /// Gets or sets the original size
    /// </summary>
    [JsonPropertyName("s")]
    public long OriginalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the final size
    /// </summary>
    [JsonPropertyName("z")]
    public long FinalSize { get; set; }
}