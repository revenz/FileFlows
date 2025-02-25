using System.Text.RegularExpressions;

namespace FileFlows.Client.Helpers;

/// <summary>
/// Icon Helper
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Gets the image for the file
    /// </summary>
    /// <param name="path">the path of the file</param>
    /// <returns>the image to show</returns>
    public static string GetExtensionImage(string path)
    {
        var extension = GetExtension(path);
        if(extension is "folder" or "html")
            return $"/icons/filetypes/{extension}.svg";
        
        string prefix = "/icon/filetype";
#if (DEBUG)
        prefix = "http://localhost:6868/icon/filetype";
#endif
        return $"{prefix}/{extension}.svg";
    }

    private static string GetExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "folder";
        if(path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            return "html";
        int index = path.LastIndexOf('.');
        if (index < 0)
            return "folder";
        string extension = path[(index + 1)..].ToLowerInvariant();
        if (Regex.IsMatch(path, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            extension = "url";
        return extension;   
    }
    
    /// <summary>
    /// Gets the image for the extension
    /// </summary>
    /// <param name="extension">the extension of the file</param>
    /// <returns>the image to show</returns>
    public static string GetImage(string extension)
    {
        if(string.IsNullOrWhiteSpace(extension))
            return "/icons/filetypes/folder.svg";
        string prefix = "/icon/filetype";
#if (DEBUG)
        prefix = "http://localhost:6868/icon/filetype";
#endif
        return $"{prefix}/{extension}.svg";
    }

    /// <summary>
    /// Gets the thumbnail url
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="libraryFileName">the name of the library file</param>
    /// <param name="IsDirectory">if this is a directory</param>
    /// <returns>the thumbnail url</returns>
    public static string GetThumbnail(Guid libraryFileUid, string libraryFileName, bool IsDirectory)
    {
        var extension = GetExtension(libraryFileName);
#if(DEBUG)
        return $"http://localhost:6868/api/thumbnail/{libraryFileUid}?extension={extension}&pad=50&folder={IsDirectory}";
#else
        return $"/api/thumbnail/{libraryFileUid}?extension={extension}&pad=50&folder={IsDirectory}";
#endif
    }
}