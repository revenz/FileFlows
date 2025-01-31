using System.Runtime.InteropServices;
using FileFlows.Services;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs once when the server is started
/// </summary>
public class StartupWorker:Worker
{
    /// <summary>
    /// Constructs a new instance of the worker
    /// </summary>
    public StartupWorker() : base(ScheduleType.Startup, 30)
    {
    }

    /// <summary>
    /// Executes
    /// </summary>
    protected override void Execute()
    {
        UpdateInternalNode();
    }
    
    /// <summary>
    /// Update the internal node information
    /// </summary>
    private void UpdateInternalNode()
    {
        var service = ServiceLoader.Load<NodeService>();
        var internalNode = service.GetByUidAsync(CommonVariables.InternalNodeUid).Result;
        if (internalNode == null)
            return;
        internalNode.Architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm32 :
                                    RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? ArchitectureType.Arm64 :
                                    RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm64 :
                                    RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ArchitectureType.x64 : 
                                    RuntimeInformation.ProcessArchitecture == Architecture.X86 ? ArchitectureType.x86 :
                                    IntPtr.Size == 8 ? ArchitectureType.x64 :
                                    IntPtr.Size == 4 ? ArchitectureType.x86 : 
                                    ArchitectureType.x64; // default to x64
        internalNode.OperatingSystem = Globals.IsDocker  ? OperatingSystemType.Docker : 
                                       Globals.IsWindows ? OperatingSystemType.Windows :
                                       Globals.IsLinux ? OperatingSystemType.Linux :
                                       Globals.IsMac ? OperatingSystemType.Mac :
                                       Globals.IsFreeBsd ? OperatingSystemType.FreeBsd :
                                       OperatingSystemType.Unknown;
        service.Update(internalNode, auditDetails: AuditDetails.ForServer()).Wait();
    }
}