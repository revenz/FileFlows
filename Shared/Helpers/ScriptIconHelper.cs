namespace FileFlows.Shared.Helpers;

/// <summary>
/// Gets the icon to show for a script
/// </summary>
public static class ScriptIconHelper
{
    /// <summary>
    /// Gets the icon to show for a script
    /// </summary>
    /// <param name="name">the name of the script</param>
    /// <returns>the icon to show</returns>
    public static string GetIcon(string name)
    {
        string url = "";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        string nameLower = name.ToLowerInvariant();
        if (nameLower.StartsWith("video"))
            return "/icons/video.svg";
        if (nameLower.StartsWith("fileflows"))
            return "/favicon.svg";
        if (nameLower.StartsWith("image"))
            return "/icons/image.svg";
        if (nameLower.StartsWith("folder"))
            return "/icons/filetypes/folder.svg";
        if (nameLower.StartsWith("file"))
            return url + "/icon/filetype/file.svg";
        if (nameLower.StartsWith("7zip"))
            return url + "/icon/filetype/7z.svg";
        if (nameLower.StartsWith("language"))
            return "fas fa-comments";
        var icons = new[]
        {
            "apple", "apprise", "audio", "basic", "bat", "comic", "database", "docker", "emby", "folder", "gotify", "gz",
            "image", "intel", "linux", "nvidia", "plex", "pushbullet", "pushover", "ps1", "radarr", "sabnzbd", "sh", "sonarr",
            "video", "windows"
        };
        foreach (var icon in icons)
        {
            if (nameLower.StartsWith(icon))
                return $"/icons/{icon}.svg";
        }

        return "fas fa-scroll";
    }
}