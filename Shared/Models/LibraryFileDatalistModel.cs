namespace FileFlows.Shared.Models;

/// <summary>
/// Model class for the Library Files page
/// </summary>
public class LibraryFileDatalistModel
{
    /// <summary>
    /// Gets or sets a list of library files to show in the UI
    /// </summary>
    public IEnumerable<LibraryFileListModel> LibraryFiles { get; set; }

    /// <summary>
    /// Gets or sets the status data for the libraries
    /// </summary>
    public IEnumerable<LibraryStatus> Status { get; set; }
}