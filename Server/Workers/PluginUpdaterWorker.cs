using FileFlows.Helpers;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to update plugins
/// </summary>
public class PluginUpdaterWorker : ServerWorker
{
    /// <summary>
    /// Constructs a new plugin update worker
    /// </summary>
    public PluginUpdaterWorker() : base(ScheduleType.Daily, 5)
    {
        Trigger();
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings? settings)
    {
#if (DEBUG)
        settings = null;
#endif
        if (settings?.AutoUpdatePlugins != true)
            return;

        Logger.Instance?.ILog("Plugin Updater started");
        var service = ServiceLoader.Load<PluginService>();
        var plugins = service.GetAllAsync().Result;
        var latestPackagesResult = ServiceLoader.Load<PluginService>().GetPluginPackagesActual().Result;
        var latestPackages = latestPackagesResult.IsFailed ? new () 
            : latestPackagesResult.Value;

        var pluginDownloader = new PluginDownloader();
        var pluginScanner = ServiceLoader.Load<IPluginScanner>();
        foreach(var plugin in plugins)
        {
            try
            {
                var package = latestPackages?.Where(x => x?.Package == plugin?.PackageName)?.FirstOrDefault();
                if (package == null)
                    continue; // no plugin, so no update

                if (Version.Parse(package.Version) <= Version.Parse(plugin.Version))
                {
                    // no new version, cannot update
                    continue;
                }

                var dlResult = pluginDownloader.Download(Version.Parse(package.Version), 
                    package.Package, PluginService.GetXCode()
                    ).Result;

                if (dlResult.Success == false)
                {
                    Logger.Instance.WLog($"Failed to download package '{plugin.PackageName}' update");
                    continue;
                }
                pluginScanner.UpdatePlugin(package.Package, dlResult.Data);
            }
            catch(Exception ex)
            {
                Logger.Instance.WLog($"Failed to update plugin '{plugin.PackageName}': " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        Logger.Instance?.ILog("Plugin Updater finished");
    }
}
