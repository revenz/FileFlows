using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.Shared.Models;
using Microsoft.Extensions.Logging;
using NPoco;
using Org.BouncyCastle.Pkix;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;
using ILogger = FileFlows.Common.ILogger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.03.2
/// Changing DateTimes to UTC
/// </summary>
public class Upgrade_24_03_2
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        if ((int)dbType == 0 || (int)dbType >= 10)
            return RunSqlite(logger, connectionString);
        else
            return RunMySql(logger, connectionString);

    }

    /// <summary>
    /// Run the upgrade for a MySql Server
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    private Result<bool> RunMySql(ILogger logger, string connectionString)
    {
        var connector = new DatabaseConnectors.MySqlConnector(logger, connectionString);
        using var db = connector.GetDb(true).Result;
        
        db.Db.BeginTransaction();
        try
        {
            // DbObject
            var dbos = db.Db.Fetch<DbObjectUpgrade>("select * from DbObject");
            StringBuilder sql = new();
            List<Guid> knownLibraries = new(); 
            foreach (var dbo in dbos)
            {
                dbo.DateCreated = DateTimeHelper.LocalToUtc(dbo.DateCreated);
                dbo.DateModified = DateTimeHelper.LocalToUtc(dbo.DateModified);
                if (dbo.Type == "FileFlows.Shared.Models.Library")
                    knownLibraries.Add(dbo.Uid);
                sql.AppendLine("update DbObject set DateCreated = " + connector.FormatDateQuoted(dbo.DateCreated) +
                               ", DateModified = " + connector.FormatDateQuoted(dbo.DateModified) +
                               $" where Uid = '{dbo.Uid}';");
            }

            db.Db.Execute(sql.ToString());


            // DbRevision
            var dbr = db.Db.Fetch<RevisionedObject>("select * from RevisionedObject");
            sql.Clear();
            foreach (var r in dbr)
            {
                r.RevisionDate = DateTimeHelper.LocalToUtc(r.RevisionDate);
                r.RevisionCreated = DateTimeHelper.LocalToUtc(r.RevisionCreated);
                sql.AppendLine("update RevisionedObject set RevisionDate = " +
                               connector.FormatDateQuoted(r.RevisionDate) +
                               ", RevisionCreated = " + connector.FormatDateQuoted(r.RevisionCreated) +
                               $" where Uid = '{r.Uid}';");
            }

            db.Db.Execute(sql.ToString());
            
            // LibraryFiles
            var libFiles = db.Db.Fetch<LibraryFileUpgrade>(
                "select Uid, Name, LibraryName, OriginalSize, FinalSize, DateCreated, DateModified, ProcessingStarted, ProcessingEnded, HoldUntil, CreationTime, LastWriteTime from LibraryFile");
            sql.Clear();

            Dictionary<string, StorageSavedData> storageSavedData = new(); 
            foreach (var lf in libFiles)
            {
                if (storageSavedData.ContainsKey(lf.LibraryName) == false)
                    storageSavedData[lf.LibraryName] = new() { Library = lf.LibraryName };
                storageSavedData[lf.LibraryName].TotalFiles += 1;
                storageSavedData[lf.LibraryName].FinalSize += lf.FinalSize;
                storageSavedData[lf.LibraryName].OriginalSize += lf.OriginalSize;
                    
                var fileInfo = new FileInfo(lf.Name);
                lf.DateCreated = DateTimeHelper.LocalToUtc(lf.DateCreated);
                lf.DateModified = DateTimeHelper.LocalToUtc(lf.DateModified);
                lf.ProcessingStarted = DateTimeHelper.LocalToUtc(lf.ProcessingStarted);
                lf.ProcessingEnded = DateTimeHelper.LocalToUtc(lf.ProcessingEnded);
                lf.HoldUntil = DateTimeHelper.LocalToUtc(lf.HoldUntil);
                if (fileInfo.Exists)
                {
                    lf.CreationTime = fileInfo.CreationTimeUtc;
                    lf.LastWriteTime = fileInfo.LastWriteTimeUtc;
                }
                else
                {
                    lf.CreationTime = DateTimeHelper.LocalToUtc(lf.CreationTime);
                    lf.LastWriteTime = DateTimeHelper.LocalToUtc(lf.LastWriteTime);
                }
                sql.AppendLine("update LibraryFile set DateCreated = " +
                               connector.FormatDateQuoted(lf.DateCreated) +
                               ", DateModified = " + connector.FormatDateQuoted(lf.DateModified) +
                               ", ProcessingStarted = " + connector.FormatDateQuoted(lf.ProcessingStarted) +
                               ", ProcessingEnded = " + connector.FormatDateQuoted(lf.ProcessingEnded) +
                               ", HoldUntil = " + connector.FormatDateQuoted(lf.HoldUntil) +
                               ", CreationTime = " + connector.FormatDateQuoted(lf.CreationTime) +
                               ", LastWriteTime = " + connector.FormatDateQuoted(lf.LastWriteTime) +
                               $" where Uid = '{lf.Uid}';");
                if (sql.Length > 1000)
                {
                    db.Db.Execute(sql.ToString());
                    sql.Clear();
                }
            }

            if(sql.Length > 0)
                db.Db.Execute(sql.ToString());

            db.Db.Execute(
                "update DbObject set Name = REPLACE(Name, 'PluginsSettings_', '') , Type = 'FileFlows.ServerShared.Models.PluginSettingsModel' where Type = 'FileFlows.ServerModels.PluginSettingsModel'");

            var upgradeStats = GetUpgradedStatistics(logger, db);

            UpgradeStatistics(logger, db, true, upgradeStats, knownLibraries, storageSavedData);

            db.Db.CompleteTransaction();

            return true;
        }
        catch (Exception ex)
        {
            db.Db.AbortTransaction();
#if(DEBUG)
            return Result<bool>.Fail(ex.Message + Environment.NewLine + ex.StackTrace);
#else
            return Result<bool>.Fail(ex.Message);
#endif
        }

    }

    /// <summary>
    /// Run the upgrade for a SQLite Server
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    private Result<bool> RunSqlite(ILogger logger, string connectionString)
    {
        try
        {
            string dbFile = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
            File.Move(dbFile, dbFile);
            var connector = new SQLiteConnectorPooledConnection(logger, SqliteHelper.GetConnectionString(dbFile));
            List<DbObject> dbObjects;
            List<RevisionedObject> revisionObjects;
            List<LibraryFileUpgrade> libFiles;
            List<UpgradedStatistic> upgradeStatistics;
            List<Guid> knownLibraries = new();
            List<string> sql = new();
            Dictionary<string, StorageSavedData> storageSavedData = new(); 
            using (var db = connector.GetDb(true).Result)
            {
                dbObjects = db.Db.Fetch<DbObject>("select * from DbObject");
                foreach (var obj in dbObjects)
                {
                    obj.DateCreated = DateTimeHelper.LocalToUtc(obj.DateCreated);
                    obj.DateModified = DateTimeHelper.LocalToUtc(obj.DateModified);
                    if (obj.Type == "FileFlows.Shared.Models.Library")
                        knownLibraries.Add(obj.Uid);
                    if (obj.Type == "FileFlows.ServerModels.PluginSettingsModel")
                        obj.Type = "FileFlows.ServerShared.Models.PluginSettingsModel";
                }

                revisionObjects = db.Db.Fetch<RevisionedObject>("select * from RevisionedObject");
                foreach (var obj in revisionObjects)
                {
                    obj.RevisionDate = DateTimeHelper.LocalToUtc(obj.RevisionDate);
                    obj.RevisionCreated = DateTimeHelper.LocalToUtc(obj.RevisionCreated);
                }

                upgradeStatistics = GetUpgradedStatistics(logger, db);

                // LibraryFiles
                libFiles = db.Db.Fetch<LibraryFileUpgrade>("select * from LibraryFile");
                for (int i=libFiles.Count - 1;i >= 0;i--)
                {
                    var lf = libFiles[i];
                    
                    if (storageSavedData.ContainsKey(lf.LibraryName) == false)
                        storageSavedData[lf.LibraryName] = new() { Library = lf.LibraryName };
                    storageSavedData[lf.LibraryName].TotalFiles += 1;
                    storageSavedData[lf.LibraryName].FinalSize += lf.FinalSize;
                    storageSavedData[lf.LibraryName].OriginalSize += lf.OriginalSize;
                    
                    var fileInfo = new FileInfo(lf.Name);
                    lf.DateCreated = DateTimeHelper.LocalToUtc(lf.DateCreated);
                    lf.DateModified = DateTimeHelper.LocalToUtc(lf.DateModified);
                    lf.ProcessingStarted = DateTimeHelper.LocalToUtc(lf.ProcessingStarted);
                    lf.ProcessingEnded = DateTimeHelper.LocalToUtc(lf.ProcessingEnded);
                    lf.HoldUntil = DateTimeHelper.LocalToUtc(lf.HoldUntil);
                    if (fileInfo.Exists)
                    {
                        lf.CreationTime = fileInfo.CreationTimeUtc;
                        lf.LastWriteTime = fileInfo.LastWriteTimeUtc;
                    }
                    else
                    {
                        lf.CreationTime = DateTimeHelper.LocalToUtc(lf.CreationTime);
                        lf.LastWriteTime = DateTimeHelper.LocalToUtc(lf.LastWriteTime);
                    }
                }
            }


            string newFile = Path.Combine(DirectoryHelper.DatabaseDirectory, $"FileFlows-{Guid.NewGuid()}.sqlite");
            string connString = SqliteHelper.GetConnectionString(newFile);
            var creator = new SQLiteDatabaseCreator(logger, connString);
            creator.CreateDatabase(true);
            creator.CreateDatabaseStructure();
            
            connector = new SQLiteConnectorPooledConnection(logger, connString);
            using (var db = connector.GetDb(true).Result)
            {
                foreach (var dbo in dbObjects)
                {
                    db.Db.Insert(dbo);
                }
                foreach (var dbo in revisionObjects)
                {
                    db.Db.Execute("insert into RevisionedObject (Uid, RevisionUid, RevisionName, RevisionType, RevisionDate, RevisionCreated, RevisionData) " +
                                  "values (@0, @1, @2, @3, @4, @5, @6)",
                        dbo.Uid, dbo.RevisionUid, dbo.RevisionName, dbo.RevisionType, dbo.RevisionDate, dbo.RevisionCreated, dbo.RevisionData);
                }

                int BULK_SIZE = 100;
                int count = 1;
                while (libFiles.Any())
                {
                    List<LibraryFileUpgrade> set;
                    if (libFiles.Count > BULK_SIZE)
                    {
                        set = libFiles.Take(BULK_SIZE).ToList();
                        libFiles.RemoveRange(0, BULK_SIZE);
                    }
                    else
                    {
                        set = libFiles.ToList();
                        libFiles.Clear();
                    }
                    logger.ILog($"Insert bucket {count} library files");
                    InsertBulk(db, connector, set);
                    count++;
                }
                
                UpgradeStatistics(logger, db, false, upgradeStatistics, knownLibraries, storageSavedData);
            }
            string old = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows-24-03-02-UpgradeBackup.sqlite");
            File.Move(dbFile, old, true);
            File.Move(newFile, dbFile, true);

            return true;
        }
        catch (Exception ex)
        {
#if(DEBUG)
            return Result<bool>.Fail(ex.Message + Environment.NewLine + ex.StackTrace);
#else
            return Result<bool>.Fail(ex.Message);
#endif
        }
    }

    /// <summary>
    /// Upgrades the statistics table to the new format
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the database connector</param>
    private List<UpgradedStatistic> GetUpgradedStatistics(ILogger logger, DatabaseConnection connector)
    {
        logger.ILog("Convert old statistics data");
        var old = connector.Db.Fetch<DbStatisticOld>("select * from DbStatistic")
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.ToList());

        Dictionary<string, object> newStats = new();
        foreach (string key in old.Keys)
        {
            if (key == "COMIC_PAGES")
            {
                var totals = new Average();
                foreach (var stat in old[key])
                {
                    if (totals.Data.TryAdd((int)stat.NumberValue, 1) == false)
                        totals.Data[(int)stat.NumberValue] += 1;
                }

                newStats.Add(key, totals);
            }
            else
            {

                var totals = new RunningTotals();
                foreach (var stat in old[key])
                {
                    if (totals.Data.TryAdd(stat.StringValue, 1) == false)
                        totals.Data[stat.StringValue] += 1;
                }

                newStats.Add(key, totals);
            }
        }
        logger.ILog("Total Number of new stats: " + newStats.Count);

        var results = new List<UpgradedStatistic>();
        foreach (var key in newStats.Keys)
        {
            results.Add(new()
            {
                Name = key,
                Type = (int)(key == "COMIC_PAGES" ? StatisticType.Average : StatisticType.RunningTotals),
                Data = JsonSerializer.Serialize(newStats[key])
            });
        }

        return results;
    }


    /// <summary>
    /// Upgrades the statistics table to the new format
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the database connector</param>
    /// <param name="mySql">true if using mysql, otherwise false</param>
    /// <param name="upgradedStatistics">the upgrade statistics</param>
    /// <param name="knownLibraries">the known library UIDs</param>
    /// <param name="storageSavedData">the storage saved data</param>
    private void UpgradeStatistics(ILogger logger, DatabaseConnection connector, bool mySql, 
        List<UpgradedStatistic> upgradedStatistics, List<Guid> knownLibraries, Dictionary<string, StorageSavedData> storageSavedData)
    {
        // processing heatmap, after upgrade, so now UTC dates
        logger.ILog("Creating processing heatmap data");
        if (mySql)
        {
            string newTable = $@"DROP TABLE DbStatistic; CREATE TABLE DbStatistic
(
    Name            varchar(255)       {(mySql ? "COLLATE utf8_unicode_ci" : "")}      NOT NULL          PRIMARY KEY,
    Type            int                NOT NULL,
    Data            TEXT               {(mySql ? "COLLATE utf8_unicode_ci" : "")}      NOT NULL
)";
            connector.Db.Execute(newTable);
        }

        foreach (var stat in upgradedStatistics)
        {
            connector.Db.Execute("insert into DbStatistic (Name, Type, Data) values (@0, @1, @2)",
                stat.Name, stat.Type, stat.Data);
        }


        var processedDates =
            connector.Db.Fetch<DateTime>($"select ProcessingStarted from LibraryFile where Status = 1");
        Heatmap heatmap = new();
        foreach (var dt in processedDates)
        {
            int quarter = TimeHelper.GetQuarter(dt);
            if (heatmap.Data.TryAdd(quarter, 1) == false)
                heatmap.Data[quarter] += 1;
        }

        connector.Db.Execute($"insert into DbStatistic (Name, Type, Data) " +
                             $" values (@0, {((int)StatisticType.Heatmap)}, @1)",
            Globals.STAT_PROCESSING_TIMES_HEATMAP, JsonSerializer.Serialize(heatmap));


        // storage saved
        logger.ILog("Creating storage saved data");
        var storageSaved = new StorageSaved() { Data = storageSavedData.Values.ToList() };

        connector.Db.Execute("insert into DbStatistic (Name, Type, Data) values " +
                             $" (@0, {((int)StatisticType.StorageSaved)}, @1)",
            Globals.STAT_STORAGE_SAVED, JsonSerializer.Serialize(storageSaved));

        // total files
        logger.ILog("Creating total files data");
        int totalProcessed = connector.Db.ExecuteScalar<int>("select count(*) from LibraryFile where Status = 1");
        int totalFailed = connector.Db.ExecuteScalar<int>("select count(*) from LibraryFile where Status = 4");

        connector.Db.Execute("insert into DbStatistic (Name, Type, Data)" +
                             $" values (@0, {(int)StatisticType.RunningTotals}, @1)",
            Globals.STAT_TOTAL_FILES, JsonSerializer.Serialize(new RunningTotals()
            {
                Data = new()
                {
                    { nameof(FileStatus.Processed), totalProcessed },
                    { nameof(FileStatus.ProcessingFailed), totalFailed },
                }
            }));

        
        string libUids = string.Join(",", knownLibraries.Select(x => $"'{x}'"));
        connector.Db.Execute($"delete from LibraryFile where LibraryUid not in ({libUids});");
    }
    /// <summary>
    /// Bulk insert many files
    /// </summary>
    /// <param name="files">the files to insert</param>
    private void InsertBulk(DatabaseConnection db, IDatabaseConnector connector, List<LibraryFileUpgrade> files)
    {
        db.Db.BeginTransaction();
        foreach (var file in files)
        {
            List<object> parameters = new();
            int offset = 0; //parameters.Count - 1;
            string sql = $"insert into {nameof(LibraryFile)} ( " +
                         $"{(nameof(LibraryFile.Uid))}, " +
                         $"{(nameof(LibraryFile.Name))}, " +
                         $"{(nameof(LibraryFile.Status))}, " +
                         $"{(nameof(LibraryFile.Flags))}, " +
                         $"{(nameof(LibraryFile.OriginalSize))}, " +
                         $"{(nameof(LibraryFile.FinalSize))}, " +
                         $"{("ProcessingOrder")}, " + // special case, since Order is reserved in sql
                         $"{(nameof(LibraryFile.IsDirectory))}, " +
                         $"{(nameof(LibraryFile.NoLongerExistsAfterProcessing))}, " +

                         $"{(nameof(LibraryFile.DateCreated))}, " +
                         $"{(nameof(LibraryFile.DateModified))}, " +
                         $"{(nameof(LibraryFile.CreationTime))}, " +
                         $"{(nameof(LibraryFile.LastWriteTime))}, " +
                         $"{(nameof(LibraryFile.HoldUntil))}, " +
                         $"{(nameof(LibraryFile.ProcessingStarted))}, " +
                         $"{(nameof(LibraryFile.ProcessingEnded))}, " +

                         $"{(nameof(LibraryFile.RelativePath))}, " +
                         $"{(nameof(LibraryFile.Fingerprint))}, " +
                         $"{(nameof(LibraryFile.FinalFingerprint))}, " +
                         $"{(nameof(LibraryFile.LibraryUid))}, " +
                         $"{(nameof(LibraryFile.LibraryName))}, " +
                         $"{(nameof(LibraryFile.FlowUid))}, " +
                         $"{(nameof(LibraryFile.FlowName))}, " +
                         $"{(nameof(LibraryFile.DuplicateUid))}, " +
                         $"{(nameof(LibraryFile.DuplicateName))}, " +
                         $"{(nameof(LibraryFile.NodeUid))}, " +
                         $"{(nameof(LibraryFile.NodeName))}, " +
                         $"{(nameof(LibraryFile.WorkerUid))}, " +
                         $"{(nameof(LibraryFile.ProcessOnNodeUid))}, " +
                         $"{(nameof(LibraryFile.OutputPath))}, " +
                         $"{(nameof(LibraryFile.OriginalMetadata))}, " +
                         $"{(nameof(LibraryFile.FinalMetadata))}, " +
                         $"{(nameof(LibraryFile.ExecutedNodes))}, " +
                         $"{(nameof(LibraryFile.FailureReason))} " +
                         " )" +
                         $" values (@{offset++},@{offset++}," +
                         ((int)file.Status) + ", " +
                         ((int)file.Flags) + ", " +
                         (file.OriginalSize) + ", " +
                         (file.FinalSize) + ", " +
                         (file.ProcessingOrder) + ", " +
                         file.IsDirectory+ "," +
                         file.NoLongerExistsAfterProcessing + "," +

                         connector.FormatDateQuoted(file.DateCreated) + "," + //$"@{++offset}," + // date created
                         connector.FormatDateQuoted(file.DateModified) + "," + //$"@{++offset}," + // date modified
                         connector.FormatDateQuoted(file.CreationTime) + ", " +
                         connector.FormatDateQuoted(file.LastWriteTime) + ", " +
                         connector.FormatDateQuoted(file.HoldUntil) + ", " +
                         connector.FormatDateQuoted(file.ProcessingStarted) + ", " +
                         connector.FormatDateQuoted(file.ProcessingEnded) + ", " +
                         $"@{offset++},@{offset++},@{offset++},@{offset++},@{offset++},@{offset++}," +
                         $"@{offset++},@{offset++},@{offset++},@{offset++},@{offset++},@{offset++}," +
                         $"@{offset++},@{offset++},@{offset++},@{offset++},@{offset++},@{offset++});\n";

            parameters.Add(file.Uid.ToString());
            parameters.Add(file.Name);

            // we have to always include every value for the migration, otherwise if we use default and the data is migrated that data will change
            parameters.Add(file.RelativePath);
            parameters.Add(file.Fingerprint);
            parameters.Add(file.FinalFingerprint ?? string.Empty);
            parameters.Add(file.LibraryUid.ToString() ?? string.Empty);
            parameters.Add(file.LibraryName);
            parameters.Add(file.FlowUid?.ToString() ?? string.Empty);
            parameters.Add(file.FlowName ?? string.Empty);
            parameters.Add(file.DuplicateUid?.ToString() ?? string.Empty);
            parameters.Add(file.DuplicateName ?? string.Empty);
            parameters.Add(file.NodeUid?.ToString() ?? string.Empty);
            parameters.Add(file.NodeName ?? string.Empty);
            parameters.Add(file.WorkerUid?.ToString() ?? string.Empty);
            parameters.Add(file.ProcessOnNodeUid?.ToString() ?? string.Empty);
            parameters.Add(file.OutputPath ?? string.Empty);
            parameters.Add(file.OriginalMetadata);
            parameters.Add(file.FinalMetadata);
            parameters.Add(file.ExecutedNodes);
            parameters.Add(file.FailureReason ?? string.Empty);
            db.Db.Execute(sql, parameters.ToArray());
        }
        db.Db.CompleteTransaction();
    }

    /// <summary>
    /// An upgraded statistic
    /// </summary>
    private class UpgradedStatistic
    {
        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// Gets or sets the data
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents an object in the database that will be upgraded.
    /// </summary>
    private class DbObjectUpgrade
    {
        /// <summary>
        /// Gets or sets the unique identifier of the object.
        /// </summary>
        public Guid Uid { get; set; }
        
        /// <summary>
        /// Gets or sets the type of object
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the object was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }
    }

    /// <summary>
    /// Represents a file in the library that will be upgraded.
    /// </summary>
    [TableName("LibraryFile")]
    private class LibraryFileUpgrade
    {
        /// <summary>
        /// Gets or sets the unique identifier of the file.
        /// </summary>
        public Guid Uid { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the file
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the file was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }
        
        /// <summary>
        /// Gets or sets the relative path
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the status
        /// </summary>
        public int Status { get; set; }
        
        /// <summary>
        /// Gets or sets the processing order
        /// </summary>
        public int ProcessingOrder { get; set; }
        
        /// <summary>
        /// Gets or sets the Fingerprint
        /// </summary>
        public string Fingerprint { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the final Fingerprint
        /// </summary>
        public string FinalFingerprint { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets if its a directory
        /// </summary>
        public int IsDirectory { get; set; }
        
        /// <summary>
        /// Gets or sets the flags
        /// </summary>
        public int Flags { get; set; }
        
        /// <summary>
        /// Gets or sets the original size
        /// </summary>
        public long OriginalSize { get; set; }
        
        /// <summary>
        /// Gets or sets the final size
        /// </summary>
        public long FinalSize { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was created in the filesystem.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last written to in the filesystem.
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time until which the file is held.
        /// </summary>
        public DateTime HoldUntil { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing of the file started.
        /// </summary>
        public DateTime ProcessingStarted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing of the file ended.
        /// </summary>
        public DateTime ProcessingEnded { get; set; }

        /// <summary>
        /// Gets or sets the library uid
        /// </summary>
        public Guid? LibraryUid { get; set; }

        /// <summary>
        /// Gets or sets the library name
        /// </summary>
        public string LibraryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the flow uid
        /// </summary>
        public string FlowUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the flow name
        /// </summary>
        public string FlowName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duplicate uid
        /// </summary>
        public string DuplicateUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duplicate name
        /// </summary>
        public string DuplicateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the node uid
        /// </summary>
        public string NodeUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the node name
        /// </summary>
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the worker uid
        /// </summary>
        public string WorkerUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the output path
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the failure reason
        /// </summary>
        public string FailureReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets if the file no longer exists after processing
        /// </summary>
        public int NoLongerExistsAfterProcessing { get; set; }

        /// <summary>
        /// Gets or sets the original metadata
        /// </summary>
        public string OriginalMetadata { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the final metadata
        /// </summary>
        public string FinalMetadata { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the executed nodes
        /// </summary>
        public string ExecutedNodes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the node to process this file on
        /// </summary>
        public string ProcessOnNodeUid { get; set; } = string.Empty;
    }
    
    
    /// <summary>
    /// Statistic saved in the database
    /// </summary>
    public class DbStatisticOld
    {
        /// <summary>
        /// Gets or sets when the statistic was recorded
        /// </summary>
        public DateTime LogDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the statistic
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public StatisticType Type { get; set; }
    
        /// <summary>
        /// Gets or sets the string value
        /// </summary>
        public string StringValue { get; set; } = string.Empty;
    
        /// <summary>
        /// Gets or sets the number value
        /// </summary>
        public double NumberValue { get; set; }
        
        /// <summary>
        /// Statistic types
        /// </summary>
        public enum StatisticType
        {
            /// <summary>
            /// String statistic
            /// </summary>
            String = 0,
            /// <summary>
            /// Number statistic
            /// </summary>
            Number = 1
        }
    }
}