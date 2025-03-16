using System.Runtime.InteropServices;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;
using ZstdSharp.Unsafe;

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
    /// <summary>
    /// Gets the node
    /// </summary>
    public ProcessingNode? Node => _node;
    public Guid? NodeUid => _node?.Uid;
    private ClientParameters _parameters;

    private readonly SemaphoreSlim _configurationSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Called when the configuration has to be updated
    /// </summary>
    /// <param name="revision">the revision on the server</param>
    private async Task UpdateConfiguration(int revision)
    {
        if (_node == null)
            return;
        const string prefix = "UpdateConfiguration:";
        _logger.ILog($"{prefix} Update Configuration to '{revision}' requested");

        if (await _configurationSemaphore.WaitAsync(20_000) == false)
        {
            _logger.ILog($"{prefix} Failed to acquire configuration update semaphore within 20 seconds.");
            return;
        }

        bool updated = false;
        try
        {

            if (_configurationService.CurrentConfig?.Revision >= revision)
            {
                _logger.ILog($"{prefix} Configuration already updated to  to '{_configurationService.CurrentConfig?.Revision}'");
                return; // already up to date
            }
            _logger.ILog($"{prefix} Updating configuration...");
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var cfg = await _connection.InvokeAsync<ConfigurationRevision>("GetConfiguration", cts2.Token);

            if (cfg != null && _configurationService.CurrentConfig?.Revision != cfg.Revision)
            {
                await _configurationService.SaveConfiguration(cfg, _node);
                _logger.ILog($"{prefix} Configuration updated");
                updated = true;
            }
        }
        catch (Exception ex)
        {
            // Ignore
            _logger.WLog($"{prefix} Failed to update: {ex}");
        }
        finally
        {
            _configurationSemaphore.Release();
        }
        
        if(updated)
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
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await UpdateConfiguration(result.CurrentConfigRevision);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.ELog($"Node registration failed: {ex.Message}");
            _registrationCompletion.TrySetResult(false);
        }
    }

    /// <summary>
    /// Aborts a file
    /// </summary>
    /// <param name="uid">the UID of the file to abort</param>
    private async Task AbortFile(Guid uid)
    {
        await _runnerManager.AbortRunner(uid);
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
                        ActiveRunners = _runnerManager.GetActiveRunnerUids(),
                        NodeVersion = Globals.Version,
                        InstallingDockerMods = _InstallingDockerMods
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
                _logger.WLog(
                    $"Configuration is out of date {_configurationService.CurrentConfig?.Revision}', needs updating: {args.ConfigRevision}");
                var updateTask = _configurationService.UpdateConfiguration(args.ConfigRevision, _node);
                if (await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromSeconds(10))) != updateTask)
                {
                    _logger.WLog("Configuration update timed out.");
                    return false;
                }

                if (_configurationService.CurrentConfig?.Revision < args.ConfigRevision)
                {
                    _logger.WLog(
                        $"Configuration failed to update {_configurationService.CurrentConfig?.Revision}', : {args.ConfigRevision}");

                    return false;
                }
            }

            _logger.ILog($"Trying to start runner for: {args.LibraryFile.Name}");
            bool result = _runnerManager.TryStartRunner(this, args, _node, _configurationService.CurrentConfig!);
            _logger.ILog($"Process file result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error processing file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the file exists on the server
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>true if exists otherwise false</returns>
    public async Task<bool> ExistsOnServer(Guid uid)
        => await _connection.InvokeAsync<bool>("ExistsOnServer", uid);

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="libraryFileUid">the UID of the file</param>
    public async Task FileStartProcessing(Guid libraryFileUid)
        => await _connection.SendAsync("FileStartProcessing", libraryFileUid);

    /// <summary>
    /// Called when the file finishes processing
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <param name="log">the complete log of the file processing</param>
    public async Task FileFinishProcessing(LibraryFile libraryFile, string log)
        => await _connection.SendAsync("FileFinishProcessing", libraryFile, log);

    /// <summary>
    /// Prepends the text to the log file on the server
    /// </summary>
    /// <param name="libFileUid">the UID of the file</param>
    /// <param name="lines">the lines of the log</param>
    /// <param name="overwrite">if the file should be overwritten or appended to</param>
    public async Task FileLogAppend(Guid libFileUid, string lines, bool overwrite = false)
        => await _connection.SendAsync(nameof(FileLogAppend), libFileUid, lines, overwrite);
}