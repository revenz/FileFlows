namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Helper for mime/types
/// </summary>
public static class MimeTypeHelper
{
    /// <summary>
    /// Gets the file extension based on the provided MIME type.
    /// If the MIME type is valid, the method attempts to use the subtype as the extension.
    /// If the subtype is not a valid extension, it falls back on known mappings for common MIME types.
    /// </summary>
    /// <param name="mimeType">The MIME type to analyze.</param>
    /// <returns>A string representing the file extension, including the leading period (e.g., ".jpg"). 
    /// Returns ".bin" for unknown or invalid MIME types.</returns>
    public static string GetFileExtension(string mimeType)
    {
        // If mimeType is null or empty, return a default extension
        if (string.IsNullOrWhiteSpace(mimeType))
            return ".bin"; // Default for unknown types

        // Split the MIME type to get the type and subtype
        var parts = mimeType.Split('/');
        if (parts.Length != 2)
            return ".bin"; // Return default if it's not a valid MIME type format

        // Get the subtype and check if it can be used as a valid extension
        string subtype = parts[1].ToLower();

        // Return the subtype as the extension if it's valid (alphanumeric or hyphen)
        if (!string.IsNullOrWhiteSpace(subtype) && 
            subtype.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            return "." + subtype; // Return the valid subtype with a leading dot
        }

        // Fallback to specific known mappings for common types
        return subtype switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "gif" => ".gif",
            "bmp" => ".bmp",
            "svg+xml" => ".svg",
            "plain" => ".txt",
            "html" => ".html",
            "csv" => ".csv",
            "pdf" => ".pdf",
            "zip" => ".zip",
            "mp3" => ".mp3",
            "wav" => ".wav",
            "ogg" => ".ogg",
            "mp4" => ".mp4",
            "mov" => ".mov",
            "avi" => ".avi",
            // Add more common types as needed
            _ => ".bin" // Default for unrecognized types
        };
    }
}