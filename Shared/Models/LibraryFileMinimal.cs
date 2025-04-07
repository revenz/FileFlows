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
    /// Gets or sets the output path of the final file
    /// </summary>
    [JsonPropertyName("o")]
    public string? OutputPath { get; set; }

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
    /// Gets the total processing time of the flow
    /// </summary>
    [JsonPropertyName("pt")]
    public TimeSpan ProcessingTime { get; set; }
    
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
    /// Gets or sets any flags on the file
    /// </summary>
    [JsonPropertyName("f")]
    public LibraryFileMinimalFlag Flags { get; set; }
    // /// <summary>
    // /// Gets or sets the file drop email if this is a file drop file
    // /// </summary>
    // [JsonPropertyName("fde")]
    // public string? FileDropEmail { get; set; }
    // /// <summary>
    // /// Gets or sets the file drop short name if this is a file drop file
    // /// </summary>
    // [JsonPropertyName("fdsn")]
    // public string? FileDropShortName { get; set; }

    /// <summary>
    /// Implicitly converts a <see cref="LibraryFile"/> to a <see cref="LibraryFileMinimal"/>.
    /// </summary>
    /// <param name="file">The full library file object to convert.</param>
    /// <returns>A minimal representation of the library file.</returns>
    public static implicit operator LibraryFileMinimal(LibraryFile file)
    {
        LibraryFileMinimal lfm = new()
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
            Flags = LibraryFileMinimalFlag.None,
            ProcessingTime = file.ProcessingTime,
            Date = file.Status == FileStatus.Unprocessed
                ?
                file.HoldUntil > DateTime.UtcNow ? file.HoldUntil : file.DateCreated
                : file.Status is FileStatus.Processed or FileStatus.ProcessingFailed
                    ? file.ProcessingEnded
                    : file.DateCreated,
            Extension = file.IsDirectory ? null : FileHelper.GetExtension(file.OutputPath?.EmptyAsNull() ?? file.Name),
            DisplayName = file.Additional?.DisplayName?.EmptyAsNull() ?? file.RelativePath?.EmptyAsNull() ?? file.Name,
        };

        if (file.IsForcedProcessing)
            lfm.Flags |= LibraryFileMinimalFlag.ForcedProcessing;

        if (file.Additional?.FileDropFlowUid != null && file.Additional?.FileDropFlowUid != Guid.Empty)
        {
            lfm.Flags |= LibraryFileMinimalFlag.FileDropFile;
        }

        if (file.Additional?.Reprocessing == true)
        {
            lfm.Flags |= LibraryFileMinimalFlag.ReprocessingByFlow;
            lfm.NodeName = file.ProcessOnNodeUid.ToString();
        }

        if (file.Status == FileStatus.Processed && file.Name!= file.OutputPath)
            lfm.OutputPath = file.OutputPath;

        return lfm;
    }
}

/// <summary>
/// Flags for library file minimal
/// </summary>
[Flags]
public enum LibraryFileMinimalFlag
{
    /// <summary>
    /// No flags
    /// </summary>
    None = 0,
    /// <summary>
    /// If the file has been forced processing
    /// </summary>
    ForcedProcessing = 1,
    /// <summary>
    /// If the file is marked for reprocessing by a flow
    /// </summary>
    ReprocessingByFlow = 2,
    /// <summary>
    /// If this is a file drop file
    /// </summary>
    FileDropFile = 4
}

/// <summary>
/// File that is processing
/// </summary>
public class ProcessingLibraryFile : LibraryFileMinimal
{
    /// <summary>
    /// Gets or sets the display name of the file being executed
    /// </summary>
    [JsonPropertyName("dir")]
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the relative file being executed
    /// </summary>
    [JsonPropertyName("p")]
    public string RelativePath { get; set; }
    
    /// <summary>
    /// Gets or sets the current working file
    /// </summary>
    [JsonPropertyName("wf")]
    public string WorkingFile { get; set; }
    
    /// <summary>
    /// Gets or sets the UID fo the library
    /// that the library file belongs
    /// </summary>
    [JsonPropertyName("lu")]
    public Guid LibraryUid { get; set; }

    /// <summary>
    /// Gets or sets the total parts in the flow that is executing
    /// </summary>
    [JsonPropertyName("tp")]
    public int TotalParts { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the flow part that is currently executing
    /// </summary>
    [JsonPropertyName("cp")]
    public int CurrentPart { get; set; }

    /// <summary>
    /// Gets or sets the name of the flow part that is currently executing
    /// </summary>
    [JsonPropertyName("cpn")]
    public string CurrentPartName { get; set; }

    /// <summary>
    /// Gets or sets the current percent of the executing flow part
    /// </summary>
    [JsonPropertyName("cpp")]
    public float CurrentPartPercent { get; set; }

    /// <summary>
    /// Gets or sets when the last update was reported to the server
    /// </summary>
    [JsonPropertyName("lx")]
    public DateTime LastUpdate { get; set; }
    
    /// <summary>
    /// Gets or sets when the flow execution started
    /// </summary>
    [JsonPropertyName("sa")]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets a special variable for frames per second
    /// </summary>
    [JsonPropertyName("fps")]
    public int? FramesPerSecond { get; set; }
    
    /// <summary>
    /// Gets or sets any additional info to pass to the runner
    /// </summary>
    [JsonPropertyName("ai")]
    public object[][] Additional { get; set; }
    
    /// <summary>
    /// Gets or sets if the file has a thumbnail
    /// </summary>
    [JsonPropertyName("tb")]
    public bool HasThumbnail { get; set; }

    /// <summary>
    /// Gets or sets if the file has been aborted
    /// </summary>
    [JsonPropertyName("a")]
    public bool Aborted { get; set; }
}