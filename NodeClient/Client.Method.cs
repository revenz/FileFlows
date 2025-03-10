using System.Runtime.InteropServices;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient;

/// <summary>
/// Methods to call on the client
/// </summary>
public partial class Client
{
    public bool IsRegistered => _node != null;
    private Guid _nodeUid => _node?.Uid ?? Guid.Empty;
    private readonly string _hostname;
    private readonly RunnerManager _runnerManager;
    private ProcessingNode? _node;
    public Guid? NodeUid => _node?.Uid;
    private ClientParameters _parameters;
    
    private async Task DownloadConfiguration()
    {
        try
        {
            if (_node == null)
                return;

            _logger.ILog("Downloading configuration...");
            var cfg = await _connection.InvokeAsync<ConfigurationRevision>("GetConfiguration");
            if (cfg == null || _node == null)
                return;
            
            await _configurationService.SaveConfiguration(cfg, _node);
            _logger.ILog($"Node '{_node.Name}' Configuration updated to {cfg.Revision}");
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error downloading configuration: {ex.Message}");
        }
    }


    private async Task UpdateConfiguration(ConfigurationRevision obj)
    {
        if (_node == null)
            return;
        
        _logger.ILog("Updating configuration...");
        await _configurationService.SaveConfiguration(obj, _node);
        
        TriggerStatusUpdate();
    }

    private void UpdateNode(ProcessingNode obj)
    {
        _logger.ILog("Updating node information...");
        _node = obj;
        _registrationCompletion.TrySetResult(true);
    }
    
    /// <summary>
    /// Registers the node with the server
    /// </summary>
    private async Task RegisterNodeAsync()
    {
        try
        {
            _logger.ILog("Registering node...");
            _node = null;
            
            string path = DirectoryHelper.BaseDirectory;

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
            {
                new()
                {
                    Server = "ffmpeg",
                    Local =  Globals.IsDocker ? "/usr/local/bin/ffmpeg" :
                        windows ? Path.Combine(path, "Tools", "ffmpeg.exe") : "/usr/local/bin/ffmpeg"
                }
            };
            if (_parameters.EnvironmentalMappings?.Any() == true)
            {
                _logger.ILog("Environmental mappings found, adding those");
                mappings.AddRange(_parameters.EnvironmentalMappings);
            }

            string tempPath =  _parameters.ForcedTempPath?.EmptyAsNull() 
                               ?? (Globals.IsDocker ? "/temp" : 
                                   Path.Combine(DirectoryHelper.BaseDirectory, "Temp"));
        
            HardwareInfo? hardwareInfo = null;
            try
            {
                hardwareInfo = ServiceLoader.Load<HardwareInfoService>().GetHardwareInfo();
                _logger.ILog("Hardware Info: " + Environment.NewLine + hardwareInfo);
            }
            catch(Exception ex)
            {
                _logger.ELog("Failed to get hardware info: " + ex.Message);
            }

            var parameters = new NodeRegisterParameters()
            {
                Hostname = _hostname,
                ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                HardwareInfo = hardwareInfo,
                TempPath = tempPath,
                Version = Globals.Version,
                Mappings = mappings.Select(x => new KeyValuePair<string, string>(x.Server, x.Local)).ToList(),
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
            };
            
            var result = await _connection.InvokeAsync<NodeRegisterResult>("RegisterNode", parameters);
            
            if (!result.Success)
                return;

            
            _node = result.Node;
            _logger.ILog("Node successfully registered.");
            _registrationCompletion.TrySetResult(true);

            if (result.ServerVersion != Globals.Version)
            {
                _logger.ILog(
                    $"Server version '{result.ServerVersion}' does not match node version '{Globals.Version}'");
                EventManager.Broadcast("NodeVersionMismatch", result.ServerVersion);
                //Dispose();
            }
            else if (result.CurrentConfigRevision != _configurationService.CurrentConfig?.Revision)
                _ = DownloadConfiguration();
        }
        catch (Exception ex)
        {
            _logger.ELog($"Node registration failed: {ex.Message}");
            _registrationCompletion.TrySetResult(false);
        }
    }
    private CancellationTokenSource _delayCts = new();

    private async Task SendNodeStatusAsync()
    {
        while (true)
        {
            try
            {
                if (_connection.State == HubConnectionState.Connected && _node != null)
                {
                    await _connection.SendAsync("UpdateNodeStatus", new
                    {
                        NodeUid = _nodeUid,
                        ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                        ActiveRunners = _runnerManager.GetActiveRunnerUids()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.ELog($"Error sending node status: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), _delayCts.Token);
            }
            catch (TaskCanceledException)
            {
                // Task.Delay was interrupted, continue immediately
            }
        }
    }

    /// <summary>
    /// Triggers an immediate node status update.
    /// </summary>
    public void TriggerStatusUpdate()
    {
        _delayCts.Cancel(); // Cancels the current delay
        _delayCts.Dispose();
        _delayCts = new CancellationTokenSource(); // Reset for the next delay
    }
    
    private async Task<bool> HandleClientProcessFile(RunFileArguments args)
    {
        await EnsureRegisteredAsync();
        _logger.ILog("Handling process file request...");

        try
        {
            if (_configurationService.CurrentConfig?.Revision < args.ConfigRevision)
            {
                var updateTask = _configurationService.UpdateConfiguration(args.ConfigRevision, _node);
                if (await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromSeconds(10))) != updateTask)
                {
                    _logger.WLog("Configuration update timed out.");
                    return false;
                }
                if (_configurationService.CurrentConfig?.Revision < args.ConfigRevision)
                    return false;
            }

            bool result = _runnerManager.TryStartRunner(args, _node, _configurationService.CurrentConfig!);
            _logger.ILog($"Process file result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error processing file: {ex.Message}");
            return false;
        }
    }
}