using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.03.5
/// </summary>
public class Upgrade_24_03_5
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        using var db = connector.GetDb(true).Result;

        RemoveLibraryFiles(logger, db, connector.WrapFieldName);
        FixPluginSettings(logger, db, connector.WrapFieldName);
        CreateFileFlowsTable(logger, db, connector.WrapFieldName);

        return true;
    }


    /// <summary>
    /// Create the file flows table
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="Wrap">the Wrap method</param>
    private void CreateFileFlowsTable(ILogger logger, DatabaseConnection db, Func<string, string> Wrap)
    {
        try
        {
            db.Db.Execute($@"DROP TABLE {Wrap("FileFlows")}");
        }
        catch (Exception)
        {
        }

        logger.ILog("Created FileFlows database table");
        db.Db.Execute($@"
CREATE TABLE {Wrap("FileFlows")}
(
    {Wrap("Version")}       VARCHAR(36)        NOT NULL
)");
    }

    /// <summary>
    /// Fixes the plugin settings
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="Wrap">the Wrap method</param>
    private void FixPluginSettings(ILogger logger, DatabaseConnection db, Func<string, string> Wrap)
    {
        logger.ILog("Fixing plugin settings");
        
        var objects = db.Db.Fetch<DbObject>($"select * from {Wrap(nameof(DbObject))} " +
                                            $" where {Wrap(nameof(DbObject.Type))} = 'FileFlows.ServerShared.Models.PluginSettingsModel'")
            .ToDictionary(x => x.Name);
        
        foreach (var key in objects.Keys)
        {
            if (key.StartsWith("PluginSettings_") == false)
                continue;
            string name = key["PluginSettings_".Length..];
            if (objects.ContainsKey(name))
            {
                // delete it
                logger.ILog("Deleting duplicate plugin settings: " + name);
                db.Db.Execute(
                    $"delete from {Wrap(nameof(DbObject))} where {Wrap(nameof(DbObject.Uid))} = '{objects[key].Uid}'");
            }
            else
            {
                // update it
                logger.ILog("Updating plugin settings name");
                db.Db.Execute(
                    $"update {Wrap(nameof(DbObject))} set {Wrap(nameof(DbObject.Name))} = '{name}'" +
                    $" where {Wrap(nameof(DbObject.Uid))} = '{objects[key].Uid}'");
                
            }
        }
    }


    /// <summary>
    /// Removes the library files with missing libraries
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="db">the db connection</param>
    /// <param name="Wrap">the Wrap method</param>
    private void RemoveLibraryFiles(ILogger logger, DatabaseConnection db, Func<string, string> Wrap)
    {
        string sql =
            $"select {Wrap(nameof(DbObject.Uid))} from {Wrap(nameof(DbObject))} " +
            $" where {Wrap(nameof(DbObject.Type))} = '{typeof(Library).FullName}'";

        var knownLibraries = db.Db.Fetch<Guid>(sql);

        if (knownLibraries.Any())
            return;

        string inStr = string.Join(",", knownLibraries.Select(x => $"'{x}'"));


        logger.ILog("Deleting rogue Library Files");
        db.Db.Execute(
            $"delete from {Wrap(nameof(LibraryFile))} where {Wrap(nameof(LibraryFile.LibraryUid))} not in ({inStr})");

    }
}