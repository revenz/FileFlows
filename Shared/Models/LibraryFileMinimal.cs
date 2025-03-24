using FileFlows.Plugin.Helpers;

namespace FileFlows.Shared.Models;

/// <summary>
/// Minimal representation of a library
/// </summary>
public class LibraryFileMinimal : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    [JsonPropertyName("u")]
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the file exstions
    /// </summary>
    [JsonPropertyName("ex")]
    public string Extension { get; set; }

    /// <summary>
    /// Gets or sets the display name of the item
    /// </summary>
    [JsonPropertyName("dn")]
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the reason this file failed, if it failed
    /// </summary>
    [JsonPropertyName("fr")]
    public string? FailureReason { get; set; }
    
    /// <summary>
    /// Gets or sets the library name
    /// </summary>
    [JsonPropertyName("pn")]
    public string NodeName { get; set; }
    
    /// <summary>
    /// Gets or sets the library name
    /// </summary>
    [JsonPropertyName("l")]
    public string LibraryName { get; set; }

    /// <summary>
    /// Gets or sets the original size of the file
    /// </summary>
    [JsonPropertyName("os")]
    public long OriginalSize { get; set; }
    /// <summary>
    /// Gets or sets the final size of the file
    /// </summary>
    [JsonPropertyName("fs")]
    public long FinalSize { get; set; }
    
    /// <summary>
    /// Gets the savings
    /// </summary>
    [JsonIgnore]
    public long Savings => Status == FileStatus.Processed && FinalSize > 0 ? OriginalSize - FinalSize : 0;

    /// <summary>
    /// Gets or sets the processing status of the file
    /// </summary>
    [JsonPropertyName("s")]
    public FileStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a date (UTC) appropriate for the file status
    /// For unprocessing, the date is when the file was first found
    /// For processed, the date is the date the file finished processing
    /// </summary>
    [JsonPropertyName("dt")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the traits of a file,
    /// e.g. for video the codec, the language, for images the format the resolution
    /// </summary>
    [JsonPropertyName("t")]
    public List<string> Traits { get; set; }

    /// <summary>
    /// Gets or sets the tags on the file
    /// </summary>
    [JsonPropertyName("tg")]
    public List<Guid> Tags { get; set; }

    /// <summary>
    /// Implicitly converts a <see cref="LibraryFile"/> to a <see cref="LibraryFileMinimal"/>.
    /// </summary>
    /// <param name="file">The full library file object to convert.</param>
    /// <returns>A minimal representation of the library file.</returns>
    public static implicit operator LibraryFileMinimal(LibraryFile file)
        => new ()
        {
            Uid = file.Uid,
            FailureReason = file.FailureReason,
            Status = file.Status,
            LibraryName = file.LibraryName,
            Traits = file.Additional?.Traits != null ? new List<string>(file.Additional?.Traits) : new List<string>(),
            OriginalSize = file.OriginalSize,
            FinalSize = file.FinalSize,
            NodeName = file.NodeName,
            Tags = file.Tags,
            Date = file.Status == FileStatus.Unprocessed ? 
                file.HoldUntil > DateTime.UtcNow ? file.HoldUntil : file.DateCreated :
                file.ProcessingEnded.Year > 2000 ? file.ProcessingEnded :
                file.DateCreated,
            Extension = file.IsDirectory ? null : FileHelper.GetExtension(file.OutputPath?.EmptyAsNull() ?? file.Name),
            DisplayName = file.Additional?.DisplayName?.EmptyAsNull() ?? file.RelativePath?.EmptyAsNull() ?? file.Name,
        };
}