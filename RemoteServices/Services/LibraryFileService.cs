
namespace FileFlows.RemoteServices;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public class LibraryFileService : RemoteService, ILibraryFileService
{
    /// <inheritdoc />
    public async Task Delete(Guid uid)
    {
        try
        {
            await HttpHelper.Delete($"{ServiceBaseUrl}/remote/library-file/{uid}");                
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    /// Tests if a library file exists on server.
    /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>True if it exists on the server, otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/remote/library-file/exists-on-server/{uid}");
            if (result.Success == false)
                return false;
            return result.Data;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task NodeCannotRun(Guid nodeUid, int forSeconds)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/library-file/node-cannot-run/{nodeUid}/{forSeconds}"); 
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <summary>
    /// Gets a library file by its UID
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>The library file if found, otherwise null</returns>
    public async Task<LibraryFile?> Get(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<LibraryFile>($"{ServiceBaseUrl}/remote/library-file/{uid}");
            if (result.Success == false)
                return null;
            return result.Data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the next library file queued for processing
    /// </summary>
    /// <param name="nodeName">The name of the node requesting a library file</param>
    /// <param name="nodeUid">The UID of the node</param>
    /// <param name="nodeVersion">the version of the node</param>
    /// <param name="workerUid">The UID of the worker on the node</param>
    /// <returns>If found, the next library file to process, otherwise null</returns>
    public async Task<NextLibraryFileResult?> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid)
    {
        // can throw exception if nothing to process
        try
        {
            var result = await HttpHelper.Post<NextLibraryFileResult>($"{ServiceBaseUrl}/remote/library-file/next-file", new NextLibraryFileArgs
            {
                NodeName = nodeName,
                NodeUid = nodeUid,
                WorkerUid = workerUid,
                NodeVersion = Globals.Version
            });
            if (result.Success == false)
                return null; 
            return result.Data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the full library file log
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="log">The full plain text log to save</param>
    /// <returns>If it was successfully saved or not</returns>
    public async Task<bool> SaveFullLog(Guid uid, string log)
    {
        try
        {
            var result = await HttpHelper.Put<bool>($"{ServiceBaseUrl}/remote/library-file/{uid}/full-log", log);
            if (result.Success)
                return result.Data;
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
