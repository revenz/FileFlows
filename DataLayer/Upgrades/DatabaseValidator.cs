using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Database Valdiator
/// </summary>
public class DatabaseValidator
{
    /// <summary>
    /// Ensures columns exist in the database
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>true if successful, otherwise false</returns>
    public async Task<Result<bool>> EnsureColumnsExist(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        try
        {
            foreach (var column in new[]
                     {
                         ("LibraryFile", "FailureReason", "TEXT", "''"),
                         ("LibraryFile", "ProcessOnNodeUid", "varchar(36)", "''"),
                         ("LibraryFile", "CustomVariables", "TEXT", ""),
                         ("LibraryFile", "Additional", "TEXT", ""),
                         ("LibraryFile", "Tags", "TEXT", "")
                     })
            {
                if (await connector.ColumnExists(column.Item1, column.Item2) == false)
                {
                    logger.ILog("Adding LibraryFile.FailureReason column");
                    await connector.CreateColumn(column.Item1, column.Item2, column.Item3, column.Item4);
                }
            }
            
            // this is needed since 25.01 shipped with ResellerSettings instead of FileDropSettings
            // in a few months this can be deleted
            using var db = await connector.GetDb();
            await db.Db.ExecuteAsync(
                $"delete from {connector.WrapFieldName("DbObject")} where {connector.WrapFieldName("Type")} = @0",
                "FileFlows.Shared.Models.ResellerSettings");

            return true;
        }
        catch (Exception ex)
        {
            logger.ELog("Failed ensuring columns exist: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Ensures default values exist in the database
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <param name="objects">the objects to ensure exist</param>
    /// <returns>true if successful, otherwise false</returns>
    internal async Task<Result<bool>> EnsureDefaultsExist(ILogger logger, DatabaseType dbType, string connectionString, FileFlowObject[] objects)
    {
        try
        {
            var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
            var dbom = new DbObjectManager(logger, dbType, connector);
            var manager = new FileFlowsObjectManager(dbom);
            
            foreach (var obj in objects)
            {
                if(await manager.Exists(obj.Uid))
                    continue;

                await manager.AddOrUpdateObject(obj, null);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            logger.ELog("Failed ensuring columns exist: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<bool>.Fail(ex.Message);
        }
    }
}