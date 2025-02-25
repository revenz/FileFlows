using System.Configuration;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Loads a database connector
/// </summary>
internal static class DatabaseConnectorLoader
{
    /// <summary>
    /// Loads a database connector 
    /// </summary>
    /// <param name="logger">The logger to used for logging</param>
    /// <param name="type">The type of connector to load</param>
    /// <param name="connectionString">The connection string of the database</param>
    /// <returns>The initialized connector</returns>
    internal static IDatabaseConnector LoadConnector(ILogger logger, DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.MySql:
                return new MySqlConnector(logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerConnector(logger, connectionString);
            case DatabaseType.Postgres:
                return new PostgresConnector(logger, connectionString);
            case DatabaseType.SqliteNonCached:
                 return new SQLiteConnectorNewConnection(logger, connectionString, cached: false);
            default:
                return new SQLiteConnectorNewConnection(logger, connectionString);
        }
    }
}

/// <summary>
/// Interface for different database connection types
/// </summary>
public interface IDatabaseConnector
{
    /// <summary>
    /// Gets the database type
    /// </summary>
    DatabaseType Type { get; }

    /// <summary>
    /// Gets if the connection is cached
    /// </summary>
    bool Cached => false; 
    
    /// <summary>
    /// Gets the database connection
    /// </summary>
    /// <param name="write">If the query will be writing data</param>
    /// <returns>the database connection</returns>
    Task<DatabaseConnection> GetDb(bool write = false);

    /// <summary>
    /// Wraps a field name in the character supported by this database
    /// </summary>
    /// <param name="name">the name of the field to wrap</param>
    /// <returns>the wrapped field name</returns>
    string WrapFieldName(string name);

    /// <summary>
    /// Converts a datetime to a string for the database in quotes
    /// </summary>
    /// <param name="date">the date to convert</param>
    /// <returns>the converted data as a string</returns>
    string FormatDateQuoted(DateTime date);

    /// <summary>
    /// Creates a time difference sql select
    /// </summary>
    /// <param name="start">the start column</param>
    /// <param name="end">the end column</param>
    /// <param name="asColumn">the name of the result</param>
    /// <returns>the sql select statement</returns>
    string TimestampDiffSeconds(string start, string end, string asColumn);

    /// <summary>
    /// Check to see if a table exists
    /// </summary>
    /// <param name="table">the table to check</param>
    /// <param name="db">optional connection</param>
    /// <returns>true if the table exists, otherwise false</returns>
    Task<bool> TableExists(string table, DatabaseConnection? db = null);

    /// <summary>
    /// Check to see if a column exists
    /// </summary>
    /// <param name="table">the table to check</param>
    /// <param name="column">the name of the column</param>
    /// <returns>true if the column exists, otherwise false</returns>
    Task<bool> ColumnExists(string table, string column);

    /// <summary>
    /// Creates a column with the given name, data type, and default value.
    /// </summary>
    /// <param name="table">The name of the table.</param>
    /// <param name="column">The name of the column to create.</param>
    /// <param name="type">The data type of the column.</param>
    /// <param name="defaultValue">The default value for the column.</param>
    Task CreateColumn(string table, string column, string type, string defaultValue);

    /// <summary>
    /// Gets the number of opened connections
    /// </summary>
    /// <returns>the number of opened connections</returns>
    int GetOpenedConnections();
}