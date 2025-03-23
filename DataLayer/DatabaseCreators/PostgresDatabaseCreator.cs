using System.Text.RegularExpressions;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using Microsoft.Data.SqlClient;
using NPoco;
using NPoco.DatabaseTypes;

namespace FileFlows.DataLayer.DatabaseCreators;

/// <summary>
/// Creator for a Postgres database
/// </summary>
public class PostgresDatabaseCreator : IDatabaseCreator
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
    /// Initializes an instance of the Postgres database creator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    public PostgresDatabaseCreator(ILogger logger, string connectionString)
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
        
        using var db = new Database(connString, new PostgreSQLDatabaseType(), Npgsql.NpgsqlFactory.Instance);
        bool exists = DatabaseExists(ConnectionString);
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.ILog("Dropping existing database");
            db.Execute($"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{dbName}' AND pid <> pg_backend_pid();");
            db.Execute($"DROP DATABASE \"{dbName}\";");
        }

        Logger.ILog("Creating Database");
        var sql = $"CREATE DATABASE \"{dbName}\"";
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
        
        using var db = new FileFlowsDb(ConnectionString, new PostgreSQLDatabaseType(), Npgsql.NpgsqlFactory.Instance);
        string sqlTables = ScriptHelper.GetSqlScript("Postgres", "Tables.sql", clean: true);
        db.Execute(sqlTables);
        
        return true;
    }

    /// <summary>
    /// Checks if the MySql database exists
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

            using var db = new Database(connString, new PostgreSQLDatabaseType(), Npgsql.NpgsqlFactory.Instance);
            return db.ExecuteScalar<int>(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = @0) THEN 1 ELSE 0 END",
                    dbName) == 1;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}