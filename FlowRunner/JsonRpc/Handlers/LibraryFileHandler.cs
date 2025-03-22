using System.Text.Json;
using FileFlows.FlowRunner.JsonRpc;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc.Handlers;

public class LibraryFileHandler(JsonRpcClient client)
{
    /// <summary>
    /// Tells the server to ignore the specified path when scanning
    /// </summary>
    /// <param name="path">the Path to ignore</param>
    public async Task LibraryIgnorePath(string path)
        => await client.SendRequest("LibraryIgnorePath", path);


    /// <summary>
    /// Updates the library file
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <returns>a task to await</returns>
    public async Task UpdateLibraryFile(LibraryFile libraryFile)
        => await client.SendRequest(nameof(UpdateLibraryFile), libraryFile);
    

    /// <summary>
    /// Sends a request to delete a library file by its unique identifier.
    /// </summary>
    /// <param name="uid">The unique identifier of the library file to delete.</param>
    public async Task DeleteLibraryFile(Guid uid)
        => await client.SendRequest("DeleteLibraryFile", uid);

    /// <summary>
    /// Checks if the file exists on the server
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>true if exists otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid uid)
        => await client.SendRequest<bool>(nameof(ExistsOnServer), uid);

    /// <summary>
    /// Retrieves a library file by its unique identifier.
    /// </summary>
    /// <param name="uid">The unique identifier of the library file.</param>
    /// <returns>The requested <see cref="LibraryFile"/> object.</returns>
    public async Task<LibraryFile> Get(Guid uid)
        => await client.SendRequest<LibraryFile>("GetLibraryFile", uid);

    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="binaryData">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    public async Task SetThumbnail(Guid libraryFileUid, byte[] binaryData)
        => await client.SendRequest("SetThumbnail", libraryFileUid, binaryData);
}