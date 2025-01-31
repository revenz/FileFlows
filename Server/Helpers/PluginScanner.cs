using System.IO.Compression;
using FileFlows.Services;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper class for plugins which allows scanning, updating etc
/// </summary>
public class PluginScanner : IPluginScanner
{
    /// <summary>
    /// Gets the directory where the plugins are stored
    /// </summary>
    /// <returns>the directory where plugins are stored</returns>
    public string GetPluginDirectory() => DirectoryHelper.PluginsDirectory;

    /// <summary>
    /// Scans the disk for plugins
    /// </summary>
    public void Scan()
    {
        Logger.Instance.DLog("Scanning for plugins");
        var pluginDir = GetPluginDirectory();
        Logger.Instance.DLog("Plugin path:" + pluginDir);

        if (FileFlows.Common.Globals.IsDocker)
            EnsureDefaultsExist(pluginDir);

        var service = new PluginService();
        var dbPluginInfos = service.GetAllAsync().Result.OrderBy(x => x.Name).ToList();

        List<string> installed = new List<string>();
        var options = new JsonSerializerOptions
        {
            Converters = { new Validators.ValidatorConverter() }
        };

        // dictionary of languages index by their language code
        Dictionary<string, List<string>> langFiles = new();

        if(Directory.Exists(pluginDir) == false)
        {
            Logger.Instance?.WLog("Plugin directory does not exist: " + pluginDir);
            try
            {
                Directory.CreateDirectory(pluginDir);
            }
            catch(Exception)
            {
                Logger.Instance?.WLog("Failed to create plugin directory: " + pluginDir);
                return;
            }
        }

        foreach (string ffplugin in Directory.GetFiles(pluginDir, "*.ffplugin", SearchOption.AllDirectories))
        {
            Logger.Instance?.DLog("Plugin file found: " + ffplugin);
            try
            {
                using var zf = System.IO.Compression.ZipFile.Open(ffplugin, System.IO.Compression.ZipArchiveMode.Read);
                var entry = zf.GetEntry(".plugininfo");
                if (entry == null)
                {
                    Logger.Instance?.WLog("Unable to find .plugininfo file");
                    continue;
                }
                using var sr = new StreamReader(entry.Open());
                string json = sr.ReadToEnd();

                if (string.IsNullOrEmpty(json))
                {
                    Logger.Instance?.WLog("Unable to read plugininfo from file: " + ffplugin);
                    continue;
                }

                var langEntry = zf.GetEntry("en.json");
                if (langEntry != null)
                {
                    // older plugin
                    using var srLang = new StreamReader(langEntry.Open());
                    if (langFiles.ContainsKey("en") == false)
                        langFiles["en"] = new ();
                    langFiles["en"].Add(srLang.ReadToEnd());
                }
                else
                {
                    // get all files in the i18n directory and read them
                    ExtractLanguageFilesFromZip(zf, langFiles);
                }

                PluginInfo? pi = JsonSerializer.Deserialize<PluginInfo>(json, options);
                if (pi == null)
                {
                    Logger.Instance?.WLog("Unable to parse plugininfo from file: " + ffplugin);
                    continue;
                }

                var plugin = dbPluginInfos.FirstOrDefault(x =>
                {
                    if (x.Uid == pi.Uid)
                        return true;
                    string xpn = x.PackageName.Replace(".ffplugin", string.Empty).ToLower();
                    string pipn = pi.PackageName.Replace(".ffplugin", string.Empty).ToLower();
                    if (xpn == pipn)
                        return true;
                    
                    if(string.Equals(x.Name, pi.Name, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                    return false;
                });
                bool isNew = plugin == null;
                plugin ??= new();
                installed.Add(pi.Name);

                bool isDifferent = false;
                
                plugin.Uid = pi.Uid;
                if (isNew || plugin.PackageName != pi.PackageName)
                {
                    plugin.PackageName = pi.PackageName;
                    isDifferent = true;
                }
                if (isNew || plugin.Name != pi.Name)
                {
                    plugin.Name = pi.Name;
                    isDifferent = true;
                }
                if (isNew || plugin.Version != pi.Version)
                {
                    plugin.Version = pi.Version;
                    isDifferent = true;
                }
                if (isNew || plugin.Deleted)
                {
                    plugin.Deleted = false;
                    isDifferent = true;
                }
                if (isNew || 
                    JsonSerializer.Serialize(plugin.Elements ?? new ()) != 
                    JsonSerializer.Serialize(pi.Elements ?? new ()))
                {
                    plugin.Elements = pi.Elements;
                    isDifferent = true;
                }
                if (isNew || plugin.Authors != pi.Authors)
                {
                    plugin.Authors = pi.Authors;
                    isDifferent = true;
                }
                if (isNew || plugin.Url != pi.Url)
                {
                    plugin.Url = pi.Url;
                    isDifferent = true;
                }
                if (isNew || plugin.Description != pi.Description)
                {
                    plugin.Description = pi.Description;
                    isDifferent = true;
                }
                if (isNew || plugin.Icon != pi.Icon)
                {
                    plugin.Icon = pi.Icon;
                    isDifferent = true;
                }
                if (isNew || 
                    JsonSerializer.Serialize(plugin.Settings ?? new ()) != 
                    JsonSerializer.Serialize(pi.Settings ?? new ()))
                {
                    plugin.Settings = pi.Settings;
                    plugin.HasSettings = pi.Settings?.Any() == true;
                    isDifferent = true;
                }

                Logger.Instance.DLog("Plugin.Name: " + plugin.Name);
                Logger.Instance.DLog("Plugin.PackageName: " + plugin.PackageName);
                Logger.Instance.DLog("Plugin.Version: " + plugin.Version);
                Logger.Instance.DLog("Plugin.Url: " + plugin.Url);
                Logger.Instance.DLog("Plugin.Authors: " + plugin.Authors);

                if (Version.TryParse(pi.Version, out Version? piVersion) && piVersion != null)
                {
                    if (piVersion.Major < 23)
                    {
                        _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Critical,
                            $"'{plugin.Name}' is very old and should be upgraded or deleted",
                            "This plugin is very old and needs upgrading or it has been deprecated and should " +
                            "be removed.\nUsing it will cause unexpected issues");
                    }
                    else
                    {
                        bool old = false;
                        if (piVersion.Major < 24)
                            old = true;
                        else
                        {
                            var oldDate = new DateTime(piVersion.Major + 2000, piVersion.Minor, 1);
                            if (oldDate < new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1))
                                old = true;
                        }

                        if (old)
                        {
                            _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
                                $"'{plugin.Name}' should be upgraded or deleted",
                                "This plugin is either old and needs upgrading or it has been deprecated and should " +
                                "be removed.\nUsing it may cause unexpected issues");
                        }
                    }
                }

                if (isNew == false)
                {
                    if (isDifferent)
                    {
                        Logger.Instance.ILog("Updating plugin: " + pi.Name);
                        plugin.DateModified = DateTime.UtcNow;
                        service.Update(plugin, auditDetails: AuditDetails.ForServer()).Wait();
                    }
                }
                else
                {
                    // new dll

                    plugin.DateModified = DateTime.UtcNow;
                    Logger.Instance.ILog("Adding new plugin: " + pi.Name);
                    plugin.Name = pi.Name;
                    plugin.DateCreated = DateTime.UtcNow;
                    plugin.DateModified = DateTime.UtcNow;
                    plugin.Enabled = true;
                    service.Update(plugin, auditDetails: AuditDetails.ForServer()).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog($"Failed to scan plugin {ffplugin}: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        foreach (var plugin in dbPluginInfos.Where(x => installed.Contains(x.Name) == false))
        {
            if (String.IsNullOrEmpty(plugin.PackageName))
            {
                Logger.Instance.DLog("Delete old plugin: " + plugin.Name);
                // its an old style plugin, perm delete it
                service.Delete(new[] { plugin.Uid }, AuditDetails.ForServer()).Wait();
            }
            else
            {
                Logger.Instance.DLog("Missing plugin: " + plugin.Name);
                // mark as deleted.
                plugin.Deleted = true;
                plugin.DateModified = DateTime.UtcNow;
                service.Update(plugin, auditDetails: AuditDetails.ForServer()).Wait();
            }
        }

        foreach(var key in langFiles.Keys)
            CreateLanguageFile(langFiles[key], key);

        Logger.Instance.ILog("Finished scanning for plugins");
    }

    /// <summary>
    /// Extracts JSON language files from a ZipArchive.
    /// </summary>
    /// <param name="zf">The ZipArchive containing language files.</param>
    /// <param name="langFiles">A dictionary to store language files.</param>
    static void ExtractLanguageFilesFromZip(ZipArchive zf, Dictionary<string, List<string>> langFiles)
    {
        var i18nEntries = zf.Entries.Where(e => e.FullName.StartsWith("i18n") && e.FullName.EndsWith(".json"))
            .ToArray();

        foreach (var entry in i18nEntries)
        {
            // Get the language code from the entry name
            string languageCode = Path.GetFileNameWithoutExtension(entry.Name);

            // Read the contents of the JSON file
            using var sr = new StreamReader(entry.Open());
            string jsonContent = sr.ReadToEnd();

            // Add the JSON content to the dictionary
            if (!langFiles.ContainsKey(languageCode))
            {
                langFiles[languageCode] = new List<string>();
            }

            langFiles[languageCode].Add(jsonContent);
        }
    }

    /// <summary>
    /// This ensures the default plugins exist.
    /// Used by the docker version to copy the default plugins from the internal docker directory to
    /// mapped external plugins directory if the default plugins are missing
    /// </summary>
    /// <param name="pluginDir">the external plugin directory</param>
    private static void EnsureDefaultsExist(string pluginDir)
    {
        Logger.Instance.ILog("PluginScanner: Ensuring default plugins exist: " + pluginDir);
        DirectoryInfo di = new DirectoryInfo(pluginDir);
        FileFlows.Helpers.FileHelper.CreateDirectoryIfNotExists(di.FullName);

        var rootPlugins = new DirectoryInfo(Path.Combine(DirectoryHelper.BaseDirectory, "Server/Plugins"));

        if (rootPlugins.Exists == false)
        {
            Logger.Instance?.ILog("PluginScanner: Root plugin directory not found: " + rootPlugins);
            return;
        }
        var pluginFiles = rootPlugins.GetFiles("*.ffplugin");
        Logger.Instance?.ILog($"PluginScanner: Root plugins found: {pluginFiles.Length} in: {rootPlugins.FullName}");
        foreach (var file in pluginFiles)
        {
            Logger.Instance?.ILog($"PluginScanner: Root plugin: {file.FullName}");
            string dest = Path.Combine(pluginDir, file.Name);

            if (File.Exists(dest))
            {
                // make sure the existing plugin is not newer than the docker plugin
                var existing = GetFFPluginVersion(dest);
                var dockerVersion = GetFFPluginVersion(file.FullName);

                if(existing >= dockerVersion)
                {
                    Logger.Instance?.DLog("PluginScanner: Existing plugin newer than docker plugin: " + file.Name);
                    continue;
                }
            }

            Logger.Instance?.ILog("PluginScanner: Restoring default plugin: " + file.Name);
            file.CopyTo(dest, true);
        }
    }

    /// <summary>
    /// Gets the version of a specified plugin
    /// </summary>
    /// <param name="ffplugin">the plugin to get the version of</param>
    /// <returns>the version of the plugin</returns>
    private static Version GetFFPluginVersion(string ffplugin)
    {
        if (File.Exists(ffplugin) == false)
            return new Version();
        try
        {
            using var zf = System.IO.Compression.ZipFile.Open(ffplugin, System.IO.Compression.ZipArchiveMode.Read);
            var entry = zf.GetEntry(".plugininfo");
            if (entry == null)
            {
                Logger.Instance?.WLog("PluginScanner: Unable to find .plugininfo file");
                return new Version();
            }
            using var sr = new StreamReader(entry.Open());
            string json = sr.ReadToEnd();

            if (string.IsNullOrEmpty(json))
            {
                Logger.Instance?.WLog("PluginScanner: Unable to read plugininfo from file: " + ffplugin);
                return new Version();
            }
            var options = new JsonSerializerOptions
            {
                Converters = { new Validators.ValidatorConverter() }
            };

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            PluginInfo pi = JsonSerializer.Deserialize<PluginInfo>(json, options);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return Version.Parse(pi.Version);
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("PluginScanner: Failed to get plugin version: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return new Version();
        }
    }

    /// <summary>
    /// Updates a plugin with a new version
    /// </summary>
    /// <param name="packageName">the plugin package name</param>
    /// <param name="data">the binary data of the plugin (ffplugin byte[] data)</param>
    /// <returns>true if successful</returns>
    public bool UpdatePlugin(string packageName, byte[] data)
    {
        if (string.IsNullOrEmpty(packageName))
            throw new InvalidDataException("PackageName is required");
        try
        {
            string dest = Path.Combine(GetPluginDirectory(), packageName);
            if (dest.EndsWith(".ffplugin") == false)
                dest += ".ffplugin";

            // save the plugin
            File.WriteAllBytes(dest, data);
            Logger.Instance.ILog("PluginScanner: Saving plugin : " + dest);

            // rescan for plugins
            Scan();

            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed updating plugin: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
    }

    /// <summary>
    /// Deletes a plugin package from disk
    /// </summary>
    /// <param name="packageName">the name of the plugin package to delete (without the .ffplugin extension)</param>
    public void Delete(string packageName)
    {
        string file = Path.Combine(GetPluginDirectory(), packageName + ".ffplugin");
        if(File.Exists(file))
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }

    /// <summary>
    /// Creates a combined language file for all installed plugins
    /// Saves to wwwroot/i18n/plugins.en.json
    /// </summary>
    /// <param name="jsonFiles">the individual plugin language files</param>
    /// <param name="langCode">the language code for the file</param>
    static void CreateLanguageFile(List<string> jsonFiles, string langCode)
    {
        var json = "{}";
        try
        {
            foreach (var jf in jsonFiles)
            {
                try
                {
                    string updated = JsonHelper.Merge(json, jf);
                    json = updated;
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Error loading plugin json[0]:" + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error loading plugin json[1]:" + ex.Message + Environment.NewLine + ex.StackTrace);
        }
#if (DEBUG)
        var dir = "wwwroot/i18n";
#else
        var dir = Path.Combine(DirectoryHelper.BaseDirectory, "Server/wwwroot/i18n");
#endif

        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, $"plugins.{langCode}.json"), json);        
    }
}
