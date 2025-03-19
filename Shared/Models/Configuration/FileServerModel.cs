namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// Model for File Server settings
/// </summary>
public class FileServerModel
{
    /// <summary>
    /// Gets or sets if the file server is disabled
    /// </summary>
    public bool FileServerDisabled { get; set; }
    
    /// <summary>
    /// Gets or sets the file permissions to set on the file
    /// Only used on Unix based systems
    /// </summary>
    public int FileServerFilePermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the folder permissions to set on the folders
    /// Only used on Unix based systems
    /// </summary>
    public int FileServerFolderPermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the owner group to use
    /// </summary>
    public string FileServerOwnerGroup { get; set; }
    
    /// <summary>
    /// Gets or sets the allowed paths for the file server
    /// </summary>
    public string[] FileServerAllowedPaths { get; set; }
    
    /// <summary>
    /// Gets or sets the file server all list
    /// </summary>
    public string FileServerAllowedPathsString { get; set; }
}