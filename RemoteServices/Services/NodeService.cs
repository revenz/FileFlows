namespace FileFlows.RemoteServices;

/// <summary>
/// An Service for communicating with the server for all Processing Node related actions
/// </summary>
public class NodeService : RemoteService, INodeService
{   
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
}
