namespace FileFlows.Shared.Models;

/// <summary>
/// A model used to search the library files
/// </summary>
public class LibraryFileSearchModel
{
    /// <summary>
    /// Gets or sets the path to search for
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the from date to search
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date to search
    /// </summary>
    public DateTime ToDate { get; set; }
    
    /// <summary>
    /// Gets or sets the starting processing time to search
    /// </summary>
    public DateTime? FinishedProcessingFrom { get; set; }
    /// <summary>
    /// Gets or sets the finishing processing time to search
    /// </summary>
    public DateTime? FinishedProcessingTo { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library
    /// </summary>
    public Guid Library { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the library
    /// </summary>
    public FileStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the number of files to limit it too
    /// </summary>
    public int Limit { get; set; }
    
    /// <summary>
    /// Gets or sets the order by
    /// </summary>
    public LibraryFileSearchOrderBy? OrderBy { get; set; }
    
}

/// <summary>
/// Order by options for the library file search
/// </summary>
public enum LibraryFileSearchOrderBy
{
    /// <summary>
    /// No order by
    /// </summary>
    None = 0,
    /// <summary>
    /// Order by the storage saved
    /// </summary>
    Savings = 1
}