using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using FileFlows.Plugin;
using FileFlows.Shared.Models;
using Org.BouncyCastle.Ocsp;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations by serving as a proxy for database commands.
/// </summary>
internal  class DatabaseAccessManager
{
    /// <summary>
    /// The connector being used
    /// </summary>
    public IDatabaseConnector DbConnector { get; private set; }
    
    /// <summary>
    /// Gets or sets the singleton instance of the Database Access Manager
    /// </summary>
    public static DatabaseAccessManager Instance { get; set; } = null!;

    /// <summary>
    /// Gets the type of database this is
    /// </summary>
    public DatabaseType Type { get; init; }

    /// <summary>
    /// The logger used
    /// </summary>
    private readonly ILogger Logger;

    /// <summary>
    /// Resets the database, use this if migrating data or unit testing
    /// </summary>
    public static void Reset()
    {
        FileFlowsMapper<CustomDbMapper>.DisableAll();
    }
    
    /// <summary>
    /// Initializes a new instance of the DatabaseAccessManager class.
    /// </summary>
    /// <param name="logger">The logger used for logging</param>
    /// <param name="type">The type of database (e.g., SQLite, SQL Server).</param>
    /// <param name="connectionString">The connection string used to connect to the database.</param>
    public DatabaseAccessManager(ILogger logger, DatabaseType type, string connectionString)
    {
        Logger = logger;
        FileFlowsDb.Logger = logger;
        Type = type;
        DbConnector = DatabaseConnectorLoader.LoadConnector(logger, type, connectionString);
        // if (type == DatabaseType.Sqlite)
        //     return;
        this.ObjectManager = new (logger, type, DbConnector);
        this.StatisticManager = new (logger, type, DbConnector);
        this.RevisionManager = new (logger, type, DbConnector);
        //this.LogMessageManager = new (logger, type, DbConnector);
        this.LibraryFileManager = new (logger, type, DbConnector);
        this.AuditManager = new (logger, type, DbConnector);
        this.VersionManager = new (logger, type, DbConnector);
        this.FileFlowsObjectManager = new(this.ObjectManager);
    }

    /// <summary>
    /// Creates a database access manager from its connection string
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database access manager instance</returns>
    public static DatabaseAccessManager FromConnectionString(ILogger logger, string connectionString)
    {
        if (connectionString.ToLowerInvariant().IndexOf("host=", StringComparison.Ordinal) > 0)
            return new DatabaseAccessManager(logger, DatabaseType.Postgres, connectionString);
        if (connectionString.ToLowerInvariant().IndexOf("uid=", StringComparison.Ordinal) > 0)
            return new DatabaseAccessManager(logger, DatabaseType.MySql, connectionString);
        if (connectionString.ToLowerInvariant().IndexOf("server=", StringComparison.Ordinal) > 0)
            return new DatabaseAccessManager(logger, DatabaseType.SqlServer, connectionString);
        return new DatabaseAccessManager(logger, DatabaseType.Sqlite, connectionString);
    }

    /// <summary>
    /// Creates a database access manager from its type
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="type">the type of database</param>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database access manager instance</returns>
    internal static DatabaseAccessManager FromType(ILogger logger, DatabaseType type, string connectionString)
        => new DatabaseAccessManager(logger, type, connectionString);

    /// <summary>
    /// Initializes the database access manager instance
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="type">the type of database</param>
    /// <param name="connectionString">the connection string</param>
    /// <returns>true if initialized and can connect</returns>
    internal static Result<bool> Initialize(ILogger logger, DatabaseType type, string connectionString)
    {
        try
        {
            Instance = FromType(logger, type, connectionString);
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed connecting to database: " + ex.Message);
        }
    }

    /// <summary>
    /// Tests it eh database can be reached
    /// </summary>
    /// <param name="type">the database type</param>
    /// <param name="connectionString">the connection string</param>
    /// <returns>true if can be reached, otherwise false</returns>
    internal static Result<bool> CanConnect(DatabaseType type, string connectionString)
    {
        try
        {
            switch (type)
            {
                case DatabaseType.SqlServer:
                    return SqlServerDatabaseCreator.DatabaseExists(connectionString);
                case DatabaseType.MySql:
                    return MySqlDatabaseCreator.DatabaseExists(connectionString);
                case DatabaseType.Postgres:
                    return PostgresDatabaseCreator.DatabaseExists(connectionString);
                default:
                    return SQLiteDatabaseCreator.DatabaseExists(connectionString);
            }
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed connecting to database: " + ex.Message);
        }
    }


    /// <summary>
    /// Gets the DbObject manager to manage the database operations for the DbObject table
    /// </summary>
    public DbObjectManager ObjectManager { get; init; }
    
    /// <summary>
    /// Gets the DbStatistic manager to manage the database operations for the DbStatistic table
    /// </summary>
    public DbStatisticManager StatisticManager { get; init; }
    
    /// <summary>
    /// Gets the DbAudit manager to manage the database audit operations
    /// </summary>
    public DbAuditManager AuditManager { get; init; }
    
    /// <summary>
    /// Gets the DbRevision manager to manage the database operations for the RevisionedObject table
    /// </summary>
    public DbRevisionManager RevisionManager { get; init; }

    /// <summary>
    /// Gets the FileFlowsObject manager to manage the database operations for the FileFlowsObjects that are saved in the DbObject table
    /// </summary>
    public FileFlowsObjectManager FileFlowsObjectManager { get; init; }

    /// <summary>
    /// Gets the Version manager
    /// </summary>
    public VersionManager VersionManager { get; init; }

    /// <summary>
    /// Gets the Library File manager to manage the database operations for the Library Files
    /// </summary>
    public DbLibraryFileManager LibraryFileManager { get; init; }

    /// <summary>
    /// Gets the open number of database connections
    /// </summary>
    /// <returns>the number of connections</returns>
    public int GetOpenConnections()
        => DbConnector.GetOpenedConnections();

    /// <summary>
    /// Gets the database connection
    /// </summary>
    /// <returns>the database connection</returns>
    internal Task<DatabaseConnection> GetDb()
        => DbConnector.GetDb();

    /// <summary>
    /// Wraps a field name in the character supported by this database
    /// </summary>
    /// <param name="field">the name of the field to wrap</param>
    /// <returns>the wrapped field name</returns>
    public string WrapFieldName(string field)
        => DbConnector.WrapFieldName(field);


    /// <summary>
    /// Converts a datetime to a string for the database in quotes
    /// </summary>
    /// <param name="date">the date to convert</param>
    /// <returns>the converted data as a string</returns>
    public string FormatDateQuoted(DateTime date)
        => DbConnector.FormatDateQuoted(date);

    /// <summary>
    /// Gets the database type
    /// </summary>
    /// <returns>the database type</returns>
    public DatabaseType GetDatabaseType()
        => DbConnector.Type;
}