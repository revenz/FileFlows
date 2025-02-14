using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for a Flow Runner, which is responsible for executing a flow and processing files
/// </summary>
public interface IFlowRunnerService
{
    /// <summary>
    /// Gets the file check interval in seconds
    /// </summary>
    /// <returns>the file check interval in seconds</returns>
    Task<int> GetFileCheckInterval();

    /// <summary>
    /// Gets if the server is licensed
    /// </summary>
    /// <returns>if hte server is licensed</returns>
    Task<bool> IsLicensed();
    
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>The updated information</returns>
    Task<FlowExecutorInfo?> Start(FlowExecutorInfo info);
    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    Task Finish(FlowExecutorInfo info);
    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    Task Update(FlowExecutorInfo info);

    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="binaryData">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    Task SetThumbnail(Guid libraryFileUid, byte[] binaryData);

    /// <summary>
    /// Gets the username of the reseller user
    /// </summary>
    /// <param name="resellerUserUid">the UID of the reseller user</param>
    /// <returns>the reseller user username</returns>
    Task<string> GetResellerUserUsername(Guid resellerUserUid);
}
