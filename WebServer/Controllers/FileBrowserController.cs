using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FileFlows.WebServer.Authentication;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.WebServer.Controllers;

[Route("/api/file-browser")]
[ApiExplorerSettings(IgnoreApi = true)]
[FileFlowsAuthorize(UserRole.Libraries | UserRole.Flows)]
public class FileBrowserController : Controller
{
    /// <summary>
    /// Gets the items in a given folder
    /// </summary>
    /// <param name="start">the start path</param>
    /// <param name="includeFiles">if files should be included</param>
    /// <param name="extensions">the allowed file extensions</param>
    /// <returns>a list of items</returns>
    [HttpGet]
    public IEnumerable<FileBrowserItem> GetItems([FromQuery] string start, [FromQuery] bool includeFiles, [FromQuery] string[] extensions)
    {
        if (start == "ROOT")
        {
            // special case for windows we list the drives
            var results = System.IO.DriveInfo.GetDrives().Where(x => x.IsReady).Select(x => new FileBrowserItem
            {
                IsDrive = true,
                Name = x.Name,
                FullName = x.RootDirectory.FullName
            });
            return results;
        }

        if (string.IsNullOrEmpty(start))
            start = GetStartDirectory();
        else if (System.IO.File.Exists(start))
            start = new FileInfo(start).DirectoryName!;
        else if (Directory.Exists(start) == false)
            start = GetStartDirectory();

        var items = new List<FileBrowserItem>();
        var di = new DirectoryInfo(start!);
        if (di.Exists)
        {
            if (di.Parent?.Exists == true)
            {
                items.Add(new FileBrowserItem
                {
                    IsParent = true,
                    FullName = di.Parent.FullName,
                    Name = di.FullName
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                items.Add(new FileBrowserItem
                {
                    IsParent = true,
                    FullName = "ROOT",
                    Name = di.FullName
                });
            }
            foreach (var dir in di.GetDirectories().OrderBy(x => x.Name?.ToLower() ?? string.Empty))
            {
                if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    if(OperatingSystem.IsWindows())
                        continue;
                }

                items.Add(new FileBrowserItem { FullName = dir.FullName, Name = dir.Name, IsPath = true });
            }
            if (includeFiles)
            {
                string expression = extensions?.Any() == false ? "" :
                                     ".(" + string.Join("|", extensions!.Select(x => Regex.Escape(x.ToLower()))) + ")$";
                var rgxFile = new Regex(expression);
                foreach (var file in di.GetFiles().OrderBy(x => x.Name?.ToLower() ?? string.Empty))
                {
                    if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;
                    if (rgxFile.IsMatch(file.Name))
                        items.Add(new FileBrowserItem { FullName = file.FullName, Name = file.Name });
                }
            }
        }
        return items;
    }
    
    /// <summary>
    /// Gets the start directory of the directory browser
    /// </summary>
    /// <returns>the start directory</returns>
    private string GetStartDirectory()
    {
        if (Globals.IsDocker && Directory.Exists("/media"))
            return "/media";
        return DirectoryHelper.GetUsersHomeDirectory();
    }
}