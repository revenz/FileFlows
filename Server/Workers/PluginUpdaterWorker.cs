﻿using FileFlows.Helpers;
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

        _ = ExecuteAsync();
    }

    private async Task ExecuteAsync()
    {
        Logger.ILog("Plugin Updater started");
        var service = ServiceLoader.Load<PluginService>();
        var plugins = await service.GetAllAsync();
        var latestPackagesResult = await ServiceLoader.Load<PluginService>().GetPluginPackagesActual();
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

                var dlResult = await pluginDownloader.Download(Version.Parse(package.Version), 
                    package.Package, PluginService.GetXCode()
                    );

                if (dlResult.Success == false)
                {
                    Logger.WLog($"Failed to download package '{plugin.PackageName}' update");
                    continue;
                }
                await pluginScanner.UpdatePlugin(package.Package, dlResult.Data);
            }
            catch(Exception ex)
            {
                Logger.WLog($"Failed to update plugin '{plugin.PackageName}': " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        Logger.ILog("Plugin Updater finished");
    }
}
