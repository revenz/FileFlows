using FileFlows.Managers.InitializationManagers;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Base class for upgrades
/// </summary>
public abstract class UpgradeBase
{
    
    /// <summary>
    /// the logger
    /// </summary>
    protected ILogger Logger { get; private set; }
    /// <summary>
    /// The upgrade manager
    /// </summary>
    protected UpgradeManager UpgradeManager { get; private set; }
    /// <summary>
    /// The database type
    /// </summary>
    protected DatabaseType DbType { get; private set; }
    /// <summary>
    /// The database connection string
    /// </summary>
    protected string ConnectionString { get; private set; }
    
    /// <summary>
    /// The application settings service
    /// </summary>
    protected AppSettingsService SettingsService { get; private set; }
    
    /// <summary>
    /// Initializes the upgrade for 24.03.2
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="settingsService">the settings service</param>
    /// <param name="upgradeManager">the upgrade manager</param>
    public UpgradeBase(ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
    {
        Logger = logger;
        SettingsService = settingsService;
        UpgradeManager = upgradeManager;
        DbType = settingsService.Settings.DatabaseType;
        ConnectionString = settingsService.Settings.DatabaseConnection;
    }
}