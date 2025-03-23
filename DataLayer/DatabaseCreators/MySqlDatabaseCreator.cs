using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using NPoco;
using NPoco.DatabaseTypes;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// A MySQL database creator
/// </summary>
public class MySqlDatabaseCreator : IDatabaseCreator
{
    /// <summary>
    /// The connection string to the database
    /// </summary>
    private string ConnectionString { get; init; }
    /// <summary>
    /// The logger to use
    /// </summary>
    private ILogger Logger;
    
    /// <summary>
    /// Initializes an instance of the MySQL database creator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    public MySqlDatabaseCreator(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Checks if the MySql database exists
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>true if exists, otherwise false</returns>
    internal static Result<bool> DatabaseExists(string connectionString)
    {
        try
        {
            string connString = Regex.Replace(connectionString, "(^|;)Database=[^;]+", "");
            if (connString.StartsWith(";"))
                connString = connString[1..];
            string dbName = GetDatabaseName(connectionString);

            using var db = new Database(connString, new MySqlDatabaseType(), MySqlConnectorFactory.Instance);
            bool exists =
                string.IsNullOrEmpty(db.ExecuteScalar<string>(
                    "select schema_name from information_schema.schemata where schema_name = @0", dbName)) == false;
            return exists;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
    
    /// <inheritdoc />
    public Result<DbCreateResult> CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = GetDatabaseName(ConnectionString);
        
        using var db = new Database(connString, new MySqlDatabaseType(), MySqlConnectorFactory.Instance);
        bool exists = DatabaseExists(ConnectionString);
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.ILog("Dropping existing database");
            db.Execute($"drop database {dbName};");
        }

        Logger.ILog("Creating Database");
        bool created = db.Execute("create database " + dbName + " character set utf8 collate 'utf8_unicode_ci';") > 0;
        if (created)
            return DbCreateResult.Created;
        return Result<DbCreateResult>.Fail("Failed to create database");
    }
    
    /// <summary>
    /// Gets the database name from the connection string
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database name</returns>
    private static string GetDatabaseName(string connectionString)
        => Regex.Match(connectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;
    
    
    /// <inheritdoc />
    public Result<bool> CreateDatabaseStructure()
    {
        Logger.ILog("Creating Database Structure");
        
        using var db = new FileFlowsDb(ConnectionString, new MySqlDatabaseType(), MySqlConnector.MySqlConnectorFactory.Instance);
        string sqlTables = ScriptHelper.GetSqlScript("MySql", "Tables.sql", clean: true);
        db.Execute(sqlTables);
        
        return true;
    }
}