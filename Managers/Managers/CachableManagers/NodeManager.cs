using System.Runtime.InteropServices;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models;
using Microsoft.Extensions.Logging;

namespace FileFlows.Managers;

/// <summary>
/// An Manager for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeManager : CachedManager<ProcessingNode>
{
    public override bool IncrementsConfiguration => false;
    private readonly Dictionary<Guid, DateTime> NodeLastSeenUpdate = new();

    /// <summary>
    /// Updates the last seen date for a node
    /// </summary>
    /// <param name="nodeUid">the UID node being updated</param>
    /// <param name="lastSeenUtc">the date the node was last seen</param>
    /// <returns>a task to await</returns>
    public async Task UpdateLastSeen(Guid nodeUid, DateTime lastSeenUtc)
    {
        if (UseCache)
        {
            var node = _Data?.FirstOrDefault(x => x.Uid == nodeUid);
            if (node != null)
            {
                node.LastSeen = lastSeenUtc;
                if(NodeLastSeenUpdate.TryGetValue(node.Uid, out var lsDate) && lsDate > DateTime.UtcNow.AddMinutes(-5))
                    return;
                NodeLastSeenUpdate[node.Uid] = lastSeenUtc;
            }
        }

        if (nodeUid == CommonVariables.InternalNodeUid)
            return; // no need to update this one

        string dt = lastSeenUtc.ToString("o"); // same format as json
        await DatabaseAccessManager.Instance.ObjectManager.SetDataValue(nodeUid, typeof(ProcessingNode).FullName,
            nameof(ProcessingNode.LastSeen), dt);
    }
    
    /// <summary>
    /// Gets a processing node by its physical address
    /// </summary>
    /// <param name="address">The address (hostname or IP address) of the node</param>
    /// <returns>An instance of the processing node</returns>
    public async Task<ProcessingNode?> GetByAddress(string address)
    {
        if (address == "INTERNAL_NODE")
            return await GetByUid(CommonVariables.InternalNodeUid);
        address = address.Trim().ToLowerInvariant();
        var all = await GetAll();
        return all.FirstOrDefault(x => x.Address.ToLowerInvariant() == address);
    }


    /// <summary>
    /// Updates the node version
    /// </summary>
    /// <param name="nodeUid">the UID of the node being updated</param>
    /// <param name="nodeVersion">the new version number</param>
    /// <returns>a task to await</returns>
    public Task UpdateVersion(Guid nodeUid, string nodeVersion)
        => DatabaseAccessManager.Instance.ObjectManager.SetDataValue(nodeUid, typeof(ProcessingNode).FullName,
            nameof(ProcessingNode.Version), nodeVersion);

    /// <summary>
    /// Ensures the internal processing node exists
    /// </summary>
    /// <returns>true if successful</returns>
    public async Task<Result<bool>> EnsureInternalNodeExists()
    {
        try
        {
            var manager = DatabaseAccessManager.Instance.FileFlowsObjectManager;
            var node = await manager.Single<ProcessingNode>(CommonVariables.InternalNodeUid);
            if (node.Failed(out string error))
                return Result<bool>.Fail(error);
            if (node.Value == null)
            {
                string tempPath;
                if (Globals.IsDocker)
                    tempPath = "/temp";
                else
                    tempPath = Path.Combine(DirectoryHelper.BaseDirectory, "Temp");

                if (Directory.Exists(tempPath) == false)
                    Directory.CreateDirectory(tempPath);

                node = new ProcessingNode
                {
                    Uid = CommonVariables.InternalNodeUid,
                    Name = CommonVariables.InternalNodeName,
                    Address = CommonVariables.InternalNodeName,
                    AllLibraries = ProcessingLibraries.All,
                    OperatingSystem = Globals.IsDocker ? OperatingSystemType.Docker :
                        Globals.IsWindows ? OperatingSystemType.Windows :
                        Globals.IsLinux ? OperatingSystemType.Linux :
                        Globals.IsMac ? OperatingSystemType.Mac :
                        OperatingSystemType.Unknown,
                    Architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm32 :
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? ArchitectureType.Arm64 :
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm64 :
                        RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ArchitectureType.x64 :
                        RuntimeInformation.ProcessArchitecture == Architecture.X86 ? ArchitectureType.x86 :
                        ArchitectureType.Unknown,
                    Schedule = new string('1', 672),
                    Enabled = true,
                    FlowRunners = 1,
                    TempPath = tempPath,
                };
            }
            else
            {
                node.Value.Version = Globals.Version;
            }

            await manager.AddOrUpdateObject((FileFlowObject)node.Value!, auditDetails: AuditDetails.ForServer());

            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed to ensure default node exists: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<bool>.Fail(ex.Message);
        } 
    }

    /// <summary>
    /// Gets the total files each node has processed
    /// </summary>
    /// <returns>A dictionary of the total files indexed by the node UID</returns>
    public Task<Dictionary<Guid, int>> GetTotalFiles()
        => DatabaseAccessManager.Instance.LibraryFileManager.GetNodeTotalFiles();
}