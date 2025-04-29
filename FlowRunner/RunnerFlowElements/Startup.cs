using System.Diagnostics;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Plugin.Services;
using FileFlows.ServerShared;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Startup of a run, downloads scripts, plugins etc
/// </summary>
public class Startup : Node
{
    /// <summary>
    /// The run instance running this
    /// </summary>
    private readonly RunInstance runInstance;

    /// <summary>
    /// Creates a new instance of the startup
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    public Startup(RunInstance runInstance)
    {
        this.runInstance = runInstance;
    }

    /// <summary>
    /// Executes the startup of a flow
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output</returns>
    public override int Execute(NodeParameters args)
    {
        // now we can initialize the file safely
        args.InitFile(args.WorkingFile);

        LogHeader(args, runInstance.Properties.ConfigDirectory, runInstance.Properties.ProcessingNode);
        try
        {
            Helpers.RunPreparationHelper.DownloadPlugins(runInstance);
        }
        catch (Exception ex)
        {
            args.FailureReason = "Error downloading plugins: " + ex.Message;
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        try
        {
            Helpers.RunPreparationHelper.DownloadScripts(runInstance.Properties.WorkingDirectory, runInstance.Properties.ConfigDirectory);
        }
        catch (Exception ex)
        {
            args.FailureReason = "Error downloading scripts: " + ex.Message;
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        ExtractImageVariables(args);

        return 1;
    }


    /// <summary>
    /// Logs the version info for all plugins etc
    /// </summary>
    /// <param name="nodeParameters">the node parameters</param>
    /// <param name="configDirectory">the directory of the configuration</param>
    /// <param name="node">the node executing this flow</param>
    private static void LogHeader(NodeParameters nodeParameters, string configDirectory, ProcessingNode node)
    {
        nodeParameters.Logger!.ILog("Version: " + Globals.Version);
        if (Globals.IsDocker)
            nodeParameters.Logger!.ILog("Platform: Docker" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsLinux)
            nodeParameters.Logger!.ILog("Platform: Linux" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsWindows)
            nodeParameters.Logger!.ILog("Platform: Windows" + (Globals.IsArm ? " (ARM)" : string.Empty));
        else if (Globals.IsMac)
            nodeParameters.Logger!.ILog("Platform: Mac" + (Globals.IsArm ? " (ARM)" : string.Empty));

        nodeParameters.Logger!.ILog("File: " + nodeParameters.FileName);
        //nodeParameters.Logger!.ILog("Executing Flow: " + flow.Name);
        nodeParameters.Logger!.ILog("File Service: " + FileService.Instance.GetType().Name);
        nodeParameters.Logger!.ILog("Processing Node: " + node.Name);

        var dir = Path.Combine(configDirectory, "Plugins");
        if (Directory.Exists(dir))
        {
            foreach (var dll in new DirectoryInfo(dir).GetFiles("*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    string version = string.Empty;
                    var versionInfo = FileVersionInfo.GetVersionInfo(dll.FullName);
                    if (versionInfo.CompanyName != "FileFlows")
                        continue;
                    version = versionInfo.FileVersion?.EmptyAsNull() ?? versionInfo.ProductVersion ?? string.Empty;
                    nodeParameters.Logger!.ILog("Plugin: " + dll.Name + " version " +
                                                (version?.EmptyAsNull() ?? "unknown"));
                }
                catch (Exception)
                {
                }
            }
        }

        Helpers.FFmpegHelper.LogFFmpegVersion(nodeParameters);

        foreach (var v in nodeParameters.Variables)
        {
            //if (v.Key.StartsWith("file.") || v.Key.StartsWith("folder.") || v.Key == "ext" ||  
            if (v.Key.Contains(".Url") || v.Key.Contains("Key"))
                continue;

            nodeParameters.Logger!.ILog($"Variables['{v.Key}'] = {v.Value}");
        }
    }

    /// <summary>
    /// Extracts and saves base64-encoded images from the provided node parameters.
    /// </summary>
    /// <param name="args">The node parameters containing variables with potential base64 image data.</param>
    void ExtractImageVariables(NodeParameters args)
    {
        foreach (var key in args.Variables.Keys)
        {
            var str = args.Variables[key]?.ToString() ?? string.Empty;
            if (!str.StartsWith("data:image"))
                continue;

            // Match "data:image/{mime};base64,{data}"
            var match = Regex.Match(str, @"^data:image/(?<mime>[a-zA-Z0-9]+);base64,(?<data>.+)$");
            if (!match.Success)
                continue;
            
            args.Logger?.ILog("Base64 Image variable detected: " + key);

            string mimeType = match.Groups["mime"].Value;
            string base64Data = match.Groups["data"].Value;
            string extension = GetImageExtension(mimeType);
            if (extension == null)
                continue;

            
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                string safeFileName = SanitizeFileName(key) + extension;
                string filePath = Path.Combine(args.TempPath, safeFileName);
                
                args.Logger?.ILog("Saving Base64 Image variable: " + filePath);

                File.WriteAllBytes(filePath, imageBytes);
                args.Variables[key] = filePath;
            }
            catch(Exception ex)
            {
                // Handle invalid base64 or write errors gracefully
                args.Logger?.WLog("Failed saving Base64 Image variable: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Gets the file extension based on the image MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type of the image.</param>
    /// <returns>The corresponding file extension, or null if unsupported.</returns>
    string GetImageExtension(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "png" => ".png",
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "gif" => ".gif",
            "bmp" => ".bmp",
            "webp" => ".webp",
            "svg+xml" => ".svg",
            "x-icon" => ".ico",
            "tiff" => ".tiff",
            "tif" => ".tif",
            "apng" => ".apng",
            "avif" => ".avif",
            "heif" => ".heif",
            "heic" => ".heic",
            "jxl" => ".jxl",
            _ => null // Unsupported MIME type
        };
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    /// <param name="fileName">The original filename.</param>
    /// <returns>A safe filename.</returns>
    string SanitizeFileName(string fileName)
        => Regex.Replace(fileName, @"[^a-zA-Z0-9_-]", "_");
}