using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Manager for the library files
/// </summary>
public class LibraryFileManager
{
    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>the library file, or null if not found</returns>
    public Task<LibraryFile?> Get(Guid uid)
        => DatabaseAccessManager.Instance.LibraryFileManager.Get(uid);

    /// <summary>
    /// Gets a library file if it is known
    /// </summary>
    /// <param name="path">the path of the library file</param>
    /// <param name="libraryUid">[Optional] the UID of the library the file is in, if not passed in then the first file with the name will be used</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?> GetFileIfKnown(string path, Guid? libraryUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileIfKnown(path, libraryUid);
    
    /// <summary>
    /// Gets a library file if it is known by its fingerprint
    /// </summary>
    /// <param name="libraryUid">The UID of the library</param>
    /// <param name="fingerprint">the fingerprint of the library file</param>
    /// <returns>the library file if it is known</returns>
    public Task<LibraryFile?> GetFileByFingerprint(Guid libraryUid, string fingerprint)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileByFingerprint(libraryUid, fingerprint);

    /// <summary>
    /// Adds a files 
    /// </summary>
    /// <param name="files">the files being added</param>
    public Task Insert(params LibraryFile[] files)
        => DatabaseAccessManager.Instance.LibraryFileManager.InsertBulk(files);

    /// <summary>
    /// Updates a file 
    /// </summary>
    /// <param name="file">the file being updated</param>
    public Task UpdateFile(LibraryFile file)
        => DatabaseAccessManager.Instance.LibraryFileManager.Update(file);

