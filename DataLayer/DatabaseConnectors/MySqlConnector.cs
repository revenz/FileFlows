using System.Globalization;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.Converters;
using FileFlows.Plugin;
using NPoco;
using NPoco.DatabaseTypes;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for MySQL/MariaDB
/// </summary>
public class MySqlConnector : IDatabaseConnector
{
    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.MySql;

    /// <summary>
    /// The connection string to the database
    /// </summary>
    private string ConnectionString { get; init; }
    /// <summary>
    /// Pool of database connections
    /// </summary>
    private DatabaseConnectionPool connectionPool;

    /// <summary>
    /// Logger used for logging
    /// </summary>
    private ILogger Logger;

    /// <inheritdoc />
    public string FormatDateQuoted(DateTime date)
        => "'" + date.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
    
    /// <inheritdoc />
    public string TimestampDiffSeconds(string start, string end, string asColumn)
        => $" timestampdiff(second, {start}, {end}) AS {asColumn} ";

    /// <summary>
    /// Initialises a MySQL Connector
    /// </summary>
    /// <param name="logger">the logger to use by this connector</param>
    /// <param name="connectionString">the connection string for this connector</param>
    public MySqlConnector(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
        connectionPool = new(CreateConnection, 20, connectionLifetime: new TimeSpan(0, 10, 0));
    }
    
    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>the new connection</returns>
    private DatabaseConnection CreateConnection()
    {
        var db = new FileFlowsDb(ConnectionString, new MySqlDatabaseType(), MySqlConnectorFactory.Instance);
        db.Mappers = new()
        {
            GuidNullableConverter.UseInstance(),
            NoNullsConverter.UseInstance(),
            CustomDbMapper.UseInstance()
        };
        return new DatabaseConnection(db, false);
    }

    /// <inheritdoc />
    public async Task<DatabaseConnection> GetDb(bool write)
    {
        return await connectionPool.AcquireConnectionAsync();
    }

    /// <inheritdoc />
    public string WrapFieldName(string name) => name;
    
    /// <summary>
    /// Gets the database name from the connection string
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database name</returns>
    private static string GetDatabaseName(string connectionString)
        => Regex.Match(connectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;

    /// <inheritdoc />
    public async Task<bool> ColumnExists(string table, string column)
    {
        string dbName = GetDatabaseName(this.ConnectionString);
        using var db = await GetDb(false);
        var result = db.Db.ExecuteScalar<int>($@"SELECT count(*) 
        FROM information_schema.COLUMNS 
        WHERE 
            TABLE_SCHEMA = @0 
        AND TABLE_NAME = @1 
        AND COLUMN_NAME = @2", dbName, table, column);
        return result > 0;
    }

    /// <inheritdoc />
    public async Task<bool> TableExists(string table, DatabaseConnection? db = null)
    {
        const string sql = $@"SELECT count(*) 
        FROM information_schema.COLUMNS 
        WHERE 
            TABLE_SCHEMA = @0 
        AND TABLE_NAME = @1";
        string dbName = GetDatabaseName(this.ConnectionString);
        if (db == null)
        {
            using var db2 = await GetDb(false);
            var result = db2.Db.ExecuteScalar<int>(sql, dbName, table);
            return result > 0;
        }
        else
        {
            var result = db.Db.ExecuteScalar<int>(sql, dbName, table);
            return result > 0;
            
        }
    }

    /// <inheritdoc />
    public async Task CreateColumn(string table, string column, string type, string defaultValue)
    {
        if (type.ToLowerInvariant().IndexOf("varchar", StringComparison.InvariantCulture) > 0)
            type += " COLLATE utf8_unicode_ci";
        string sql = $@"ALTER TABLE {table} ADD COLUMN {column} {type}" + (string.IsNullOrWhiteSpace(defaultValue) ? "" : $" DEFAULT {defaultValue}");
        using var db = await GetDb(false);
        await db.Db.ExecuteAsync(sql);
        if (type?.ToLowerInvariant() == "text" && defaultValue == string.Empty)
            await db.Db.ExecuteAsync($"update {table} set {column} = ''");
    }

    /// <inheritdoc />
    public int GetOpenedConnections()
        => connectionPool.OpenedConnections;
}