using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Shared.Models;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Service for the FileFlows configuration
/// </summary>
public class ConfigurationService
{
    /// <summary>
    /// Gets the Current configuration revision
    /// </summary>
    public ConfigurationRevision? CurrentConfig { get; set; }

    private readonly string _configKeyDefault = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets if a failed flow should keep its files
    /// </summary>
    bool CurrentConfigurationKeepFailedFlowFiles { get; set; }

    /// <summary>
    /// Constructs a new instance of the configuration service
    /// </summary>
    public ConfigurationService()
    {
        LoadPreviousConfig();
    }

    void LoadPreviousConfig()
    {
        if (Directory.Exists(DirectoryHelper.ConfigDirectory) == false)
            return;

        var path = new DirectoryInfo(DirectoryHelper.ConfigDirectory).GetDirectories(
            ).Where(x => Regex.IsMatch(x.Name, "^[\\d]+$") && x.Name != "0")
            .OrderByDescending(x => int.TryParse(x.Name, out var value) ? value : 0)
            .FirstOrDefault();
        if (path == null)
            return;
        
        try
        {
            string cfgFile = Path.Combine(path.FullName, "config.json");
            if (File.Exists(cfgFile) == false)
                return;

            var cfgJson = Environment.GetEnvironmentVariable("FF_NO_ENCRYPT") == "1"
                ? File.ReadAllText(cfgFile)
                : ConfigEncrypter.DecryptConfig(cfgFile);

            CurrentConfig = JsonSerializer.Deserialize<ConfigurationRevision>(cfgJson);
            Logger.Instance?.ILog("Loading configuration from disk: " + path.Name);
        }
        catch (Exception ex)
        {
            // Ignored
            Logger.Instance?.WLog("Failed to load previous config: " + ex.Message);
            path.Delete(true);
        }
    }


    /// <summary>
    /// Gets the configuration directory
    /// </summary>
    /// <param name="configVersion">the config revision</param>
    /// <returns>the configuration directory</returns>
    public string GetConfigurationDirectory(int? configVersion = null) =>
        Path.Combine(DirectoryHelper.ConfigDirectory, (configVersion ?? CurrentConfig?.Revision ?? 0).ToString());

