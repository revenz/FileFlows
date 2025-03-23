using System.Data;
using System.Data.Common;
using NPoco;

namespace FileFlows.DataLayer;

public class FileFlowsDb : NPoco.Database
{
    /// <summary>
    /// Gets or sets the logger
    /// </summary>
    internal static ILogger? Logger { get; set; }
    
    public FileFlowsDb(DbConnection connection) : base(connection)
    {
    }

    public FileFlowsDb(DbConnection connection, DatabaseType? dbType) : base(connection, dbType)
    {
    }

    public FileFlowsDb(DbConnection connection, DatabaseType? dbType, IsolationLevel? isolationLevel) : base(connection, dbType, isolationLevel)
    {
    }

    public FileFlowsDb(DbConnection connection, DatabaseType? dbType, IsolationLevel? isolationLevel, bool enableAutoSelect) : base(connection, dbType, isolationLevel, enableAutoSelect)
    {
    }

    public FileFlowsDb(string connectionString, DatabaseType databaseType, DbProviderFactory provider) : base(connectionString, databaseType, provider)
    {
    }

    public FileFlowsDb(string connectionString, DatabaseType databaseType, DbProviderFactory provider, IsolationLevel? isolationLevel = null, bool enableAutoSelect = true) : base(connectionString, databaseType, provider, isolationLevel, enableAutoSelect)
    {
    }

    protected override void OnExecutingCommand(DbCommand cmd)
    {
        Logger?.ILog(FormatCommand(cmd));
    }
}