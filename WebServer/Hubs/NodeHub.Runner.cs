using System.Collections.Concurrent;
using FileFlows.LibraryUtils;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Runner methods from the node
/// </summary>
public partial class NodeHub
{
    /// <summary>
    /// Tells the server to ignore the specified path when scanning
    /// </summary>
    /// <param name="path">the Path to ignore</param>
    public void LibraryIgnorePath(string path)
    {
        try
        {
            Logger.Instance.ILog("Ignoring Path from library scanning: " + path);
            WatchedLibraryNew.IgnorePath(path);
        }
        catch(Exception ex)
        {
            Logger.Instance.ELog("Failed to ignore path from library scanning: " + path + " => " + ex.Message);
            // Ignored
        }
    }

    /// <summary>
    /// Tests if a file exists on the server
    /// </summary>
    /// <param name="libraryFileUid">the UID of the file</param>
    /// <returns>true if it exists, otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid libraryFileUid)
        => await ServiceLoader.Load<LibraryFileService>().ExistsOnServer(libraryFileUid);

    // /// <summary>
    // /// Gets updated file processing info from the node
    // /// </summary>
    // /// <param name="info">the updated processing info</param>
    // public async Task FileUpdateInfo(FlowExecutorInfo info)
    // {
    //     flowExecutors[info.Uid] = info;
    //     var minified = FlowRunnerService.GetMinified(flowExecutors.ToDictionary()).Values.ToList();
    //     await ClientServiceManager.Instance.UpdateExecutors(minified);
    // }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="libraryFile">the library file </param>
    public async Task FileStartProcessing(LibraryFile libraryFile)
    {
        var libraryFileService = ServiceLoader.Load<LibraryFileService>();
        await libraryFileService.SetStatus(FileStatus.Processing, libraryFile.Uid);
        var nodeService = ServiceLoader.Load<NodeService>();
        nodeService.StartProcessing(libraryFile);
    }

    /// <summary>
    /// Called when the file finishes processing
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <param name="log">the complete log of the file processing</param>
    public async Task FileFinishProcessing(LibraryFile libraryFile, string log)
    {
        var libraryFileService = ServiceLoader.Load<LibraryFileService>();
        await libraryFileService.Update(libraryFile);
        await libraryFileService.SaveFullLog(libraryFile.Uid, log);
        var nodeService = ServiceLoader.Load<NodeService>();
        nodeService.FinishProcess(libraryFile);
    }
    /// <summary>
    /// Prepends the text to the log file on the server
    /// </summary>
    /// <param name="libFileUid">the UID of the file</param>
    /// <param name="lines">the lines of the log</param>
    /// <param name="overwrite">if the file should be overwritten or appended to</param>
    public async Task FileLogAppend(Guid libFileUid, string lines, bool overwrite)
        => await LibraryFileLogHelper.AppendToLog(libFileUid, lines, overwrite);
    
    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="binaryData">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    public async Task SetThumbnail(Guid libraryFileUid, byte[] binaryData)
        => await ServiceLoader.Load<LibraryFileService>().SetThumbnail(libraryFileUid, binaryData);
}