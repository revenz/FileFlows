using System.Diagnostics;
using System.Text;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper for DockerMods
/// </summary>
public static class DockerModHelper
{
    /// <summary>
    /// A collection of executed docker mods
    /// </summary>
    private static Dictionary<Guid, int> ExecutedDockerMods = new();

    private static FairSemaphore _semaphore = new(1);
    
    /// <summary>
    /// Executes a DockerMod, if does file does not exist on disk, this will write it
    /// </summary>
    /// <param name="mod">the DockerMod to execute</param>
    /// <param name="forceExecution">If this should run even if it has already been run</param>
    /// <param name="outputCallback">Callback to log the output to</param>
    /// <returns>true if successful, otherwise the failure reason</returns>
    public static async Task<Result<bool>> Execute(DockerMod mod, bool forceExecution = false, Action<string>? outputCallback = null)
    {
        if (Globals.IsDocker == false)
            return true; // Only run on Docker instances
        if (mod.Name.ToLowerInvariant().Equals("docker"))
            return true; // we dont run this old mod anymore, its now installed part of the container

        await _semaphore.WaitAsync();

        try
        {
            var directory = DirectoryHelper.DockerModsDirectory;
            
            var file = Path.Combine(directory, GetDockerModFileName(mod));
            
            if (Directory.Exists(directory) == false)
                Directory.CreateDirectory(directory);
            if (Directory.Exists(DirectoryHelper.DockerModsCommonDirectory) == false)
                Directory.CreateDirectory(DirectoryHelper.DockerModsCommonDirectory);
            string code = mod.Code.Replace("\r\n", "\n");
            if (code.StartsWith("#!/bin/bash", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                code = "#!/bin/bash\n\n";
            }
            code = code.Insert(code.IndexOf('\n') + 1, "\ncommon=\"" + DirectoryHelper.DockerModsCommonDirectory + "\"\n\n");
            await File.WriteAllTextAsync(file, code);

            // Set execute permission for the file
            await MakeExecutable(file);

            if (forceExecution == false && ExecutedDockerMods.TryGetValue(mod.Uid, out int value) &&
                value == mod.Revision)
                return true; // already executed

            // Run dpkg to configure any pending package installations
            //await Process.Start("dpkg", "--configure -a").WaitForExitAsync();
            await Process.Start(new ProcessStartInfo
            {
                //FileName = "/bin/bash",
                FileName = "/bin/su",
                ArgumentList = { "c", "dpkg --configure -a" },
                UseShellExecute = false
            })!.WaitForExitAsync();

            Logger.Instance.ILog($"Installing DockerMod: {file}");
            
            
            // Initialize the process with configuration
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/su",
                ArgumentList = { "-c", $"\"{file}\"" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = DirectoryHelper.DockerModsDirectory
            };

            // Run the process
            using var process = new Process();
            process.StartInfo = processStartInfo;

            StringBuilder outputBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Logger.Instance.Raw(e.Data);
                    outputCallback?.Invoke(outputBuilder.ToString());
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Logger.Instance.Raw(e.Data);
                    outputCallback?.Invoke(outputBuilder.ToString());
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine(); // Begin reading standard error stream asynchronously

            await process.WaitForExitAsync();
            int exitCode = process.ExitCode;
            string output = outputBuilder.ToString();

            var totalLength = 120;
            var modNameLength = mod.Name.Length;
            var sideLength = (totalLength - modNameLength - 14) / 2; // Subtracting 14 for the length of " Docker Mod: "

            if (exitCode != 0)
            {
                var header = new string('-', sideLength) + " Docker Mod Failed: " + mod.Name + " " +
                             new string('-', sideLength + (modNameLength % 2));
                Logger.Instance.ELog("\n" + header + "\n" + output + "\n" +
                                     new string('-', totalLength));
                return Result<bool>.Fail(output);
            }
            else
            {
                var header = new string('-', sideLength) + " Docker Mod: " + mod.Name + " " +
                             new string('-', sideLength + (modNameLength % 2));
                Logger.Instance.ILog("\n" + header + "\n" + output + "\n" +
                                 new string('-', totalLength));
            }

            ExecutedDockerMods[mod.Uid] = mod.Revision;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed Running DockerMod: " + ex.Message);
            return Result<bool>.Fail("Failed Running DockerMod: " + ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Makes a script (.sh) file executable
    /// </summary>
    /// <param name="scriptFile">the script file</param>
    private static async Task MakeExecutable(string scriptFile)
    {
        await Process.Start("chmod", new[] { "+x", scriptFile }).WaitForExitAsync();
    }

    /// <summary>
    /// Gets the filename for a DockerMod
    /// </summary>
    /// <param name="mod">the DockerMod</param>
    /// <returns>the File name</returns>
    private static string GetDockerModFileName(DockerMod mod)
        => FileHelper.RemoveIllegalCharacters($"{mod.Order:0000}_{mod.Name.Replace(" ", "")}_[{mod.Revision}].sh");
    
    /// <summary>
    /// Uninstalls any DockerMod that is not know
    /// </summary>
    /// <param name="mods">The known DockerMods</param>
    public static async Task UninstallUnknownMods(List<DockerMod> mods)
    {
        var modNames = mods?.Select(GetDockerModFileName)?.ToList() ?? new (); // Convert to list for efficient lookup
        var modFiles = new DirectoryInfo(DirectoryHelper.DockerModsDirectory).GetFiles("*.sh");
        var unknownMods = modFiles
            .Where(file => modNames.Contains(file.Name) == false).ToList();

        foreach (var unknown in unknownMods)
        {
            string name = Path.GetFileNameWithoutExtension(unknown.Name);
            if (name.Contains("_docker_[", StringComparison.InvariantCultureIgnoreCase))
            {
                unknown.Delete();
                continue;
            }

            try
            {
                Logger.Instance.WLog($"About to uninstall DockerMod '{name}'");
                // Initialize the process start info
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/su",
                    ArgumentList = { "-c", $"\"{unknown.FullName}\" --uninstall" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = DirectoryHelper.DockerModsDirectory
                };

                // Create the process with the configured start info
                using var process = new Process();
                process.StartInfo = processStartInfo;

                StringBuilder outputBuilder = new ();

                Logger.Instance.ILog($"Uninstalling DockerMod: {name}");
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Instance.Raw(e.Data);
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Instance.Raw(e.Data);
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine(); // Begin reading standard error stream asynchronously

                await process.WaitForExitAsync();
                int exitCode = process.ExitCode;
                string output = outputBuilder.ToString();

                var totalLength = 120;
                var modNameLength = name.Length;
                var sideLength =
                    (totalLength - modNameLength - 14) / 2; // Subtracting 14 for the length of " Docker Mod: "

                if (exitCode != 0)
                {
                    var header = new string('-', sideLength) + " Docker Mod Uninstall Failed: " + name + " " +
                                 new string('-', sideLength + (modNameLength % 2));
                    Logger.Instance.ELog("\n" + header + "\n" + output + "\n" +
                                         new string('-', totalLength));
                }
                else
                {
                    var header = new string('-', sideLength) + " Docker Mod Uninstall: " + name + " " +
                                 new string('-', sideLength + (modNameLength % 2));
                    Logger.Instance.ILog("\n" + header + "\n" + output + "\n" +
                                         new string('-', totalLength));
                }
            }
            catch (Exception)
            {
                Logger.Instance.ELog("Failed to uninstall DockerMod: " + name);
            }

            unknown.Delete();
        }
    }
}