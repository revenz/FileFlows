using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Filter used for searching for library files
/// </summary>
public class LibraryFileFilter
{
    /// <summary>
    /// Gets if we are requesting a file for processing
    /// </summary>
    public bool GettingFileForProcess => Rows == 1;
    
    /// <summary>
    /// the status of the data
    /// </summary>
    public FileStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets a list of libraries that are allowed, or null if any are allowed
    /// </summary>
    public List<Guid>? AllowedLibraries { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum size in MBs of the file to be returned
    /// </summary>
    public long? MaxSizeMBs { get; set; }

    /// <summary>
    /// Gets or sets UIDs of files to be ignored
    /// </summary>
    public List<Guid>? ExclusionUids { get; set; }
    /// <summary>
    /// Gets or sets if only forced files should be returned
    /// </summary>
    public bool ForcedOnly { get; set; }
    /// <summary>
    /// Gets or sets the number to rows that will be fetched, not fetched now, but later on, used to determine
    /// if we are getting the 'NextFile' which takes Library runners into account
    /// </summary>
    public int Rows { get; set; }

    /// <summary>
    /// Gets or sets the amount to skip
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets a filter text the file name must contain
    /// </summary>
    public string? Filter { get; set; }
    
    /// <summary>
    /// Gets or sets a Node UID to filter by
    /// </summary>
    public Guid? NodeUid { get; set; }
    
    /// <summary>
    /// Gets or sets an optional override for node processing order
    /// </summary>
    public ProcessingOrder? NodeProcessingOrder { get; set; }
    
    /// <summary>
    /// Gets or sets a Node UID of the processing node requesting this file
    /// </summary>
    public Guid? ProcessingNodeUid { get; set; }
    
    /// <summary>
    /// Gets or sets a Library UID to filter by
    /// </summary>
    public Guid? LibraryUid { get; set; }
    
    /// <summary>
    /// Gets or sets a Flow UID to filter by
    /// </summary>
    public Guid? FlowUid { get; set; }
    
    /// <summary>
    /// Gets or sets a Tag UID to filter by
    /// </summary>
    public Guid? TagUid { get; set; }
    
    /// <summary>
    /// Gets or sets a specific sort by to sort by
    /// </summary>
    public FilesSortBy? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the system info
    /// </summary>
    public LibraryFilterSystemInfo SysInfo { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a UID of a reseller user who this file belongs to
    /// </summary>
    public Guid? ResellerUserUid { get; set; }
    
    /// <summary>
    /// Gets or sets a UID of a reseller flow who this file belongs to
    /// </summary>
    public Guid? ResellerFlowUid { get; set; }

    /// <summary>
    /// Tests if a file matches the filter
    /// </summary>
    /// <param name="file">the file</param>
    /// <returns>true if matches</returns>
    public bool Matches(LibraryFile file)
    {
        // test if it matches
        if (Status != null)
        {
            if (Status == FileStatus.Disabled)
            {
                if ((file.Flags & LibraryFileFlags.ForceProcessing) == LibraryFileFlags.ForceProcessing)
                    return false; // its force, therefore not disabled
                if (file.Status != FileStatus.Unprocessed)
                    return false;
                if(file.LibraryUid == null || SysInfo.AllLibraries.TryGetValue(file.LibraryUid.Value, out var lib) == false || lib?.Enabled != false)
                    return false;
            }
            else if (Status == FileStatus.OnHold)
            {
                if (file.Status != FileStatus.Unprocessed)
                    return false;
                if(file.HoldUntil < DateTime.UtcNow)
                    return false;
            }
            else if (Status == FileStatus.OutOfSchedule)
            {
                if (file.Status != FileStatus.Unprocessed)
                    return false;
                if(file.LibraryUid == null || SysInfo.AllLibraries.TryGetValue(file.LibraryUid.Value, out var lib) == false || lib?.Enabled != true)
                    return false;
                if (TimeHelper.InSchedule(lib.Schedule) == true)
                    return false;
                return true;
            }
            else if (Status == FileStatus.Unprocessed && file.Status == FileStatus.Unprocessed)
            {
                // need to check that it isn't actually disabled
                if (file.IsForcedProcessing)
                    return true; // its forced to process
                if (file.LibraryUid != null && SysInfo.AllLibraries.TryGetValue(file.LibraryUid.Value, out var lib))
                {
                    if (lib.Enabled == false)
                        return false; // its actually disabled
                    if (string.IsNullOrWhiteSpace(lib.Schedule) == false &&
                        TimeHelper.InSchedule(lib.Schedule) == false)
                        return false; // not in schedule
                }

                if(file.HoldUntil > DateTime.UtcNow)
                    return false; // its on hold
            }
            else if (file.Status != Status)
                return false;
        }

        if (AllowedLibraries != null && 
            (file.LibraryUid == null || AllowedLibraries!.Contains(file.LibraryUid.Value) == false))
            return false;
        if (MaxSizeMBs is > 0 && file.OriginalSize > MaxSizeMBs * 1_000_000)
            return false;
        if (ExclusionUids != null && ExclusionUids.Contains(file.Uid))
            return false;
        if (ForcedOnly && (file.Flags & LibraryFileFlags.ForceProcessing) == 0)
            return false;
        if (!string.IsNullOrEmpty(Filter) && !file.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            return false;
        if (NodeUid != null && file.NodeUid != NodeUid)
            return false;
        if (ProcessingNodeUid != null && file.ProcessOnNodeUid != null && file.ProcessOnNodeUid != Guid.Empty && file.ProcessOnNodeUid != ProcessingNodeUid)
            return false;
        if (LibraryUid != null && file.LibraryUid != LibraryUid)
            return false;
        if (FlowUid != null && file.FlowUid != FlowUid)
            return false;
        if (TagUid != null && file.Tags?.Contains(TagUid.Value) != true)
            return false;
        if (ResellerUserUid != null && file.Additional?.ResellerUserUid != ResellerUserUid)
            return false;
        if (ResellerFlowUid != null && file.Additional?.ResellerFlowUid != ResellerFlowUid)
            return false;

        return true;
    }
}

/// <summary>
/// Library Filter system information
/// </summary>
public class LibraryFilterSystemInfo
{
    /// <summary>
    /// Gets or sets all the libraries in the system
    /// </summary>
    public Dictionary<Guid, Library> AllLibraries { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of current executors
    /// </summary>
    public List<FlowExecutorInfo> Executors { get; set; } = new();
    
    /// <summary>
    /// Gets or sets if licensed for processing order
    /// </summary>
    public bool LicensedForProcessingOrder { get; set; }
    
} 