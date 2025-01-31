
using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Services;

namespace FileFlows.Server.Upgrade;

        
/// <summary>
/// Run upgrade from 24.09.2
/// </summary>
public class Upgrade_24_09_2 : UpgradeBase
{
    /// <summary>
    /// Initializes the upgrade for 24.09.2
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="settingsService">the settings service</param>
    /// <param name="upgradeManager">the upgrade manager</param>
    public Upgrade_24_09_2(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
        : base(logger, settingsService, upgradeManager)
    {
    }
    
    /// <summary>
    /// Runs the upgrade
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public Result<bool> Run()
        => UpgradeManager.Run_Upgrade_24_09_2(Logger, DbType, ConnectionString);
}