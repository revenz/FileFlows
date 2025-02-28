using System.Net;
using System.Runtime.InteropServices;

namespace FileFlows.RemoteServices;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService : RemoteService, INodeService
{   
    /// <inheritdoc />
    public async Task ClearWorkersAsync(Guid nodeUid)
    {
        try
        {
            await HttpHelper.Post(ServiceBaseUrl + "/remote/work/clear/" + Uri.EscapeDataString(nodeUid.ToString()));
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <inheritdoc />
    public async Task<ProcessingNode?> GetServerNodeAsync()
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/remote/node/by-address/INTERNAL_NODE");
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to locate server node: " + ex.Message);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<ProcessingNode?> GetByAddressAsync(string address)
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/remote/node/by-address/" + Uri.EscapeDataString(address) + "?version=" + Globals.Version);
            if (result.Success == false)
                throw new Exception("Failed to get node: " + result.Body);                
            if(result.Data == null)
            {
                // node does not exist
                Logger.Instance.ILog("Node does not exist: " + address);
                return null;
            }
            result.Data.SignalrUrl = ServiceBaseUrl + "/" + result.Data.SignalrUrl;
            return result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get node by address: " + ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetStatus(Guid uid, ProcessingNodeStatus? status)
    {
        try
        {
            await HttpHelper.Post(ServiceBaseUrl + "/remote/node/" + uid + $"/status/{status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to set node status: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProcessingNode?> GetByUidAsync(Guid uid)
    {
        try
        {
            var result = await HttpHelper.Get<ProcessingNode>(ServiceBaseUrl + "/remote/node/" + uid + "?version=" + Globals.Version);
            if (result.Success == false)
                throw new Exception("Failed to get node: " + result.Body);                
            if(result.Data == null)
            {
                // node does not exist
                throw new Exception("Node does not exist: " + uid);
            }
            result.Data.SignalrUrl = ServiceBaseUrl + "/" + result.Data.SignalrUrl;
            return result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get node by address: " + ex.Message + Environment.NewLine + ex.StackTrace);
            throw;
        }
    }
    
    /// <summary>
    /// Registers a node with FileFlows
    /// </summary>
    /// <param name="serverUrl">The URL of the FileFlows Server</param>
    /// <param name="address">The address (Hostname or IP Address) of the node</param>
    /// <param name="tempPath">The temporary path location of the node</param>
    /// <param name="mappings">Any mappings for the node</param>
    /// <param name="hardwareInfo">The hardware information</param>
    /// <returns>An instance of the registered node</returns>
    /// <exception cref="Exception">If fails to register, an exception will be thrown</exception>
    public async Task<ProcessingNode?> Register(string serverUrl, string address, string tempPath, List<RegisterModelMapping> mappings, HardwareInfo? hardwareInfo)
    {
        if(serverUrl.EndsWith("/"))
            serverUrl = serverUrl.Substring(0, serverUrl.Length - 1);

        var result = await HttpHelper.Post<ProcessingNode>(serverUrl + "/remote/node/register", new RegisterModel
        {
            Address = address,
            TempPath = tempPath,
            // FlowRunners = runners,
            // Enabled = enabled,
            Mappings = mappings,
            Version = Globals.Version,
            Architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm32 :
                           RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? ArchitectureType.Arm64 :
                           RuntimeInformation.ProcessArchitecture == Architecture.Arm ? ArchitectureType.Arm64 :
                           RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ArchitectureType.x64 : 
                           RuntimeInformation.ProcessArchitecture == Architecture.X86 ? ArchitectureType.x86 :
                           IntPtr.Size == 8 ? ArchitectureType.x64 : 
                           IntPtr.Size == 4 ? ArchitectureType.x86 :
                           ArchitectureType.Unknown,
            OperatingSystem = Globals.IsDocker ? OperatingSystemType.Docker : 
                Globals.IsWindows ? OperatingSystemType.Windows : 
                 Globals.IsLinux ? OperatingSystemType.Linux :       
                 Globals.IsMac ? OperatingSystemType.Mac :
                 Globals.IsFreeBsd ? OperatingSystemType.FreeBsd :
                 OperatingSystemType.Unknown,
            HardwareInfo = hardwareInfo
        }, timeoutSeconds: 10);

        if (result.Success == false)
            throw new Exception("Failed to register node: " + result.Body);

        return result.Data;
    }

    /// <inheritdoc />
    public async Task<Version> GetNodeUpdateVersion()
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/remote/node/update-version?version={Globals.Version}&system={GetSystem()}");
            if (result.Success == false)
                throw new Exception(result.Body);
            if (string.IsNullOrWhiteSpace(result.Data))
                return new Version();
            return Version.Parse(result.Data);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get node update version: " + ex.Message);
            return new Version(0, 0, 0, 0);
        }
    }
    
    string GetSystem() =>
        Globals.IsDocker ? "docker" :
        Globals.IsWindows ? "windows" :
        Globals.IsLinux ? "linux" :
        Globals.IsMac ? "macos" :
        "unknown";

    /// <inheritdoc />
    public async Task<bool> AutoUpdateNodes()
    {
        try
        {
            var result = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/remote/node/auto-update-nodes");
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
    public async Task<byte[]> GetNodeUpdater()
    {
        try
        {
            var result = await HttpHelper.Get<byte[]>($"{ServiceBaseUrl}/remote/node/updater?version={Globals.Version}&system={GetSystem()}");
            if (result.Success == false || result.Data == null)
                throw new Exception("Failed to get update: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get update: " + ex.Message);
            return [];
        }
    }


    // /// <summary>
    // /// Records the node system statistics to the server
    // /// </summary>
    // /// <param name="args">the node system statistics</param>
    // /// <returns>the task to await</returns>
    // public async Task RecordNodeSystemStatistics(NodeSystemStatistics args)
    // {
    //     try
    //     {
    //         await HttpHelper.Post($"{ServiceBaseUrl}/remote/node/system-statistics", args);
    //     }
    //     catch (Exception)
    //     {
    //         // Ignored
    //     }
    // }

    /// <inheritdoc />
    public async Task Pause(int minutes)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/remote/node/pause?minutes=" + minutes);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    /// <inheritdoc />
    public async Task<bool> GetSystemIsRunning()
    {
        try
        {
            var isPaused = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/remote/node/system-is-paused");
            if (isPaused.Success == false)
                return false;
            return isPaused.Data == false;
        }
        catch (Exception)
        {
            // Ignored
            return false;
        }
    }
}
