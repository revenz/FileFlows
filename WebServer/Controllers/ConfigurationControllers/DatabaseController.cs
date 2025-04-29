using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models.Configuration;

namespace FileFlows.WebServer.Controllers.ConfigurationControllers;

/// <summary>
/// Settings Controller
/// </summary>
[Route("/api/configuration/database")]
[FileFlowsAuthorize(UserRole.Admin)]
public class DatabaseController
{

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public async Task<DatabaseModel> Get()
    {
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;

        DatabaseModel model = new();
        model.DbType = settings.DatabaseMigrateType ?? settings.DatabaseType;
        if (model.DbType != DatabaseType.Sqlite)
            PopulateDbSettings(model,
                settings.DatabaseMigrateConnection?.EmptyAsNull() ?? settings.DatabaseConnection);
        model.RecreateDatabase = settings.RecreateDatabase;
        if(model.DbType != DatabaseType.Sqlite)
            model.DontBackupOnUpgrade = settings.DontBackupOnUpgrade;
        return model;
    }
    
    
    /// <summary>
    /// Saves the database model
    /// </summary>
    /// <param name="model">the database model</param>
    [HttpPut]
    public void Save([FromBody] DatabaseModel model)
    {
        if (model == null)
            return;

        var service = ServiceLoader.Load<AppSettingsService>();
        var Settings = service.Settings;
        
        var newConnectionString = GetConnectionString(model, model.DbType);
        if (IsConnectionSame(Settings.DatabaseConnection ?? string.Empty, newConnectionString) == false)
        {
             // need to migrate the database
             Settings.DatabaseMigrateConnection = newConnectionString;
             Settings.DatabaseMigrateType = model.DbType;
        }
        else if (Settings.DatabaseType != model.DbType)
        {
            // if switching from SQLite new connection 
            Settings.DatabaseType = model.DbType;
        }

        Settings.RecreateDatabase = model.RecreateDatabase;
        Settings.DontBackupOnUpgrade = model.DbType != DatabaseType.Sqlite && 
                                       model.DontBackupOnUpgrade;
        // save AppSettings with updated license and db migration if set
        service.Save();
    }
    
    /// <summary>
    /// Retrieves the connection string based on the provided settings and database type.
    /// </summary>
    /// <param name="settings">The settings containing the connection details.</param>
    /// <param name="dbType">The type of the database.</param>
    /// <returns>The connection string for the specified database type.</returns>
    private string GetConnectionString(DatabaseModel settings, DatabaseType dbType)
    {
        return new DbConnectionInfo()
        {
            Type = dbType,
            Server = settings.DbServer,
            Name = settings.DbName,
            Port = settings.DbPort,
            User = settings.DbUser,
            Password = settings.DbPassword
        }.ToString();
    }
    
    /// <summary>
    /// Parses the connection string and populates the provided DbSettings object with server, user, password, database name, and port information.
    /// </summary>
    /// <param name="settings">The setting object to populate.</param>
    /// <param name="connectionString">The connection string to parse.</param>
    void PopulateDbSettings(DatabaseModel settings, string? connectionString)
    {
        var parts = connectionString.Split(';');

        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length != 2)
                continue;

            var key = keyValue[0].Trim().ToLowerInvariant();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "server":
                case "host":
                    settings.DbServer = value;
                    break;
                case "user":
                case "uid":
                case "username":
                case "user id":
                    settings.DbUser = value;
                    break;
                case "password":
                case "pwd":
                    settings.DbPassword = value;
                    break;
                case "database":
                case "initial catalog": // SQL Server specific
                    settings.DbName = value;
                    break;
                case "port":
                    if(int.TryParse(value, out int port))
                        settings.DbPort = port;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Determines if the new connection string is the same as the original connection string.
    /// </summary>
    /// <param name="original">The original connection string.</param>
    /// <param name="newConnection">The new connection string to compare against the original.</param>
    /// <returns>
    /// <c>true</c> if both connection strings are for SQLite or if they are exactly the same;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool IsConnectionSame(string original, string newConnection)
    {
        if (IsSqliteConnection(original) && IsSqliteConnection(newConnection))
            return true;
        return original == newConnection;
    }

    /// <summary>
    /// Determines if the provided connection string is an SQLite connection.
    /// </summary>
    /// <param name="connString">The connection string to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the connection string is for an SQLite database or if it is null or whitespace;
    /// otherwise, <c>false</c>.
    /// </returns>
    private bool IsSqliteConnection(string connString)
    {
        if (string.IsNullOrWhiteSpace(connString))
            return true;
        return connString.IndexOf("FileFlows.sqlite", StringComparison.Ordinal) > 0;
    }
}