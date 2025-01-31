using FileFlows.Managers.InitializationManagers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Cli.Commands;

/// <summary>
/// A command that creates a new database and copies all the data from one database to another
/// </summary>
public class CopyDatabase : Command
{
    /// <inheritdoc />
    public override string Switch => "copy-db";

    /// <inheritdoc />
    public override string Description =>
        "Creates a new SQLite database and copies all the data from one database into a new database.";
    
    /// <summary>
    /// Gets or set the source database
    /// </summary>
    [CommandLineArg("src", "The source database file")]
    public string Source { get; set; }
    
    /// <summary>
    /// Gets or set the destination database
    /// </summary>
    [CommandLineArg("dest", "The destination database file")]
    public string Destination { get; set; }
    
    /// <summary>
    /// Gets or set the destination database should be overrwriten if it exists
    /// </summary>
    [CommandLineArg("f", "If the destination file should be overwritten if it exists", optional: true)]
    public bool Force { get; set; }
    
    
    
    /// <inheritdoc />
    public override bool Run(ILogger  logger)
    {
        if (File.Exists(Source) == false)
        {
            logger.ELog("Source database does not exist");
            return true;
        }

        if (File.Exists(Destination))
        {
            if (Force == false)
            {
                logger.ELog("Destnation database exists, use -f if you want to overwrite this database file");
                return true;
            }

            logger.ILog("Deleting existing database file: " + Destination);
            File.Delete(Destination);
        }

        MigrationManager migrator = new(logger, new()
            {
                Type = DatabaseType.Sqlite,
                ConnectionString = SqliteHelper.GetConnectionString(Source)
            },
            new()
            {
                Type = DatabaseType.Sqlite,
                ConnectionString = SqliteHelper.GetConnectionString(Destination)
            });
        var result = migrator.Migrate();
        if(result.Failed(out string error))
            logger.ELog(error);
        
        return true;
    }
}