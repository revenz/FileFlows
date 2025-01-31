using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Runs the upgrades
/// </summary>
public class Upgrader
{
    // update this with the latest db version
    private readonly Version LATEST_DB_VERSION = new Version(24, 09, 2, 3545);

    /// <summary>
    /// Gets an instance of the upgrade manager
    /// </summary>
    /// <param name="settings">the application settings</param>
    /// <returns>an instance of the upgrade manager</returns>
    private UpgradeManager GetUpgradeManager(AppSettings settings)
        => new (Logger.Instance, settings.DatabaseType, settings.DatabaseConnection);
    
    /// <summary>
    /// Checks if an upgrade is required
    /// </summary>
    /// <param name="settings">the application settings</param>
    /// <returns>true if an upgrade is required, otherwise false</returns>
    internal Result<(bool Required, Version Current)> UpgradeRequired(AppSettings settings)
    {
        var manager = GetUpgradeManager(settings);
        
        var versionResult = manager.GetCurrentVersion().Result;
        if (versionResult.Failed(out string error))
            return Result<(bool Required, Version Current)>.Fail(error);
        if (versionResult.Value == null)
            return (false, new Version()); // database has not been initialized

        var version = versionResult.Value;
        Logger.Instance.ILog("Current Database Version: " + version);
        Logger.Instance.ILog("Expected Database Version: " + LATEST_DB_VERSION);
        return (version < LATEST_DB_VERSION, version);
    }

    /// <summary>
    /// Backup the current database
    /// </summary>
    /// <param name="currentVersion">the current version in the database</param>
    /// <param name="settings">the application settings</param>
    /// <param name="updateStatusCallback">callback to update additional status information</param>
    internal void Backup(Version currentVersion, AppSettings settings, Action<string> updateStatusCallback)
    {
        // backup server.config
        string serverConfig = Path.Combine(DirectoryHelper.DataDirectory, "server.config");
        string scBackup = serverConfig.Replace(".config",
            "-" + currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build +
            ".config");
        if(File.Exists(serverConfig))
            File.Copy(serverConfig, scBackup, true);
        
        // first backup the database
        if (settings.DatabaseType == DatabaseType.Sqlite)
        {
            try
            {
                Logger.Instance.ILog("Backing up database");
                string source = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
                string dbBackup = source.Replace(".sqlite",
                    "-" + currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build +
                    ".sqlite.backup");
                File.Copy(source, dbBackup);
                Logger.Instance.ILog("Backed up database to: " + dbBackup);
            }
            catch (Exception)
            {
            }
        }
        else if(settings.DontBackupOnUpgrade == false)
        {
            try
            {
                Logger.Instance.ILog("Backing up database, please wait this may take a while");
                string dbBackup = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows-" +
                    currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build +
                    ".sqlite.backup");
                var manager = new MigrationManager(Logger.Instance,
                    new() { Type = settings.DatabaseType, ConnectionString = settings.DatabaseConnection },
                    new () { Type = DatabaseType.Sqlite, ConnectionString = SqliteHelper.GetConnectionString(dbBackup) }
                );
                var result = manager.Migrate(backingUp: true, updateStatusCallback);
                if(result.Failed(out string error))
                    Logger.Instance.ILog("Failed to backup database: " + error);
                else
                    Logger.Instance.ILog("Backed up database to: " + dbBackup);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Failed creating database backup: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Run the updates
    /// </summary>
    /// <param name="currentVersion">the current version in the database</param>
    /// <param name="appSettingsService">the application settings service</param>
    /// <param name="statusCallback">Callback to update the status</param>
    internal Result<bool> Run(Version currentVersion, AppSettingsService appSettingsService, Action<string> statusCallback)
    {
        Logger.Instance.ILog("Current version: " + currentVersion);
        // check if current version is even set, and only then do we run the upgrades
        // so on a clean install these do not run
        if (currentVersion < new Version(23, 0))
            return true; // way to old, or new install
        
        var manager = GetUpgradeManager(appSettingsService.Settings);
        DataLayer.Helpers.Decrypter.EncryptionKey = appSettingsService.Settings.EncryptionKey;
        if (currentVersion < new Version(24, 2))
        {
            statusCallback("Running 24.02.1 upgrade");
            Logger.Instance.ILog("Running 24.02.1 upgrade");
            if(new Upgrade_24_02(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.02.1 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }

        if (currentVersion < new Version(24, 3, 2))
        {
            statusCallback("Running 24.03.2 upgrade");
            Logger.Instance.ILog("Running 24.03.2 upgrade");
            if(new Upgrade_24_03_2(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.03.2 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }

        if (currentVersion < new Version(24, 3, 5))
        {
            statusCallback("Running 24.03.5 upgrade");
            Logger.Instance.ILog("Running 24.03.5 upgrade");
            if(new Upgrade_24_03_5(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.03.5 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }

        if (currentVersion < new Version(24, 4, 1))
        {
            statusCallback("Running 24.04.1 upgrade");
            Logger.Instance.ILog("Running 24.04.1 upgrade");
            if(new Upgrade_24_04_1(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.04.1 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }

        if (currentVersion < new Version(24, 5, 1, 3143))
        {
            statusCallback("Running 24.05.1 upgrade");
            Logger.Instance.ILog("Running 24.05.1 upgrade");
            if(new Upgrade_24_05_1(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.05.1 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }
        
        if (currentVersion < new Version(24, 6, 1, 3217))
        {
            statusCallback("Running 24.06.1 upgrade");
            Logger.Instance.ILog("Running 24.06.1 upgrade");
            if(new Upgrade_24_06_1(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.06.1 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }

        if (currentVersion < new Version(24, 7, 2, 3402))
        {
            statusCallback("Running 24.07.2 upgrade");
            Logger.Instance.ILog("Running 24.07.2 upgrade");
            if(new Upgrade_24_07_2(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.07.2 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }
        
        if (currentVersion < new Version(24, 8, 1, 3448))
        {
            statusCallback("Running 24.08.1 upgrade");
            Logger.Instance.ILog("Running 24.08.1 upgrade");
            if(new Upgrade_24_08_1(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.08.1 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }
        
        if (currentVersion < new Version(24, 9, 2, 3545))
        {
            statusCallback("Running 24.09.2 upgrade");
            Logger.Instance.ILog("Running 24.09.2 upgrade");
            if(new Upgrade_24_09_2(Logger.Instance, appSettingsService, manager).Run().Failed(out string error))
            {
                Logger.Instance.ELog("24.09.2 Upgrade failed: " + error);
                return Result<bool>.Fail(error);
            }
        }
        
        // save the settings
        Logger.Instance.ILog("Saving version to database");
        var result = manager.SaveCurrentVersion().Result;
        if (result.IsFailed)
            return result;

        Logger.Instance.ILog("Finished checking upgrade scripts");
        return true;
    }

    /// <summary>
    /// Ensures the expected columns exist
    /// </summary>
    /// <param name="appSettingsService">the app settings service</param>
    public void EnsureColumnsExist(AppSettingsService appSettingsService)
    {
        var manager = GetUpgradeManager(appSettingsService.Settings);
        new DatabaseValidator(Logger.Instance, appSettingsService, manager).EnsureColumnsExist().Wait();
    }

    /// <summary>
    /// Ensures any default values exist
    /// </summary>
    /// <param name="appSettingsService">the app settings service</param>
    public void EnsureDefaultsExist(AppSettingsService appSettingsService)
    {
        var manager = GetUpgradeManager(appSettingsService.Settings);
        new DatabaseValidator(Logger.Instance, appSettingsService, manager).EnsureDefaultsExist().Wait();
    }
}
