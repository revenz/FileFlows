using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using Microsoft.Data.SqlClient;
using NPoco;
using NPoco.DatabaseTypes;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// Creator for a SQL Server database
/// </summary>
public class SqlServerDatabaseCreator : IDatabaseCreator
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
    /// Initializes an instance of the SQL Server database creator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    public SqlServerDatabaseCreator(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
    }
    
    /// <inheritdoc />
    public Result<DbCreateResult> CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = GetDatabaseName(ConnectionString);
        
        using var db = new Database(connString, new SqlServerDatabaseType(), SqlClientFactory.Instance);
        bool exists = DatabaseExists(ConnectionString);
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.ILog("Dropping existing database");
            db.Execute($"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}];");
        }

        Logger.ILog("Creating Database");
        var sql = "create database " + dbName + " COLLATE Latin1_General_100_CI_AS_SC_UTF8";
        Logger.ILog("SQL: " + sql.Replace("\n", " "));
        try
        {
            db.Execute(sql);
            return DbCreateResult.Created;
        }
        catch (Exception ex)
        {
            Logger.ELog("Error creating SQL Server database: " + ex.Message);
            return Result<DbCreateResult>.Fail(ex.Message);
        }
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
        
        using var db = new FileFlowsDb(ConnectionString, new SqlServerDatabaseType(), SqlClientFactory.Instance);
        string sqlTables = ScriptHelper.GetSqlScript("SqlServer", "Tables.sql", clean: true);
        //Logger.ILog("SQL Tables:\n" + sqlTables);
        db.Execute(sqlTables);
        
        return true;
    }

    /// <summary>
    /// Checks if the SQL Server database exists
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>true if exists, otherwise false</returns>
    public static Result<bool> DatabaseExists(string connectionString)
    {
        try
        {
            string connString = Regex.Replace(connectionString, "(^|;)Database=[^;]+", "");
            if (connString.StartsWith(";"))
                connString = connString[1..];
            string dbName = GetDatabaseName(connectionString);

            using var db = new Database(connString, new SqlServer2008DatabaseType(), SqlClientFactory.Instance);
            return db.ExecuteScalar<int>(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.databases WHERE name = @0) THEN 1 ELSE 0 END",
                    dbName) == 1;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}