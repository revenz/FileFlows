using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// An interface for communicating with the server for all Processing Node related actions
/// </summary>
public interface INodeService
{
    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    Task<ProcessingNode?> GetByAddressAsync(string address);
    
    /// <summary>
    /// Sets the node status
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    /// <param name="status">the new status</param>
    /// <returns>the status</returns>
    Task SetStatus(Guid uid, ProcessingNodeStatus? status);

    /// <summary>
    /// Gets an instance of the internal processing node
    /// </summary>
    /// <returns>an instance of the internal processing node</returns>
    Task<ProcessingNode?> GetServerNodeAsync();

    /// <summary>
    /// Gets the version the node can update to
    /// </summary>
    /// <returns>the version the node can update to</returns>
    Task<Version> GetNodeUpdateVersion();

    /// <summary>
    /// Gets if the processing nodes should auto update
    /// </summary>
    /// <returns>true if they should auto update</returns>
    Task<bool> AutoUpdateNodes();

    /// <summary>
    /// Get a 
    /// </summary>
    /// <returns></returns>
    Task<byte[]> GetNodeUpdater();
}

