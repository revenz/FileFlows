namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for a Flow Runner, which is responsible for executing a flow and processing files
/// </summary>
public interface IFlowRunnerService
{
    /// <summary>
    /// Gets if the server is licensed
    /// </summary>
    /// <returns>if hte server is licensed</returns>
    Task<bool> IsLicensed();

    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="binaryData">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    Task SetThumbnail(Guid libraryFileUid, byte[] binaryData);
}
