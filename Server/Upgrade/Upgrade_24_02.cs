using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v24.02
/// </summary>
public class Upgrade_24_02 : UpgradeBase
{
    /// <summary>
    /// Initializes the upgrade for 24.03.2
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="settingsService">the settings service</param>
    /// <param name="upgradeManager">the upgrade manager</param>
    public Upgrade_24_02(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
        : base(logger, settingsService, upgradeManager)
    {
    }
    
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public Result<bool> Run()
    {
        Logger.ILog("Upgrade running, running 24.02 upgrade script");
        SetServerPort();
        return UpgradeManager.Run_Upgrade_24_02(Logger, DbType, ConnectionString);
    }

    /// <summary>
    /// Sets the server port to 5000
    /// </summary>
    private void SetServerPort()
    {
        if (SettingsService.Settings.ServerPort != null && SettingsService.Settings.ServerPort >= 1 &&
            SettingsService.Settings.ServerPort <= 65535)
            return;
        Logger.ILog("Saving server port 5000");
        SettingsService.Settings.ServerPort = 5000;
        SettingsService.Save();
    }
}