using System.Text.Json;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

public class LibraryFileHandler
{
    private JsonRpcServer rpcServer;
    private ClientConnection _client;
    public LibraryFileHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _client = rpcServer._client.Connection;
        rpcRegister.Register<LibraryFile>(nameof(UpdateLibraryFile), UpdateLibraryFile);
        rpcRegister.Register(nameof(LibraryIgnorePath), LibraryIgnorePath);
        // rpcRegister.Register(nameof(DeleteLibraryFile), DeleteLibraryFile);
        rpcRegister.Register<ExistsOnServerModel, bool>(nameof(ExistsOnServer), ExistsOnServer);
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
    {
        try
        {
            if (await _client.AwaitConnection())
                await _client.SendAsync("LibraryIgnorePath", path);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Checks if the file exists on the server
    /// </summary>
    /// <param name="model">The model</param>
    /// <returns>true if exists otherwise false</returns>
    public async Task<bool> ExistsOnServer(ExistsOnServerModel model)
    {
        try
        {
            if (await _client.AwaitConnection())
                return await _client.InvokeAsync<bool>(nameof(ExistsOnServer), model.Path, model.IsDirectory);
        }
        catch (Exception)
        {
            // Ignore
        }

        return false;
    }

    /// <summary>
    /// Sets a thumbnail for a file
    /// </summary>
    /// <param name="base64">the binary data for the thumbnail</param>
    /// <returns>a completed task</returns>
    public void SetThumbnail(string base64)
    {
        try
        {
            if (_client.AwaitConnection().GetAwaiter().GetResult())
                _ = _client.SendAsync("SetThumbnail", this.rpcServer.runnerParameters.LibraryFile.Uid,
                    Convert.FromBase64String(base64));
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Model for exists on server
    /// </summary>
    public class ExistsOnServerModel
    {
        /// <summary>
        /// Gets or sets the path
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Gets or sets if its a directory
        /// </summary>
        public bool IsDirectory { get; set; }
    }
}