    private SemaphoreSlim _updateConfigSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Ensures the local configuration is current with the server
    /// </summary>
    /// <param name="revision">the revision to get</param>
    /// <param name="node">the processing node</param>
    /// <returns>an awaited task</returns>
    public async Task<bool> UpdateConfiguration(int revision, ProcessingNode node)
    {
        await _updateConfigSemaphore.WaitAsync();
        try
        {
            var service = ServiceLoader.Load<ISettingsService>();
            
            string dir = GetConfigurationDirectory(revision);
            if (revision == CurrentConfig?.Revision && Directory.Exists(dir))
                return true;
            var config  = await service.GetCurrentConfiguration();
            if (config == null)
            {
                Logger.Instance.ELog("Failed downloading latest configuration from server");
                return false;
            }

            return await SaveConfiguration(config, node);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed getting configuration: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
        finally
        {
            _updateConfigSemaphore.Release();
        }
    }


    /// <summary>
    /// Saves the the specific configuration
    /// </summary>
    /// <param name="config">the specific configuration</param>
    /// <param name="node">the node</param>
    /// <returns>the configuration</returns>
    public async Task<bool> SaveConfiguration(ConfigurationRevision config, ProcessingNode node)
    {
        try
        {
            var service = ServiceLoader.Load<ISettingsService>();
            
            string dir = GetConfigurationDirectory(config.Revision);

            try
            {
                DeleteDirectory(dir);

                Logger.Instance.ILog("Deleted config directory: " + dir);

                Directory.CreateDirectory(dir);
                Logger.Instance.ILog("Created config directory: " + dir);
                Directory.CreateDirectory(Path.Combine(dir, "Scripts"));
                Logger.Instance.ILog("Created config directory: " + Path.Combine(dir, "Scripts"));
                Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Shared"));
                Logger.Instance.ILog("Created config directory: " + Path.Combine(dir, "Scripts", "Shared"));
                Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Flow"));
                Logger.Instance.ILog("Created config directory: " + Path.Combine(dir, "Scripts", "Flow"));
                Directory.CreateDirectory(Path.Combine(dir, "Scripts", "System"));
                Logger.Instance.ILog("Created config directory: " + Path.Combine(dir, "Scripts", "System"));
                Directory.CreateDirectory(Path.Combine(dir, "Plugins"));
                Logger.Instance.ILog("Created config directory: " + Path.Combine(dir, "Plugins"));
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Failed recreating configuration directory '{dir}': {ex.Message}");
                return false;
            }

            foreach (var script in config.FlowScripts)
                await File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "Flow", script.Uid + ".js"), script.Code);
            foreach (var script in config.SystemScripts)
                await File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "System", script.Uid + ".js"), script.Code);
            foreach (var script in config.SharedScripts)
                await File.WriteAllTextAsync(Path.Combine(dir, "Scripts", "Shared", script.Name + ".js"), script.Code);

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool macOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool is64bit = IntPtr.Size == 8;
            foreach (var plugin in config.Plugins)
            {
                var result = await service.DownloadPlugin(plugin, dir);
                if (result.Failed(out string error))
                {
                    Logger.Instance?.ELog(error);
                    return false;
                }

                var zip = result.Value;
                string destDir = Path.Combine(dir, "Plugins", plugin);
                if (Directory.Exists(destDir))
                {
                    Directory.Delete(destDir, true);
                }
                else
                {
                    Directory.CreateDirectory(destDir);
                }
                Logger.Instance?.ILog($"Extracting '{zip}' to '{destDir}'");

                if (ExtractWithOverwrite(zip, destDir).Failed(out error))
                {
                    Logger.Instance?.ELog($"Configuration failed to extract zip '{zip}': " + error);
                    return false;
                }

                File.Delete(zip);

                // check if there are runtime specific files that need to be moved
                foreach (string rdir in windows ? new[] { "win", "win-" + (is64bit ? "x64" : "x86") } :
                         macOs ? new[] { "osx-x64" } : new[] { "linux-x64", "linux" })
                {
                    var runtimeDir = new DirectoryInfo(Path.Combine(destDir, "runtimes", rdir));
                    Logger.Instance?.ILog("Searching for runtime directory: " + runtimeDir.FullName);
                    if (!runtimeDir.Exists) 
                        continue;
                    foreach (var rfile in runtimeDir.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (Regex.IsMatch(rfile.Name, @"\.(dll|so)$") == false)
                                continue;

                            Logger.Instance?.ILog("Trying to move file: \"" + rfile.FullName + "\" to \"" +
                                                  destDir + "\"");
                            rfile.MoveTo(Path.Combine(destDir, rfile.Name));
                            Logger.Instance?.ILog("Moved file: \"" + rfile.FullName + "\" to \"" + destDir + "\"");
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance?.ILog("Failed to move file: " + ex.Message);
                        }
                    }
                }
            }

            var variables = config.Variables;
            if (node.Variables?.Any() == true)
            {
                foreach (var v in node.Variables)
                {
                    variables[v.Key] = v.Value;
                }
            }

            string json = System.Text.Json.JsonSerializer.Serialize(new
            {
                config.Revision,
                config.MaxNodes,
                config.LicenseLevel,
                config.AllowRemote,
                config.Tags,
                config.Resources,
                config.DontUseTempFilesWhenMovingOrCopying,
                Variables = variables,
                config.ManualLibraryPath,
                config.Libraries,
                config.PluginNames,
                config.PluginSettings,
                config.Flows,
                config.FlowScripts,
                config.SharedScripts,
                config.SystemScripts
            });

            string cfgFile = Path.Combine(dir, "config.json");
            if (Environment.GetEnvironmentVariable("FF_NO_ENCRYPT") == "1")
            {
                Logger.Instance?.DLog("Configuration set to no encryption, saving as plain text");
                await File.WriteAllTextAsync(cfgFile, json);
            }
            else
            {
                Logger.Instance?.DLog("Saving encrypted configuration");
                ConfigEncrypter.EncryptConfig(json, cfgFile);
            }

            if (Globals.IsDocker)
            {
                if (await WriteAndRunDockerMods(config.DockerMods ?? new(), node.Uid, node.Name) == false)
                    return false;
            }


            CurrentConfig = config;
            CurrentConfigurationKeepFailedFlowFiles = config.KeepFailedFlowTempFiles;

            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed getting configuration: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
        finally
        {
            _updateConfigSemaphore.Release();
        }
    }
    
    void DeleteDirectory(string dir)
    {
        if (!Directory.Exists(dir))
            return;

        Logger.Instance.ILog("Deleting config directory: " + dir);

        // Ensure all files are writable before attempting to delete them
        foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Failed to delete file '{file}': {ex.Message}");
            }
        }

        // Ensure symbolic links are deleted before the directory itself
        foreach (var subDir in new DirectoryInfo(dir).GetDirectories("*", SearchOption.AllDirectories))
        {
            if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                try
                {
                    subDir.Delete(); // Delete symlink
                }
                catch (Exception ex)
                {
                    Logger.Instance.WLog($"Failed to delete symlink '{subDir.FullName}': {ex.Message}");
                }
            }
        }

        // Retry directory deletion if necessary
        bool deleted = false;
        for (int i = 0; i < 5; i++)  // Retry up to 5 times
        {
            try
            {
                if (Directory.Exists(dir))  // Check again before deleting
                {
                    Directory.Delete(dir, true);
                    deleted = true;
                    Logger.Instance.ILog("Deleted config directory: " + dir);
                }
                break;
            }
            catch (DirectoryNotFoundException)
            {
                Logger.Instance.ILog($"Directory '{dir}' not found, assuming deleted.");
                return; // Exit since it's already gone
            }
            catch (IOException)
            {
                Logger.Instance.WLog($"Directory deletion failed, retrying... {dir}");
                Thread.Sleep(500);  // Wait and retry
            }
        }

        if (!deleted && Directory.Exists(dir))
            Logger.Instance.WLog($"Failed to delete directory '{dir}' after multiple attempts.");
    }

    /// <summary>
    /// Extracts a ZIP archive to the specified directory, overwriting existing files and skipping those that cannot be overwritten.
    /// </summary>
    /// <param name="archivePath">The path to the ZIP file.</param>
    /// <param name="destinationPath">The directory where the files should be extracted.</param>
    public static Result<bool> ExtractWithOverwrite(string archivePath, string destinationPath)
    {
        try
        {
            Logger.Instance.ILog("About to open archive: " + archivePath);

            using Stream stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);

            while (reader.MoveToNextEntry())
            {
                // Determine the type of the archive entry
                if (reader.Entry.IsDirectory)
                {
                    // Skip directories, as we're interested in files
                    continue;
                }

                // Extract the file
                reader.WriteEntryToDirectory(destinationPath, new ExtractionOptions()
                {
                    ExtractFullPath = true, // Extract files with full path, ie in the appropriate sub directories
                    Overwrite = true // Overwrite existing files
                });
            }

            return true;

        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to extract archive: " + ex.Message);
        }
    }


    /// <summary>
    /// Writes and run all the DockerMods
    /// </summary>
    /// <param name="mods">the DockerMods</param>
    /// <param name="nodeUid">the UID of the node</param>
    /// <param name="nodeName">the hostname node this is running on</param>
    async Task<bool> WriteAndRunDockerMods(List<DockerMod> mods, Guid nodeUid, string nodeName)
    {
        var nodeService = ServiceLoader.Load<INodeService>();
        await nodeService.SetStatus(nodeUid, ProcessingNodeStatus.InstallingDockerMods);
        try
        {
            var directory = DirectoryHelper.DockerModsDirectory;
            Logger.Instance.ILog("DockerMods Directory: " + directory);
            if (Directory.Exists(directory) == false)
                Directory.CreateDirectory(directory);

            DockerModHelper.UninstallUnknownMods(mods).Wait();

            if (mods?.Any() != true)
            {
                Logger.Instance.ILog("No DockerMods to run");
                return true;
            }

            mods = mods.OrderBy(x => x.Order).ThenByDescending(x => x.Name.ToLowerInvariant()).ToList();
            Logger.Instance.ILog("DockerMods: \n" +
                                 string.Join("\n", mods.Select(x => $" - {x.Order:0000} - {x.Name} [{x.Revision}] ")));

            foreach (var mod in mods)
            {
                var result = await DockerModHelper.Execute(mod);
                if (result.Failed(out string error))
                {
                    _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Critical,
                        $"Docker Mod '{mod.Name}' Failed on '{nodeName}'",
                        error);
                    return false;
                }
            }

            return true;
        }
        finally
        {
            await nodeService.SetStatus(nodeUid, null);
        }
    }
}