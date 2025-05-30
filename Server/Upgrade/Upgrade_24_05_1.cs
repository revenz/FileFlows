
using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

        
/// <summary>
/// Run upgrade from 24.05.1
/// </summary>
public class Upgrade_24_05_1 : UpgradeBase
{
    /// <summary>
    /// Initializes the upgrade for 24.05.1
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="settingsService">the settings service</param>
    /// <param name="upgradeManager">the upgrade manager</param>
    public Upgrade_24_05_1(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
        : base(logger, settingsService, upgradeManager)
    {
    }
    
    /// <summary>
    /// Runs the upgrade
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public Result<bool> Run()
     => UpgradeManager.Run_Upgrade_24_05_1(Logger, DbType, ConnectionString);
}