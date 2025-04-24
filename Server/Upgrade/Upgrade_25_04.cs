
using FileFlows.Managers.InitializationManagers;
using FileFlows.Services;

namespace FileFlows.Server.Upgrade;

        
/// <summary>
/// Run upgrade from 25.04
/// </summary>
public class Upgrade_25_04 : UpgradeBase
{
    /// <summary>
    /// Initializes the upgrade for 25.04
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="settingsService">the settings service</param>
    /// <param name="upgradeManager">the upgrade manager</param>
    public Upgrade_25_04(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
        : base(logger, settingsService, upgradeManager)
    {
    }
    
    /// <summary>
    /// Runs the upgrade
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public Result<bool> Run()
        => UpgradeManager.Run_Upgrade_25_04(Logger, DbType, ConnectionString);
}