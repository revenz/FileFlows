// using System.Data;
// using System.Data.Common;
// using System.Globalization;
// using System.Text.RegularExpressions;
// using FileFlows.Plugin;
// using FileFlows.ServerShared.Helpers;
// using NPoco.DatabaseTypes;
// using DatabaseType = FileFlows.Shared.Models.DatabaseType;
//
// namespace FileFlows.DataLayer.DatabaseConnectors;
//
// /// <summary>
// /// Connector for SQLite Pooled Connectiion
// /// </summary>
// public class SQLiteConnectorPooledConnection : IDatabaseConnector
// {
//     private DatabaseConnection dbConnectionWrite;
//     private FairSemaphore writeSemaphore = new(1);
//     
//     /// <summary>
//     /// Logger used for logging
//     /// </summary>
//     private ILogger Logger;
//
//     /// <inheritdoc />
//     public string FormatDateQuoted(DateTime date)
//         => "datetime('" + date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) + "', 'utc')"; 
//     // ^^ this worked for all but one, that one user had many other issues, reverting to this 
//     
//     //     // if Z is added to the end here, it causes the timezone bias to be applied twice
//     //     string dateString = date.ToString("yyyy-MM-ddTHH:mm:ss.fff");
//     //     return "'" + dateString + "'";
//     // }
//
//     /// <inheritdoc />
//     public string TimestampDiffSeconds(string start, string end, string asColumn)
//        => $"(strftime('%s', {end}) - strftime('%s', {start})) AS {asColumn}";
//     
//     /// <inheritdoc />
//     public int GetOpenedConnections()
//         => writeSemaphore.CurrentInUse;
//
//     /// <inheritdoc />
//     public bool Cached { get; }
//
//     /// <summary>
//     /// Initialise a new SQLite Connector
//     /// </summary>
//     /// <param name="logger">the logger to use</param>
//     /// <param name="connectionString">the connection string</param>
//     /// <param name="cached">if the connectino is cached or not</param>
//     public SQLiteConnectorPooledConnection(ILogger logger, string connectionString, bool cached = false)
//     {
//         Cached = cached;
//         Logger = logger;
//         logger.ILog("Using SQLite Connector");
//
//         if (string.IsNullOrWhiteSpace(DirectoryHelper.DatabaseDirectory) == false)
//         {
//             // database directory can be null during unit testing
//             // if connection string is using relative file, update with full path
//             connectionString = connectionString.Replace($"Data Source=FileFlows.sqlite",
//                 $"Data Source={Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite")}");
//         }
//
//         dbConnectionWrite = CreateConnection(connectionString);
//         dbConnectionWrite.OnDispose += Dispose;
//         // readPool = new(() => CreateConnection(connectionString), 5);
//     }
//
//     private void Dispose(object? sender, EventArgs e)
//     {
//         writeSemaphore.Release();
//     }
//
//     private DatabaseConnection CreateConnection(string connectionString)
//     {
//         var db = new LoggingDatabase(Logger, connectionString, new SQLiteDatabaseType(),
//             PlatformHelper.IsArm ? Microsoft.Data.Sqlite.SqliteFactory.Instance : System.Data.SQLite.SQLiteFactory.Instance);
//
//         db.Mappers = new()
//         {
//             Converters.GuidConverter.UseInstance(),
//             Converters.CustomDbMapper.UseInstance(),
//             // Converters.UtcDateConverter.UseInstance()
//         };
//         
//
//         var connection = new DatabaseConnection(db, false);
//         
//         // Set the synchronous mode to FULL
//         connection.Db.Execute("PRAGMA synchronous=FULL;");
//         
//         return connection;
//     }
//
//
//     /// <inheritdoc />
//     public DatabaseType Type => DatabaseType.Sqlite;
//
//     /// <inheritdoc />
//     public async Task<DatabaseConnection> GetDb(bool write)
//     {
//         await writeSemaphore.WaitAsync();
//         return dbConnectionWrite;
//     }
//
//     /// <inheritdoc />
//     public string WrapFieldName(string name) => name;
//     
//     /// <inheritdoc />
//     public async Task<bool> ColumnExists(string table, string column)
//     {
//         using var db = await GetDb(false);
//         bool exists = db.Db.ExecuteScalar<int>("SELECT COUNT(*) AS CNTREC FROM pragma_table_info(@0) WHERE name=@1", table, column) > 0;
//         return exists;
//     }
//
//     /// <inheritdoc />
//     public async Task<bool> TableExists(string table, DatabaseConnection? db = null)
//     {
//         const string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@0";
//         if (db == null)
//         {
//             using var db2 = await GetDb(false);
//             bool tableExists = db2.Db.ExecuteScalar<int>(sql, table) > 0;
//             return tableExists;
//         }
//         else
//         {
//             bool tableExists = db.Db.ExecuteScalar<int>(sql, table) > 0;
//             return tableExists;
//         }
//     }
//
//     /// <inheritdoc />
//     public async Task CreateColumn(string table, string column, string type, string defaultValue)
//     {
//         string sql = $@"ALTER TABLE {table} ADD COLUMN {column} {type}" + (string.IsNullOrWhiteSpace(defaultValue) ? "" : $" DEFAULT {defaultValue}");
//         using var db = await GetDb(false);
//         await db.Db.ExecuteAsync(sql);
//
//         if (type?.ToLowerInvariant() == "text" && defaultValue == string.Empty)
//             await db.Db.ExecuteAsync($"update {table} set {column} = ''");
//     }
//     
//     /// <summary>
//     /// Looks to see if the file in the specified connection string exists, and if so, moves it
//     /// </summary>
//     /// <param name="connectionString">The connection string</param>
//     internal static void MoveFileFromConnectionString(string connectionString)
//     {
//         string filename = GetFilenameFromConnectionString(connectionString);
//         if (string.IsNullOrWhiteSpace(filename))
//             return;
//         
//         if (File.Exists(filename) == false)
//             return;
//         
//         string dest = filename + ".backup";
//         File.Move(filename, dest, true);
//     }
//
//     /// <summary>
//     /// Gets the filename from a connection string
//     /// </summary>
//     /// <param name="connectionString">the connection string</param>
//     /// <returns>the filename</returns>
//     private static string GetFilenameFromConnectionString(string connectionString)
//     {
//         if (string.IsNullOrWhiteSpace(connectionString))
//             return string.Empty;
//         return Regex.Match(connectionString, @"(?<=(Data Source=))[^;]+")?.Value ?? string.Empty;
//     }
// }
//
// public class LoggingDatabase(ILogger Logger, string connectionString, NPoco.DatabaseType dbType, DbProviderFactory provider) 
//     : NPoco.Database(connectionString, dbType, provider)
// {
//
//     protected override void OnExecutingCommand(DbCommand cmd)
//     {
//         // Log the query and parameters
//         LogQuery(cmd);
//
//         // Call base method to ensure the normal operation
//         base.OnExecutingCommand(cmd);
//     }
//
//     private void LogQuery(IDbCommand cmd)
//     {
//         var sqlQuery = cmd.CommandText;
//
//         // Log parameters if any
//         if (cmd.Parameters.Count > 0)
//         {
//             sqlQuery += " | Parameters: ";
//             foreach (IDbDataParameter param in cmd.Parameters)
//             {
//                 sqlQuery += $"{param.ParameterName}={param.Value}, ";
//             }
//             sqlQuery = sqlQuery.TrimEnd(',', ' ');
//         }
//         
//         Logger.ILog($"Executing Query: {sqlQuery}");
//     }
// }
