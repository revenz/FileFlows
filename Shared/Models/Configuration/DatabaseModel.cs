namespace FileFlows.Shared.Models.Configuration;

/// <summary>
/// Database model
/// </summary>
public class DatabaseModel
{
    /// <summary>
    /// Gets or sets the type of database to use
    /// </summary>
    public DatabaseType DbType { get; set; }
    
    /// <summary>
    /// Gets or sets the db server to use
    /// </summary>
    public string DbServer { get; set; }
    
    /// <summary>
    /// Gets or sets the db port to use
    /// </summary>
    public int DbPort { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the database
    /// </summary>
    public string DbName { get; set; }
    
    /// <summary>
    /// Gets or sets the user used to connect to the database
    /// </summary>
    public string DbUser { get; set; }
    
    /// <summary>
    /// Gets or sets the password used to connect to the database
    /// </summary>
    public string DbPassword { get; set; }

    /// <summary>
    /// Gets or sets if the database should be recreated if it already exists
    /// </summary>
    public bool RecreateDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets if database backups should not be taken during upgrade
    /// </summary>
    public bool DontBackupOnUpgrade { get; set; }
}