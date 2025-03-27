using System.Text.Json;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

public class LibraryFileHandler
{
    private JsonRpcServer rpcServer;
    private HubConnection _connection;
    public LibraryFileHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _connection = rpcServer._client._connection;
        rpcRegister.Register<LibraryFile>(nameof(UpdateLibraryFile), UpdateLibraryFile);
        rpcRegister.Register(nameof(LibraryIgnorePath), LibraryIgnorePath);
        rpcRegister.Register(nameof(DeleteLibraryFile), DeleteLibraryFile);
        rpcRegister.Register(nameof(ExistsOnServer), ExistsOnServer);
        rpcRegister.Register(nameof(SetThumbnail), SetThumbnail);
    }
    
    public void UpdateLibraryFile(LibraryFile libraryFile)
    {
        try
        {
            if (libraryFile != null)
                rpcServer._libraryFile = libraryFile;
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    /// <summary>
    /// Tells the server to ignore the specified path when scanning
    /// </summary>
    /// <param name="path">the Path to ignore</param>
    public async Task LibraryIgnorePath(string path)
        => await _connection.SendAsync("LibraryIgnorePath", path);
    

    public void DeleteLibraryFile(Guid uid)
    {
        throw new Exception("TODO");
    }

    /// <summary>
    /// Checks if the file exists on the server
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>true if exists otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid uid)
    {
        throw new Exception("This is failing cos i want it to ");
        //=> await _connection.InvokeAsync<bool>("ExistsOnServer", uid);
    }

    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="base64">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    public void SetThumbnail(string base64)
    {
        _ = _connection.InvokeAsync("SetThumbnail", this.rpcServer.runnerParameters.LibraryFile.Uid,
            Convert.FromBase64String(base64));
        
    }
}