    /// <summary>
    /// Remove files from the cache
    /// </summary>
    /// <param name="uids">UIDs to remove</param>
    public Task Remove(params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.Delete(uids);

    /// <summary>
    /// Gets the library status overview
    /// </summary>
    /// <returns>the library status overview</returns>
    public async Task<List<LibraryStatus>> GetStatus()
    {
        var libraries = await new LibraryManager().GetAll();
        return await DatabaseAccessManager.Instance.LibraryFileManager.GetStatus(libraries);
    }

    /// <summary>
    /// Clears the executed nodes, metadata, final size etc for a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="flowUid">the UID of the flow that will be executed</param>
    /// <param name="flowName">the name of the flow that will be executed</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task ResetFileInfoForProcessing(Guid uid, Guid? flowUid, string? flowName)
        => DatabaseAccessManager.Instance.LibraryFileManager.ResetFileInfoForProcessing(uid, flowUid, flowName);

    /// <summary>
    /// Deletes files from the database
    /// </summary>
    /// <param name="uids">the UIDs of the files to remove</param>
    public Task Delete(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.Delete(uids);

    /// <summary>
    /// Gets all matching library files
    /// </summary>
    /// <param name="filter">the filter to get files for</param>
    /// <returns>a list of matching library files</returns>
    public async Task<List<LibraryFile>> GetAll(LibraryFileFilter filter)
        => await DatabaseAccessManager.Instance.LibraryFileManager.GetAll(filter);

    /// <summary>
    /// Gets the total items matching the filter
    /// </summary>
    /// <param name="filter">the filter</param>
    /// <returns>the total number of items matching</returns>
    public Task<int> GetTotalMatchingItems(LibraryFileFilter filter)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetTotalMatchingItems(filter);

    /// <summary>
    /// Updates a file as started processing
    /// </summary>
    /// <param name="nextFileUid">the UID of the file</param>
    /// <param name="nodeUid">the UID of the node processing this file</param>
    /// <param name="nodeName">the name of the node processing this file</param>
    /// <param name="workerUid">the UID of the worker processing this file</param>
    /// <returns>true if successfully updated, otherwise false</returns>
    public Task<bool> StartProcessing(Guid nextFileUid, Guid nodeUid, string nodeName, Guid workerUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.StartProcessing(nextFileUid, nodeUid, nodeName, workerUid);

    /// <summary>
    /// Unhold library files
    /// </summary>
    /// <param name="uids">the UIDs to unhold</param>
    /// <returns>an awaited task</returns>
    public Task Unhold(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.SetStatus(FileStatus.Unprocessed, uids);

    /// <summary>
    /// Updates all files with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateFlowName(uid, name);

    /// <summary>
    /// Updates all files with the new library name if they used this library
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    /// <param name="name">the new name of the library</param>
    /// <returns>a task to await</returns>
    public Task UpdateLibraryName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateLibraryName(uid, name);

    /// <summary>
    /// Updates all files with the new node name if they used this node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <param name="name">the new name of the node</param>
    /// <returns>a task to await</returns>
    public Task UpdateNodeName(Guid uid, string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateNodeName(uid, name);

    /// <summary>
    /// Deletes files from the database
    /// </summary>
    /// <param name="uids">the UIDs of the libraries to remove</param>
    public Task DeleteByLibrary(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.DeleteByLibrary(uids);

    /// <summary>
    /// Reprocess all files based on library UIDs
    /// </summary>
    /// <param name="uids">an array of UID of the libraries to reprocess</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ReprocessByLibraryUid(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ReprocessByLibraryUid(uids);

    /// <summary>
    /// Gets all the UIDs for library files in the system
    /// </summary>
    /// <returns>the UIDs of known library files</returns>
    public Task<List<Guid>> GetUids()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetUids();

    /// <summary>
    /// Gets the processing time for each library file 
    /// </summary>
    /// <returns>the processing time for each library file</returns>
    public Task<List<LibraryFileProcessingTime>> GetLibraryProcessingTimes()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetLibraryProcessingTimes();


    /// <summary>
    /// Updates the original size of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <param name="size">the size of the file in bytes</param>
    /// <returns>true if a row was updated, otherwise false</returns>
    public Task<bool> UpdateOriginalSize(Guid uid, long size)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateOriginalSize(uid, size);

    /// <summary>
    /// Resets any currently processing library files 
    /// This will happen if a server or node is reset
    /// </summary>
    /// <param name="nodeUid">[Optional] the UID of the node</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> ResetProcessingStatus(Guid? nodeUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.ResetProcessingStatus(nodeUid);


    /// <summary>
    /// Gets the current status of a file
    /// </summary>
    /// <param name="uid">The UID of the file</param>
    /// <returns>the current status of the file</returns>
    public Task<FileStatus?> GetFileStatus(Guid uid)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetFileStatus(uid);

    // /// <summary>
    // /// Special case used by the flow runner to update a processing library file
    // /// </summary>
    // /// <param name="file">the processing library file</param>
    // public Task UpdateWork(LibraryFile file)
    //     => DatabaseAccessManager.Instance.LibraryFileManager.UpdateWork(file);

    /// <summary>
    /// Moves the passed in UIDs to the top of the processing order
    /// </summary>
    /// <param name="uids">the UIDs to move</param>
    public Task MoveToTop(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.MoveToTop(uids);

    /// <summary>
    /// Sets a status on a file
    /// </summary>
    /// <param name="status">The status to set</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> SetStatus(FileStatus status, params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.SetStatus(status, uids);

    /// <summary>
    /// Toggles a flag on files
    /// </summary>
    /// <param name="flag">the flag to toggle</param>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ToggleFlag(LibraryFileFlags flag, params Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ToggleFlag(flag, uids);

    /// <summary>
    /// Force processing a set of files
    /// </summary>
    /// <param name="uids">the UIDs of the files</param>
    /// <returns>true if any rows were updated, otherwise false</returns>
    public Task<bool> ForceProcessing(Guid[] uids)
        => DatabaseAccessManager.Instance.LibraryFileManager.ForceProcessing(uids);

    /// <summary>
    /// Updates a moved file in the database
    /// </summary>
    /// <param name="file">the file to update</param>
    /// <returns>true if any files were updated</returns>
    public Task<bool> UpdateMovedFile(LibraryFile file)
        => DatabaseAccessManager.Instance.LibraryFileManager.UpdateMovedFile(file);

    /// <summary>
    /// Gets a list of all filenames and the file creation times
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>a list of all filenames</returns>
    public Task<List<KnownFileInfo>> GetKnownLibraryFilesWithCreationTimes(Guid libraryUid)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetKnownLibraryFilesWithCreationTimes(libraryUid);


    /// <summary>
    /// Gets the total storage saved
    /// </summary>
    /// <returns>the total storage saved</returns>
    public Task<long> GetTotalStorageSaved()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetTotalStorageSaved();
    
    /// <summary>
    /// Gets the total rows, sum of OriginalSize, and sum of FinalSize from the LibraryFile table grouped by Library.
    /// </summary>
    /// <param name="lastDays">The number of last days to get, or 0 for all</param>
    /// <returns>A list of library statistics</returns>
    public Task<List<(Guid LibraryUid, int TotalFiles, long SumOriginalSize, long SumFinalSize)>> GetLibraryFileStats(int lastDays = 0)
        => DatabaseAccessManager.Instance.LibraryFileManager.GetLibraryFileStats(lastDays);

    /// <summary>
    /// Performs a search for files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the matching files</returns>
    public Task<List<LibraryFile>> Search(LibraryFileSearchModel filter)
        => DatabaseAccessManager.Instance.LibraryFileManager.Search(filter);

    /// <summary>
    /// Gets if a file exists
    /// </summary>
    /// <param name="name">the name of the file</param>
    /// <returns>true if exists, otherwise false</returns>
    public Task<bool> FileExists(string name)
        => DatabaseAccessManager.Instance.LibraryFileManager.FileExists(name);

    /// <summary>
    /// Reset processing for the files
    /// </summary>
    /// <param name="model">the reprocess model</param>
    /// <param name="onlySetProcessInfo">if only the process information should be set, ie these are unprocessed files</param>
    public Task Reprocess(ReprocessModel model, bool onlySetProcessInfo)
        => DatabaseAccessManager.Instance.LibraryFileManager.Reprocess(model, onlySetProcessInfo);

    /// <summary>
    /// Deletes the tags from any file
    /// </summary>
    /// <param name="uids">the UIDs of the tags being deleted</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task DeleteTags(Guid[] uids, AuditDetails auditDetails)
        => await DatabaseAccessManager.Instance.LibraryFileManager.DeleteTags(uids, auditDetails);
}