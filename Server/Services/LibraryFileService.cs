using System.Text.RegularExpressions;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Models;
using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Json;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService : ILibraryFileService
{

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public async Task<LibraryFile> Get(Guid uid) 
        => await Database_Get<LibraryFile>("select * from LibraryFile where Uid = @0", uid);
    
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<NextLibraryFileResult> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        _ = new NodeController().UpdateLastSeen(nodeUid);
        
        if (UpdaterWorker.UpdatePending)
            return NextFileResult (NextLibraryFileStatus.UpdatePending); // if an update is pending, stop providing new files to process

        var settings = await new SettingsController().Get();
        if (settings.IsPaused)
            return NextFileResult(NextLibraryFileStatus.SystemPaused);

        if (Version.TryParse(nodeVersion, out var nVersion) == false)
            return NextFileResult(NextLibraryFileStatus.InvalidVersion);

        if (nVersion < Globals.MinimumNodeVersion)
        {
            Logger.Instance.ILog($"Node '{nodeName}' version '{nVersion}' is less than minimum supported version '{Globals.MinimumNodeVersion}'");
            return NextFileResult(NextLibraryFileStatus.VersionMismatch);
        }

        var node = (await new NodeController().Get(nodeUid));
        if (node != null && node.Version != nodeVersion)
        {
            node.Version = nodeVersion;
            await new NodeController().Update(node);
        }

        if (await NodeEnabled(node) == false)
            return NextFileResult(NextLibraryFileStatus.NodeNotEnabled);

        var file = await GetNextLibraryFile(nodeName, nodeUid, workerUid);
        if(file == null)
            return NextFileResult(NextLibraryFileStatus.NoFile, file);
        return NextFileResult(NextLibraryFileStatus.Success, file);
    }
    
    private async Task<bool> NodeEnabled(ProcessingNode node)
    {
        var licensedNodes = LicenseHelper.GetLicensedProcessingNodes();
        var allNodes = await new NodeController().GetAll();
        var enabledNodes = allNodes.Where(x => x.Enabled).OrderBy(x => x.Name).Take(licensedNodes).ToArray();
        var enabledNodeUids = enabledNodes.Select(x => x.Uid).ToArray();
        return enabledNodeUids.Contains(node.Uid);
    }
    
    /// <summary>
    /// Constructs a next library file result
    /// <param name="status">the status of the call</param>
    /// <param name="file">the library file to process</param>
    /// </summary>
    private NextLibraryFileResult NextFileResult(NextLibraryFileStatus? status = null, LibraryFile file = null)
    {
        NextLibraryFileResult result = new();
        if (status != null)
            result.Status = status.Value;
        result.File = file;
        return result;
    }
    
    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    private async Task<LibraryFile> GetNextLibraryFile(string nodeName, Guid nodeUid, Guid workerUid)
    {
        var node = await new NodeController().Get(nodeUid);
        var nodeLibraries = node.Libraries?.Select(x => x.Uid)?.ToList() ?? new List<Guid>();
        var libraries = (await new LibraryController().GetAll()).ToArray();
        int quarter = TimeHelper.GetCurrentQuarter();
        var canProcess = libraries.Where(x =>
        {
            if (x.Enabled == false)
                return false;
            if (x.Schedule?.Length == 672 && x.Schedule[quarter] == '0')
                return false;
            if (node.AllLibraries == ProcessingLibraries.All)
                return true;
            if (node.AllLibraries == ProcessingLibraries.Only)
                return nodeLibraries.Contains(x.Uid);
            return nodeLibraries.Contains(x.Uid) == false;
        }).ToArray();
        if (canProcess.Any() != true)
            return null;

        string libraryUids = string.Join(",", canProcess.Select(x => "'" + x.Uid + "'"));
        
        await GetNextSemaphore.WaitAsync();
        try
        {
            var executing = WorkerController.ExecutingLibraryFiles();
            var execAndWhere = executing?.Any() != true
                ? string.Empty
                : (" and LibraryFile.Uid not in (" + string.Join(",", executing.Select(x => "'" + x + "'")) + ") ");
            
            string sql = $"select * from LibraryFile {LIBRARY_JOIN} where Status = 0 and HoldUntil <= " + SqlHelper.Now() +
                         $" and LibraryUid in ({libraryUids}) " + execAndWhere +
                         " order by " + UNPROCESSED_ORDER_BY;


            var libFile = await Database_Get<LibraryFile>(SqlHelper.Limit(sql, 1));
            if (libFile == null)
                return null;

            // At this point, libFile will contain either the next file to process (if any file has been moved to top),
            // or the latest modified file from the highest priority library.
            // Since this does not take into account individual library ordering, determine the library of the file selected
            // (should be the highest priority), then run the query again but only for that library and with the ordering
            // set as per the library settings.
            var library = libraries.FirstOrDefault(x => x.Uid == libFile.LibraryUid);
            if (libFile.Order < 1 && library != null && library.ProcessingOrder != ProcessingOrder.AsFound)
            {
                // need to change the order up
                bool orderGood = true;
                sql = $"select * from LibraryFile where Status = 0 and HoldUntil <= " + SqlHelper.Now() +
                      execAndWhere +
                      $" and LibraryUid = '{library.Uid}' order by ";
                if (library.ProcessingOrder == ProcessingOrder.Random)
                    sql += " " + SqlHelper.Random() + " ";
                else if (library.ProcessingOrder == ProcessingOrder.LargestFirst)
                    sql += " OriginalSize desc ";
                else if (library.ProcessingOrder == ProcessingOrder.SmallestFirst)
                    sql += " OriginalSize ";
                else if (library.ProcessingOrder == ProcessingOrder.NewestFirst)
                    sql += " LibraryFile.DateCreated desc ";
                else
                    orderGood = false;

                if (orderGood)
                {
                    libFile = await Database_Get<LibraryFile>(SqlHelper.Limit(sql, 1));
                    if (libFile == null)
                        return null;
                }
            }

            await Database_Execute("update LibraryFile set NodeUid = @0 , NodeName = @1 , WorkerUid = @2 " +
                                   $" , Status = @3 , ProcessingStarted = @4, OriginalMetadata = '', FinalMetadata = '', ExecutedNodes = '' where Uid = @5",
                nodeUid, nodeName, workerUid, (int)FileStatus.Processing, DateTime.Now, libFile.Uid);

            return libFile;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed getting next file for processing: " + ex.Message);
            throw;
        }
        finally
        {
            GetNextSemaphore.Release();
        }
    }

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    public Task<bool> SaveFullLog(Guid uid, string log) => new LibraryFileController().SaveFullLog(uid, log);


    /// <summary>
    /// Adds a library file
    /// </summary>
    /// <param name="file">the library file to add</param>
    /// <returns>the added library file</returns>
    public async Task<LibraryFile> Add(LibraryFile file)
    {
        if(file.Uid == Guid.Empty)
            file.Uid = Guid.NewGuid();
        
        if(file.DateCreated < new DateTime(2000, 1,1))
            file.DateCreated = DateTime.Now;
        if (file.DateModified < new DateTime(2000, 1, 1))
            file.DateModified = DateTime.Now;
        file.ExecutedNodes ??= new ();
        file.OriginalMetadata ??= new ();
        file.FinalMetadata ??= new ();
        await Database_Insert(file);
        return await Get(file.Uid);
    }
    /// <summary>
    /// Adds a many library file
    /// </summary>
    /// <param name="files">the library files to add</param>
    /// <returns>an awaited task</returns>
    public async Task AddMany(params LibraryFile[] files)
    {
        if (files?.Any() != true)
            return;

        foreach (var file in files)
        {
            if (file == null)
                continue;
            if(file.Uid == Guid.Empty)
                file.Uid = Guid.NewGuid();
            if(file.DateCreated < new DateTime(2000, 1,1))
                file.DateCreated = DateTime.Now;
            if (file.DateModified < new DateTime(2000, 1, 1))
                file.DateModified = DateTime.Now;
            file.ExecutedNodes ??= new ();
            file.OriginalMetadata ??= new ();
            file.FinalMetadata ??= new ();
        }

        await Database_AddMany(files);
    }
    
    /// <summary>
    /// Updates a library file
    /// </summary>
    /// <param name="file">The library file to update</param>
    /// <returns>The newly updated library file</returns>
    public async Task<LibraryFile> Update(LibraryFile file)
    {
        file.DateModified = DateTime.Now;
        file.ExecutedNodes ??= new ();
        file.OriginalMetadata ??= new ();
        file.FinalMetadata ??= new ();
        await Database_Update(file);
        return file;
    }

    /// <summary>
    /// Special case used by the flow runner to update a processing library file
    /// </summary>
    /// <param name="file">the processing library file</param>
    public async Task UpdateWork(LibraryFile file)
    {
        if (file == null)
            return;
        
        string sql = "update LibraryFile set " +
                     $" Status = {((int)file.Status)}, " + 
                     $" FinalSize = {file.FinalSize}, " +
                     (file.Node == null ? "" : (
                         $" NodeUid = '{file.NodeUid}', NodeName = '{file.NodeName.Replace("'", "''")}', "
                     )) +
                     $" WorkerUid = '{file.WorkerUid}', " +
                     $" ProcessingStarted = @0, " +
                     $" ProcessingEnded = @1, " + 
                     $" ExecutedNodes = @2 " +
                     $" where Uid = '{file.Uid}'";
        
        string executedJson = file.ExecutedNodes?.Any() != true
            ? string.Empty
            : JsonSerializer.Serialize(file.ExecutedNodes, CustomDbMapper.JsonOptions);
        await Database_Execute(sql, file.ProcessingStarted, file.ProcessingEnded, executedJson);
    }

    /// <summary>
    /// Deletes library files
    /// </summary>
    /// <param name="uids">a list of UIDs to delete</param>
    /// <returns>an awaited task</returns>
    public async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await Database_Execute($"delete from LibraryFile where Uid in ({inStr})", null);
    }

    /// <summary>
    /// Deletes library files from libraries
    /// </summary>
    /// <param name="libraryUids    ">a list of UIDs of libraries delete</param>
    /// <returns>an awaited task</returns>
    public async Task DeleteFromLibraries(params Guid[] libraryUids)
    {
        if (libraryUids?.Any() != true)
            return;
        string inStr = string.Join(",", libraryUids.Select(x => $"'{x}'"));
        await Database_Execute($"delete from LibraryFile where LibraryUid in ({inStr})", null);
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public Task<bool> ExistsOnServer(Guid uid) => new LibraryFileController().ExistsOnServer(uid);

    /// <summary>
    /// Get all the library file UIDs in the database
    /// </summary>
    /// <returns>all the library file UIDs in the database</returns>
    public async Task<IEnumerable<Guid>> GetUids()
        => await Database_Fetch<Guid>("select Uid from LibraryFile");

    /// <summary>
    /// Gets all matching library files
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="skip">the amount to skip</param>
    /// <param name="rows">the number to fetch</param>
    /// <param name="filter">[Optional] filter text</param>
    /// <returns>a list of matching library files</returns>
    public async Task<IEnumerable<LibraryFile>> GetAll(FileStatus?status, int skip = 0, int rows = 0, string filter = null)
    {
        try
        {
            bool hasFilter = string.IsNullOrWhiteSpace(filter) == false;
            string filterWhere = hasFilter ? $"lower(name) like lower('%{filter.Replace("'", "''").Replace(" ", "%")}%')" : string.Empty;
            if(status == null)
            {
                string sqlStatus =
                    SqlHelper.Skip($"select * from LibraryFile " + 
                                   (hasFilter ? $" where " + filterWhere : string.Empty) +
                                   " order by DateModified desc",
                        skip, rows);
                return await Database_Fetch<LibraryFile>(sqlStatus);
            }
            if ((int)status > 0)
            {
                // the status in the db is correct and not a computed status
                string orderBy = "";
                if (status is FileStatus.Processed or FileStatus.ProcessingFailed)
                    orderBy = "ProcessingEnded desc,";

                string sqlStatus =
                    SqlHelper.Skip($"select * from LibraryFile where Status = {(int)status} " +
                                   (hasFilter ? " and " + filterWhere : string.Empty) +
                                   $" order by {orderBy}DateModified desc",
                        skip, rows);
                return await Database_Fetch<LibraryFile>(sqlStatus);
            }
            
            var libraries = await new LibraryController().GetAll();
            var disabled = string.Join(", ",
                libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
            int quarter = TimeHelper.GetCurrentQuarter();
            var outOfSchedule = string.Join(", ",
                libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));

            string sql = $"select * from LibraryFile where Status = {(int)FileStatus.Unprocessed}";
            if (hasFilter)
                sql += " and " + filterWhere;
            
            // add disabled condition
            if(string.IsNullOrEmpty(disabled) == false)
                sql += $" and LibraryUid {(status == FileStatus.Disabled ? "" : "not")} in ({disabled})";
            
            if (status == FileStatus.Disabled)
            {
                if (string.IsNullOrEmpty(disabled))
                    return new List<LibraryFile>(); // no disabled libraries
                return await Database_Fetch<LibraryFile>(SqlHelper.Skip(sql + " order by DateModified", skip, rows));
            }
            
            // add out of schedule condition
            if(string.IsNullOrEmpty(outOfSchedule) == false)
                sql += $" and LibraryUid {(status == FileStatus.OutOfSchedule ? "" : "not")} in ({outOfSchedule})";
            
            if (status == FileStatus.OutOfSchedule)
            {
                if (string.IsNullOrEmpty(outOfSchedule))
                    return new List<LibraryFile>(); // no out of schedule libraries
                
                return await Database_Fetch<LibraryFile>(SqlHelper.Skip(sql + " order by DateModified", skip, rows));
            }
            
            // add on hold condition
            sql += $" and HoldUntil {(status == FileStatus.OnHold ? ">" : "<=")} " + SqlHelper.Now();
            if (status == FileStatus.OnHold)
                return await Database_Fetch<LibraryFile>(SqlHelper.Skip(sql + " order by HoldUntil, DateModified", skip, rows));

            sql = sql.Replace("select * from LibraryFile", "select LibraryFile.* from LibraryFile  " + LIBRARY_JOIN);

            sql = SqlHelper.Skip(sql + " order by " + UNPROCESSED_ORDER_BY, skip, rows);
            return await Database_Fetch<LibraryFile>(sql);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetAll Files: " + ex.Message + "\n" + ex.StackTrace);
            return new LibraryFile[] { };
        }
    }

    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public async Task<int> GetTotalMatchingItems(FileStatus status, string filter)
    {
        try
        {
            string filterWhere = $"lower(name) like lower('%{filter.Replace("'", "''").Replace(" ", "%")}%')" ;
            if(status == null)
            {
                return await Database_ExecuteScalar<int>("select count(Uid) from LibraryFile where " + filterWhere);
            }
            if ((int)status > 0)
            {
                return await Database_ExecuteScalar<int>($"select count(Uid) from LibraryFile where Status = {(int)status} and {filterWhere}");
            }
            
            var libraries = await new LibraryController().GetAll();
            var disabled = string.Join(", ",
                libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
            int quarter = TimeHelper.GetCurrentQuarter();
            var outOfSchedule = string.Join(", ",
                libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));

            string sql = $"select count(LibraryFile.Uid) from LibraryFile where Status = {(int)FileStatus.Unprocessed} and " + filterWhere;
            
            // add disabled condition
            if(string.IsNullOrEmpty(disabled) == false)
                sql += $" and LibraryUid {(status == FileStatus.Disabled ? "" : "not")} in ({disabled})";
            
            if (status == FileStatus.Disabled)
            {
                if (string.IsNullOrEmpty(disabled))
                    return 0;
                return await Database_ExecuteScalar<int>(sql);
            }
            
            // add out of schedule condition
            if(string.IsNullOrEmpty(outOfSchedule) == false)
                sql += $" and LibraryUid {(status == FileStatus.OutOfSchedule ? "" : "not")} in ({outOfSchedule})";
            
            if (status == FileStatus.OutOfSchedule)
            {
                if (string.IsNullOrEmpty(outOfSchedule))
                    return 0; // no out of schedule libraries
                
                return await Database_ExecuteScalar<int>(sql);
            }
            
            // add on hold condition
            sql += $" and HoldUntil {(status == FileStatus.OnHold ? ">" : "<=")} " + SqlHelper.Now();
            if (status == FileStatus.OnHold)
                return await Database_ExecuteScalar<int>(sql);

            sql = sql.Replace(" from LibraryFile", " from LibraryFile  " + LIBRARY_JOIN);
            return await Database_ExecuteScalar<int>(sql);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed GetTotalMatchingItems Files: " + ex.Message + "\n" + ex.StackTrace);
            return 0;
        }
    }

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="uids">a list of UIDs to reprocess</param>
    public async Task Reprocess(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await Database_Execute($"update LibraryFile set Status = 0 where Uid in ({inStr})");
    }

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="uids">the UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    public async Task Unhold(Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string inStr = string.Join(",", uids.Select(x => $"'{x}'"));
        await Database_Execute($"update LibraryFile set HoldUntil = '1970-01-01 00:00:01' where Uid in ({inStr})");
    }
    
    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    internal async Task ResetProcessingStatus(Guid? nodeUid = null)
    {
        if(nodeUid != null)
            await Database_Execute($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing} and NodeUid = '{nodeUid}'");
        else
            await Database_Execute($"update LibraryFile set Status = 0 where Status = {(int)FileStatus.Processing}");
    }

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <returns>the library file if it is known</returns>
    public async Task<LibraryFile> GetFileIfKnown(string path)
        => await Database_Get<LibraryFile>("select * from LibraryFile where Name = @0 or OutputPath = @0", path);

    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public async Task<LibraryFile> GetFileByFingerprint(string fingerprint)
        => await Database_Get<LibraryFile>(SqlHelper.Limit("select * from LibraryFile where fingerprint = @0", 1), fingerprint);

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="includeOutput">if output names should be included</param>
    /// <returns>a list of all filenames</returns>
    public async Task<Dictionary<string, DateTime>> GetKnownLibraryFilesWithCreationTimes(bool includeOutput = false)
    {
        var list = await Database_Fetch<(string, DateTime)>("select Name, CreationTime from LibraryFile");
        if (includeOutput == false)
            return list.DistinctBy(x => x.Item1.ToLowerInvariant()).ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2);
        var outputs = await Database_Fetch<(string, DateTime)>("select OutputPath, CreationTime from LibraryFile");
        return list.Union(outputs).Where(x => string.IsNullOrEmpty(x.Item1) == false).DistinctBy(x => x.Item1.ToLowerInvariant())
            .ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2);
    }

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public async Task<IEnumerable<LibraryStatus>> GetStatus()
    {
        var libraries = await new LibraryController().GetAll();
        var disabled = string.Join(", ",
            libraries.Where(x => x.Enabled == false).Select(x => "'" + x.Uid + "'"));
        int quarter = TimeHelper.GetCurrentQuarter();
        var outOfSchedule = string.Join(", ",
            libraries.Where(x => x.Schedule?.Length != 672 || x.Schedule[quarter] == '0').Select(x => "'" + x.Uid + "'"));

        string sql = @"
        select 
        case 
            when LibraryFile.Status > 0 then LibraryFile.Status " + "\n";
        if (string.IsNullOrEmpty(disabled) == false)
            sql += $" when LibraryFile.Status = 0 and LibraryUid IN ({disabled}) then -2 " + "\n";
        if (string.IsNullOrEmpty(outOfSchedule) == false)
            sql += $" when LibraryFile.Status = 0 and LibraryUid IN ({outOfSchedule}) then -1 " + "\n";
        sql += $@"when HoldUntil > {SqlHelper.Now()} then -3
        else LibraryFile.Status
        end as FileStatus,
        count(Uid) as Count
        from LibraryFile 
        group by FileStatus
";
        var statuses = await Database_Fetch<LibraryStatus>(sql);
        foreach (var status in statuses)
            status.Name = Regex.Replace(status.Status.ToString(), "([A-Z])", " $1").Trim();
        return statuses;
    }

    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the rfile</returns>
    public async Task<FileStatus> GetFileStatus(Guid uid)
    {
        return (FileStatus)await Database_ExecuteScalar<int>($"select Status from LibraryFile where Uid = '{uid}'");
    }

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public async Task MoveToTop(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return;
        string strUids = string.Join(", ", uids.Select(x => "'" + x + "'"));
        // get existing order first so we can shift those if these uids change the order
        // only get status == 0
        List<Guid> indexed = uids.ToList();
        var sorted = await Database_Fetch<LibraryFile>($"select * from LibraryFile where Status = 0 and ( ProcessingOrder > 0 or Uid IN ({strUids}))");
        sorted = sorted.OrderBy(x =>
        {
            int index = indexed.IndexOf(x.Uid);
            if (index < 0)
                return 10000 + x.Order;
            return index;
        }).ToList();

        var commands = new List<string>();
        for(int i=0;i<sorted.Count;i++)
        {
            var file = sorted[i];
            file.Order = i + 1;
            commands.Add($"update LibraryFile set ProcessingOrder = {file.Order} where Uid = '{file.Uid}';");
        }

        await Database_Execute(string.Join("\n", commands));
    }

    /// <summary>
    /// Gets the shrinkage groups for the files
    /// </summary>
    /// <returns>the shrinkage groups</returns>
    public async Task<List<ShrinkageData>> GetShrinkageGroups()
     => (await Database_Fetch<ShrinkageData>(
            $"select LibraryName as Library, sum(OriginalSize) as OriginalSize, sum(FinalSize) as FinalSize, Count(Uid) as Items " +
            $" from LibraryFile where Status = {(int)FileStatus.Processed}" +
            $" group by LibraryName;")).OrderByDescending(x => x.Items).ToList();

    /// <summary>
    /// Updates a flow name in the database
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the updated name of the flow</param>
    public async Task UpdateFlowName(Guid uid, string name)
        => await Database_Execute("update LibraryFile set FlowName = @0 where FlowUid = @1", name, uid);

    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    public async Task<IEnumerable<LibraryFile>> Search(LibraryFileSearchModel filter)
    {
        List<string> wheres = new();
        List<object> parameters = new List<object>();
        if (filter.FromDate > new DateTime(2000, 1, 1) && filter.ToDate < DateTime.Now.AddYears(100))
        {
            parameters.Add(filter.FromDate);
            parameters.Add(filter.ToDate);
            wheres.Add("DateCreated > @0");
            wheres.Add("DateCreated < @1");
        }

        if (string.IsNullOrWhiteSpace(filter.Path) == false)
        {
            // mysql better search, need to redo this
            // and match(Name) against (SearchText IN BOOLEAN MODE)
            //wheres.Add($"( match(Name) against (@{paramIndex}) IN BOOLEAN MODE) or match(OutputPath) against (@{paramIndex}) IN BOOLEAN MODE))");
            
            int paramIndex = parameters.Count;
            parameters.Add("%" + filter.Path.Replace(" ", "%") + "%");
            wheres.Add($"( lower(Name) like lower(@{paramIndex}) or lower(OutputPath) like lower(@{paramIndex}))");
        }
        if (string.IsNullOrWhiteSpace(filter.LibraryName) == false)
        {
            int paramIndex = parameters.Count;
            parameters.Add("%" + filter.LibraryName + "%");
            wheres.Add($"lower(LibraryName) like lower(@{paramIndex})");
        }
        string sql = SqlHelper.Skip("select * from LibraryFile where " + string.Join(" and ", wheres), skip: 0, rows: filter.Limit > 0 ? filter.Limit : 100);
        var results =  await Database_Fetch<LibraryFile>(sql, parameters);
        return results;
    }
    
    
    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public async Task<IEnumerable<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
    {
        string sql = @$"select 
LibraryName as {nameof(LibraryFileProcessingTime.Library)},
OriginalSize, " +
SqlHelper.TimestampDiffSeconds("ProcessingStarted", "ProcessingEnded", nameof(LibraryFileProcessingTime.Seconds)) + 
@" from LibraryFile 
where Status = 1 and ProcessingEnded > ProcessingStarted;";

        return await Database_Fetch<LibraryFileProcessingTime>(sql);
    }

    

    /// <summary>
    /// Gets data for a days/hours heatmap.  Where the list is the days, and the dictionary is the hours with the count as the values
    /// </summary>
    /// <returns>heatmap data</returns>
    public async Task<List<Dictionary<int, int>>> GetHourProcessingTotals()
    {
        
        string sql = @"select " +
                     SqlHelper.DayOfWeek("ProcessingStarted","day") + ", " + 
                     SqlHelper.Hour("ProcessingStarted","hour") + ", " +
                     " count(Uid) as count " + 
                     " from LibraryFile where Status = 1 AND ProcessingStarted > '2000-01-01 00:00:00' " +
                     " group by " + SqlHelper.DayOfWeek("ProcessingStarted") + "," +
                     SqlHelper.Hour("ProcessingStarted");

        List<(int day, int hour, int count)>
            data = (await Database_Fetch<(int day, int hour, int count)>(sql)).ToList();


        var days = new List<Dictionary<int, int>>();
        for (int i = 0; i < 7; i++)
        {
            var results = new Dictionary<int, int>();
            for (int j = 0; j < 24; j++)
            {
                // sun=1, mon=2, sat =7
                // so we use x.day - 1 here to convert sun=0
                int count = data.Where(x => (x.day - 1) == i && x.hour == j).Select(x => x.count).FirstOrDefault();
                results.Add(j, count);
            }

            days.Add(results);
        }

        return days;
    }
    
    
    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    public Task UpdateOriginalSize(Guid uid, long size) 
        => Database_Execute($"update LibraryFile set OriginalSize = {size} where Uid = '{uid}'");



    /// <summary>
    /// Aborts a file by setting its status to failed
    /// Only used to abort files that were processing on startup
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    public async Task Abort(Guid uid)
    {
        Logger.Instance.ILog("Aborting file: " + uid);
        await Database_Execute($"update LibraryFile set Status = {((int)FileStatus.ProcessingFailed)} where Uid = '{uid}'");
    }
}