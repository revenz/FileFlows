using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Migrates one database to another database
/// </summary>
internal class DbMigrator
{
    /// <summary>
    /// The logger to use
    /// </summary>
    private readonly ILogger Logger;
    
    /// <summary>
    /// Initialises a new instance of the Database Migrator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    public DbMigrator(ILogger logger)
    {
        Logger = logger;
        FileFlowsDb.Logger = logger;
    }
    
    /// <summary>
    /// Migrates data from one database to another
    /// </summary>
    /// <param name="sourceInfo">the source database</param>
    /// <param name="destinationInfo">the destination database</param>
    /// <param name="backingUp">true if performing a backup, otherwise false</param>
    /// <param name="updateStatusCallback">callback to update additional status information</param>
    /// <returns>if the migration was successful</returns>
    public Result<bool> Migrate(DatabaseInfo sourceInfo, DatabaseInfo destinationInfo, bool backingUp = false, Action<string>? updateStatusCallback = null)
    {
        try
        {
            Logger?.ILog($"Database {(backingUp ? "Backup" : "Migration")} started");

            var source = DatabaseAccessManager.FromType(Logger!, sourceInfo.Type, sourceInfo.ConnectionString);
            // we do not want cache as this will break if the db doesnt exist yet
            var destType = destinationInfo.Type is DatabaseType.Sqlite 
                ? DatabaseType.SqliteNonCached
                : destinationInfo.Type; 
            var dest = DatabaseAccessManager.FromType(Logger!, destType, destinationInfo.ConnectionString);

            var destExternal = dest.Type is DatabaseType.Postgres or DatabaseType.MySql or DatabaseType.SqlServer;
            if(destExternal == false)
            {
                // move the db if it exists so we can create a new one
                SQLiteConnectorNewConnection.MoveFileFromConnectionString(destinationInfo.ConnectionString);
            }

            var destCreator = DatabaseCreator.Get(Logger!, dest.Type, destinationInfo.ConnectionString);
            updateStatusCallback?.Invoke("Creating Database");
            var result = destCreator.CreateDatabase(recreate: true);
            if (result.Failed(out string error))
                return Result<bool>.Fail("Failed creating destination database: " + error);
            
            var structureResult = destCreator.CreateDatabaseStructure();
            if(structureResult.Failed(out error))
                return Result<bool>.Fail("Failed creating destination database structure: " + error);
            if(structureResult.Value == false)
                return Result<bool>.Fail("Failed creating destination database structure");
            
            Logger?.ILog((backingUp ? "Backing up" : "Migrating") + " database objects");
            updateStatusCallback?.Invoke((backingUp ? "Backing up" : "Migrating") + " database objects");
            MigrateDbObjects(source, dest);
            
            Logger?.ILog((backingUp ? "Backing up" : "Migrating") + " library files");
            updateStatusCallback?.Invoke((backingUp ? "Backing up" : "Migrating") + " library files");
            MigrateLibraryFiles(source, dest);
            
            Logger?.ILog((backingUp ? "Backing up" : "Migrating") + " statistics");
            updateStatusCallback?.Invoke((backingUp ? "Backing up" : "Migrating") + " statistics");
            MigrateDbStatistics(source, dest);
            
            Logger?.ILog((backingUp ? "Backing up" : "Migrating") + " revisions");
            updateStatusCallback?.Invoke((backingUp ? "Backing up" : "Migrating") + " revisions");
            MigrateRevisions(source, dest);

            MigrateVersion(source, dest);
            
            Logger?.ILog($"Database {(backingUp ? "backup" : "migration")} complete");
            return true;
        }
        catch (Exception ex)
        {
            Logger?.ELog($"Failed to {(backingUp ? "backup" : "migrate")} data: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<bool>.Fail($"Failed to {(backingUp ? "backup" : "migrate")} data: " + ex.Message);
        }
    }

    /// <summary>
    /// Sets the version in the database
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateVersion(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var version = source.VersionManager.Get().Result;
        if(version != null)
            dest.VersionManager.Set(version.ToString()).Wait();
    }

    /// <summary>
    /// Migrates the DbObjects from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateDbObjects(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbObjects = source.ObjectManager.GetAll().Result.ToArray();
        if (dbObjects?.Any() != true)
            return;

        foreach (var obj in dbObjects)
        {
            Logger?.DLog($"Migrating [{obj.Uid}][{obj.Type}]: {obj.Name ?? string.Empty}");

            try
            {
                dest.ObjectManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger?.ELog("Failed migrating: " +  ex.Message);
                Logger?.ELog("Migration Object: " + JsonSerializer.Serialize(obj));
                throw;
            }
        }
    }

    /// <summary>
    /// Migrates database statistics from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateDbStatistics(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbStatistics = source.StatisticManager.GetAll().Result;
        if (dbStatistics?.Any() != true)
            return;

        try
        {
            dest.StatisticManager.InsertBulk(dbStatistics.ToArray()).Wait();
        }
        catch (Exception ex)
        {
            Logger?.WLog("Failed migrating database statistic: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Migrates revisions from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateRevisions(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbRevisions = source.RevisionManager.GetAll().Result;
        if (dbRevisions?.Any() != true)
            return;

        foreach (var obj in dbRevisions)
        {
            try
            {
                dest.RevisionManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger?.WLog("Failed migrating object revision: " + ex.Message);
            }
        }
    }
    
    /// <summary>
    /// Migrates library files from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateLibraryFiles(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var items = source.LibraryFileManager.GetAll().Result;
        if (items?.Any() != true)
            return;

        foreach (var obj in items)
        {
            try
            {
                dest.LibraryFileManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger.ELog($"Failed migrating library file '{obj.Name}': " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Checks if a database exists
    /// </summary>
    /// <param name="type">the type of database to check</param>
    /// <param name="connectionString">the connection string</param>
    /// <returns>true if exists, otherwise false</returns>
    public static Result<bool> DatabaseExists(DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.MySql:
                return MySqlDatabaseCreator.DatabaseExists(connectionString);
            case DatabaseType.SqlServer:
                return SqlServerDatabaseCreator.DatabaseExists(connectionString);
            case DatabaseType.Postgres:
                return PostgresDatabaseCreator.DatabaseExists(connectionString);
            default:
                return SQLiteDatabaseCreator.DatabaseExists(connectionString);
        }
    }
}