// using FileFlows.ServerShared.Models;
//
// namespace FileFlows.ServerShared.Services;
//
// using FileFlows.Shared.Helpers;
// using FileFlows.Shared.Models;
//
// /// <summary>
// /// Interface for communicating with FileFlows server for library files
// /// </summary>
// public interface ILibraryFileService
// {
//     /// <summary>
//     /// Gets the next library file queued for processing
//     /// </summary>
//     /// <param name="nodeName">The name of the node requesting a library file</param>
//     /// <param name="nodeUid">The UID of the node</param>
//     /// <param name="nodeVersion">the version of the node</param>
//     /// <param name="workerUid">The UID of the worker on the node</param>
//     /// <returns>If found, the next library file to process, otherwise null</returns>
//     Task<NextLibraryFileResult?> GetNext(string nodeName, Guid nodeUid, string nodeVersion, Guid workerUid);
//
//     /// <summary>
//     /// Gets a library file by its UID
//     /// </summary>
//     /// <param name="uid">The UID of the library file</param>
//     /// <returns>The library file if found, otherwise null</returns>
//     Task<LibraryFile?> Get(Guid uid);
//
//     /// <summary>
//     /// Deletes a library file
//     /// </summary>
//     /// <param name="uid">The UID to delete</param>
//     /// <returns>a completed task</returns>
//     Task Delete(Guid uid);
//
//     /// <summary>
//     /// Saves the full library file log
//     /// </summary>
//     /// <param name="uid">The UID of the library file</param>
//     /// <param name="log">The full plain text log to save</param>
//     /// <returns>If it was successfully saved or not</returns>
//     Task<bool> SaveFullLog(Guid uid, string log);
//     
//     /// <summary>
//     /// Tests if a library file exists on server.
//     /// This is used to test if a mapping issue exists on the node, and will be called if a Node cannot find the library file
//     /// </summary>
//     /// <param name="uid">The UID of the library file</param>
//     /// <returns>True if it exists on the server, otherwise false</returns>
//     Task<bool> ExistsOnServer(Guid uid);
//
//     /// <summary>
//     /// Tells the server not to check this node for number of seconds when checking for load balancing as it will
//     /// be unavailable for this amount of time
//     /// </summary>
//     /// <param name="nodeUid">the UID of the node</param>
//     /// <param name="forSeconds">the time in seconds</param>
//     /// <returns>a task to await</returns>
//     Task NodeCannotRun(Guid nodeUid, int forSeconds);
// }
