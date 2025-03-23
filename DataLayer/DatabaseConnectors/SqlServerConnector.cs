using System.Globalization;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.Converters;
using FileFlows.Plugin;
using Microsoft.Data.SqlClient;
using NPoco;
using NPoco.DatabaseTypes;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using MySqlConnectorFactory = MySqlConnector.MySqlConnectorFactory;

namespace FileFlows.DataLayer.DatabaseConnectors;

/// <summary>
/// Connector for SQL Server
/// </summary>
public class SqlServerConnector : IDatabaseConnector
{
    /// <inheritdoc />
    public DatabaseType Type => DatabaseType.SqlServer;
    
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
        => "'" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) + "'";
    
    /// <inheritdoc />
    public string TimestampDiffSeconds(string start, string end, string asColumn)
        => $" DATEDIFF(second, {start}, {end}) AS {asColumn} ";
    
    /// <summary>
    /// Initialises a SQL Server Connector
    /// </summary>
    /// <param name="logger">the logger to use by this connector</param>
    /// <param name="connectionString">the connection string for this connector</param>
    public SqlServerConnector(ILogger logger, string connectionString)
    {
        Logger = logger;
        ConnectionString = connectionString;
        connectionPool = new(CreateConnection, 20, connectionLifetime: new TimeSpan(0, 10, 0));
    }

    /// <inheritdoc />
    public int GetOpenedConnections()
        => connectionPool.OpenedConnections;
    
    /// <summary>
    /// Create a new database connection
    /// </summary>
    /// <returns>the new connection</returns>
    private DatabaseConnection CreateConnection()
    {
        var db = new FileFlowsDb(ConnectionString, new SqlServerDatabaseType(), SqlClientFactory.Instance);
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
        //var db = new NPoco.Database(ConnectionString, null, SqlClientFactory.Instance);
        //return Task.FromResult(new DatabaseConnection(db, true));
        
        return await connectionPool.AcquireConnectionAsync();
    }


    /// <inheritdoc />
    public string WrapFieldName(string name) => name;

    /// <inheritdoc />
    public async Task<bool> ColumnExists(string table, string column)
    {
        using var db = await GetDb(false);
        var result = db.Db.ExecuteScalar<int>(@"
        SELECT COUNT(*)
        FROM information_schema.COLUMNS
        WHERE
            TABLE_NAME = @0
        AND COLUMN_NAME = @1", table, column);
        return result > 0;
    }
    
    /// <inheritdoc />
    public async Task<bool> TableExists(string table, DatabaseConnection? db = null)
    {
        const string sql = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_NAME = @0";
        if (db == null)
        {
            using var db2 = await GetDb(false);
            var result = db2.Db.ExecuteScalar<int>(sql, table);
            return result > 0;
        }
        else
        {
            var result = db.Db.ExecuteScalar<int>(sql, table);
            return result > 0;
        }
    }
    
    /// <inheritdoc />
    public async Task CreateColumn(string table, string column, string type, string defaultValue)
    {
        if (type.ToLowerInvariant() == "text")
            type = "NVARCHAR(MAX)";
        
        string sql = $@"ALTER TABLE {table} ADD {column} {type}" + (string.IsNullOrWhiteSpace(defaultValue) ? "" : $" DEFAULT {defaultValue}");
        using var db = await GetDb(false);
        await db.Db.ExecuteAsync(sql);
        if (type?.ToLowerInvariant() == "NVARCHAR(MAX)" && defaultValue == string.Empty)
            await db.Db.ExecuteAsync($"update {table} set {column} = ''");
    }
}