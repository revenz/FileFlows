using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Validates the database and
/// </summary>
/// <param name="logger">the logger to use</param>
/// <param name="settingsService">the settings service</param>
/// <param name="upgradeManager">the upgrade manager</param>
public class DatabaseValidator(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
    : UpgradeBase(logger, settingsService, upgradeManager)
{

    /// <summary>
    /// Ensures all the required columns exist
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public async Task<Result<bool>> EnsureColumnsExist()
        => await UpgradeManager.EnsureColumnsExist(Logger, DbType, ConnectionString);

    /// <summary>
    /// Ensures any default values exist
    /// </summary>
    public async Task<Result<bool>> EnsureDefaultsExist()
    {
        FileFlowObject[] defaultObjects =
        [
            new Library()
            {
                Uid = CommonVariables.ManualLibraryUid,
                Name = CommonVariables.ManualLibrary,
                MaxRunners = 1,
                Enabled = true,
                ProcessingOrder = ProcessingOrder.AsFound,
                Priority = ProcessingPriority.Normal,
                Schedule = new string ('1', 672)
            }
        ];
        return await UpgradeManager.EnsureDefaultsExist(Logger, DbType, ConnectionString, defaultObjects);
    }
}