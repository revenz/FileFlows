using System.Runtime.InteropServices;
using System.Text.Json;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using FileFlows.Shared.Models.SignalAre;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using NPoco.fastJSON;
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
    /// <summary>
    /// Gets the runner manager
    /// </summary>
    public RunnerManager Manager => _runnerManager;
    private ProcessingNode? _node;
    /// <summary>
    /// Gets the node
    /// </summary>
    public ProcessingNode? Node => _node;
    public Guid? NodeUid => _node?.Uid;
    private ClientParameters _parameters;
    private bool _updatingConfiguration = false;

    private readonly SemaphoreSlim _configurationSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Called when the configuration has to be updated
    /// </summary>
    /// <param name="revision">the revision on the server</param>
    private async Task UpdateConfiguration(int revision)
    {
        if (_node == null || _node.Enabled == false)
            return;
        const string prefix = "UpdateConfiguration:";
        _logger.ILog($"{prefix} Update Configuration to '{revision}' requested");

        if (_configurationService.CurrentConfig?.Revision >= revision)
        {
            _logger.ILog($"{prefix} Configuration already updated to '{_configurationService.CurrentConfig?.Revision}'");
            return; // already up to date
        }

        await UpdateConfiguration();
    }
    
    /// <summary>
    /// Called when the configuration has to be updated
    /// </summary>
    public async Task UpdateConfiguration()
    {
        if (_updatingConfiguration)
            return;
        
        const string prefix = "UpdateConfiguration:";
        _logger.ILog($"{prefix} Updating configuration");
        if (_node == null)
        {
            _logger.ILog($"{prefix} Updating configuration: Node is null");
            return;
        }

        if (await _configurationSemaphore.WaitAsync(1_000) == false)
        {
            _logger.ILog($"{prefix} Failed to acquire configuration update semaphore, already updating.");
            return;
        }
        try
        {
            _updatingConfiguration = true;
            TriggerStatusUpdate();
            _logger.ILog($"{prefix} Updating configuration...");
            using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var cfg = await _connection.InvokeAsync<ConfigurationRevision>("GetConfiguration", cts2.Token);

            if (cfg != null && _configurationService.CurrentConfig?.Revision != cfg.Revision)
            {
                await _configurationService.SaveConfiguration(cfg, _node);
                _logger.ILog($"{prefix} Configuration updated");
            }
        }
        catch (Exception ex)
        {
            // Ignore
            _logger.WLog($"{prefix} Failed to update: {ex}");
        }
        finally
        {
            _updatingConfiguration = false;
            _configurationSemaphore.Release();
        }
        
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
                ActiveRunners = _runnerManager?.ActiveRunners?.Keys?.ToArray() ?? []  
            };

            if (_connection.State == HubConnectionState.Disconnected)
            {
                _registrationCompletion.TrySetResult(false);
                return;
            }

            _logger.ILog("About to call RegisterNode on server: " + JsonSerializer.Serialize(parameters));
            var result = await _connection.InvokeAsync<NodeRegisterResult>("RegisterNode", parameters);
            _logger.ILog("RegisterNode result: " + result.Success);
            
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
            else if (_node.Enabled && result.CurrentConfigRevision != _configurationService.CurrentConfig?.Revision)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await UpdateConfiguration(result.CurrentConfigRevision);
                });
            }

            TriggerLogSync();
        }
        catch (Exception ex)
        {
            _logger.ELog($"Node registration failed: {ex}");
            _registrationCompletion.TrySetResult(false);
        }
    }

    /// <summary>
    /// Aborts a file
    /// </summary>
    /// <param name="uid">the UID of the file to abort</param>
    /// <returns>true if the runner was requested to cancel</returns>
    private async Task<bool> AbortFile(Guid uid)
        => await _runnerManager.AbortRunner(uid);
    
    private CancellationTokenSource _delayCts = new();
    private CancellationTokenSource _logSyncCts = new();

    /// <summary>
    /// Sends a update to the server about this node
    /// </summary>
    private async Task SendNodeStatusAsync()
    {
        _ = SyncLog();
        while (true)
        {
            try
            {
                if (_connection.State == HubConnectionState.Connected && _node != null)
                {
                    var info = new
                    {
                        Name = _node.Name?.EmptyAsNull() ?? _hostname, 
                        NodeUid = _nodeUid,
                        ConfigRevision = _configurationService.CurrentConfig?.Revision ?? 0,
                        NodeVersion = Globals.Version,
                        UpdatingConfiguration = _updatingConfiguration,
                        InstallingDockerMods = _InstallingDockerMods,
                        InstallingDockerMod = _InstallingDockerMods ? _InstallingDockerMod?.EmptyAsNull() : null,
                        Runners = _runnerManager.ActiveRunners
                            .Where(x =>
                                x.Value.FinishedProcessing == false &&
                                // x.Value.IsRunning && 
                                x.Value.Info.LibraryFile != null)
                            .ToDictionary(x => x.Key, x => x.Value.Info),
                    };
                    var result = await _connection.InvokeAsync<NodeStatusUpdateResult>("UpdateNodeStatus", info);

                    if (result == NodeStatusUpdateResult.UpdateConfiguration)
                    {
                        _logger.ILog($"Configuration out of date for {(_node?.Name?.EmptyAsNull() ?? _hostname)}");
                        _ =  UpdateConfiguration(); // do not await this, as this could cause the node to stop sending updates
                    }
                    _logger.DLog("Node Status Update Result: " + result);
                }
                else if(_node != null)
                {
                    _logger.WLog("Cannot send not status update node is not connected to server");
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
    /// Syncs the node log with the server
    /// </summary>
    private async Task SyncLog()
    {
        while (true)
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected || _node == null)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _logSyncCts.Token);
                    continue;
                }

                if (_node.Uid == CommonVariables.InternalNodeUid)
                    return; // we dont sync this log

                if (Logger.Instance.TryGetLogger(out FileLogger logger))
                {
                    var file = logger.GetLogFilename();
                    if (File.Exists(file))
                    {
                        var log = await File.ReadAllTextAsync(file);
                        if(string.IsNullOrWhiteSpace(log) == false)
                        {
                            var compressed = Gzipper.CompressToBytes(log);
                            await _connection.InvokeAsync("SyncLog", _node.Uid, compressed);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(60), _logSyncCts.Token);
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
        try
        {
            _delayCts.Cancel(); // Cancels the current delay
            _delayCts.Dispose();
            _delayCts = new CancellationTokenSource(); // Reset for the next delay
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    
    /// <summary>
    /// Triggers an immediate node status update.
    /// </summary>
    private void TriggerLogSync()
    {
        try
        {
            _logSyncCts.Cancel(); // Cancels the current delay
            _logSyncCts.Dispose();
            _logSyncCts = new CancellationTokenSource(); // Reset for the next delay
        }
        catch (Exception)
        {
            // Ignored
        }
    }
    
    private async Task<FileCheckResult> HandleClientProcessFile(RunFileArguments args)
    {
        if (await EnsureRegisteredAsync() == false)
            return FileCheckResult.CannotProcessLibrary;
        
        //_logger.ILog("Handling process file request...");
        try
        {
            //_logger.ILog($"Trying to start runner for: {args.LibraryFile.Name}");
            var result = await _runnerManager.TryStartRunner(this, args, _node, _configurationService.CurrentConfig!);
            //_logger.ILog($"Process file result: {result}");
            if (result == FileCheckResult.CanProcess)
                TriggerStatusUpdate();
            else if (_runnerManager.ActiveRunners.ContainsKey(args.LibraryFile.Uid))
                _runnerManager.ActiveRunners.Remove(args.LibraryFile.Uid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.ELog($"Error processing file: {ex.Message}");
            return FileCheckResult.UnknownError;
        }
    }

    /// <summary>
    /// Starts processing a file
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <returns>true if could start processing</returns>
    public async Task<bool> FileStartProcessing(LibraryFile libraryFile)
    {
        int count = 0;
        while (++count < 60)
        {
            try
            {
                return await _connection.InvokeAsync<bool>("FileStartProcessing", libraryFile, new ObjectReference()
                {
                    Uid = _nodeUid,
                    Name = _node!.Name,
                    Type = _node.GetType().FullName!
                });
            }
            catch (Exception ex)
            {
                _logger.WLog($"Failed to notify file '{libraryFile.RelativePath}' started processing[{count}]: {ex.Message}");   
            }
            await Task.Delay(500);
        }

        return false;
    }
    
    /// <summary>
    /// Called when the file finishes processing
    /// </summary>
    /// <param name="libraryFile">the library file</param>
    /// <param name="log">the complete log of the file processing</param>
    public async Task FileFinishProcessing(LibraryFile libraryFile, string log)
    {
        int count = 0;
        while (++count < 10)
        {
            try
            {
                if (await AwaitConnection() == false)
                {
                    _logger.WLog("Failed to connect to notify file finished. Attempt: " + count);
                    continue;
                }

                var result = await _connection.InvokeAsync<bool>("FileFinishProcessing", libraryFile, log);
                if (result)
                    return;
            }
            catch (Exception ex)
            {
                _logger.WLog($"Failed to notify file '{libraryFile.RelativePath}' finished processing[{count}]: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Prepends the text to the log file on the server
    /// </summary>
    /// <param name="libFileUid">the UID of the file</param>
    /// <param name="lines">the lines of the log</param>
    /// <param name="overwrite">if the file should be overwritten or appended to</param>
    public async Task FileLogAppend(Guid libFileUid, string lines, bool overwrite = false)
        => await _connection.SendAsync(nameof(FileLogAppend), libFileUid, lines, overwrite);

    /// <summary>
    /// Sends a log message to the server
    /// </summary>
    /// <param name="messages">the messages being logged</param>
    public async Task Log(string[] messages)
        => await _connection.SendAsync(nameof(Log),
            _node?.Address?.EmptyAsNull() ?? _node?.Name?.EmptyAsNull() ?? Environment.MachineName, messages);
    
    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the server method.</typeparam>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <typeparamref name="TResult"/> for the hub method return value.
    /// </returns>
    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object?[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            if (_disposed)
                return default!;
            
            if(await AwaitConnection(30) == false)
                continue;
            try
            {
                TResult result;
                switch (args.Length)
                {
                    case 0: result = await _connection.InvokeAsync<TResult>(methodName); break;
                    case 1: result = await _connection.InvokeAsync<TResult>(methodName, args[0]); break;
                    case 2: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1]); break;
                    case 3: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2]); break;
                    case 4: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3]); break;
                    case 5: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4]); break;
                    case 6: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4], args[5]); break;
                    case 7: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); break;
                    case 8: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]); break;
                    case 9: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]); break;
                    case 10: result = await _connection.InvokeAsync<TResult>(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(args), "Too many arguments provided.");
                }
                return result;
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                if (_connection.State == HubConnectionState.Connected)
                    throw;
            }
        }
        _logger.ELog("Failed to invoke method on server as no connection could be established.");
        throw new Exception($"Failed to invoke method on server as no connection established.");
    }
    
    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    public async Task SendAsync( string methodName, params object?[] args)
    {
        for (int i = 0; i < 10; i++)
        {
            if (_disposed)
                return;
            
            if(await AwaitConnection(30) == false)
                continue;
            try
            {
                switch (args.Length)
                {
                    case 0: await _connection.SendAsync(methodName); break;
                    case 1: await _connection.SendAsync(methodName, args[0]); break;
                    case 2: await _connection.SendAsync(methodName, args[0], args[1]); break;
                    case 3: await _connection.SendAsync(methodName, args[0], args[1], args[2]); break;
                    case 4: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3]); break;
                    case 5: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4]); break;
                    case 6: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5]); break;
                    case 7: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); break;
                    case 8: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]); break;
                    case 9: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]); break;
                    case 10: await _connection.SendAsync(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(args), "Too many arguments provided.");
                }
                return;
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                if (_connection.State == HubConnectionState.Connected)
                    throw;
            }
        }
        _logger.ELog("Failed to sending method on server as no connection could be established.");
        throw new Exception($"Failed to sending method on server as no connection established.");
    }
    

    // in handlers
    // /// <summary>
    // /// Sets a thumbnail for a file
    // /// </summary>
    // /// <param name="libraryFileUid">the UID of the library file</param>
    // /// <param name="binaryData">the binary data for the thumbnail</param>
    // /// <returns>a completed task</returns>
    // public async Task SetThumbnail(Guid libraryFileUid, byte[] binaryData)
    //     => await _connection.SendAsync(nameof(SetThumbnail), libraryFileUid, binaryData);
    //
    // /// <summary>
    // /// Checks if the file exists on the server
    // /// </summary>
    // /// <param name="path">The file path</param>
    // /// <param name="isDirectory">if it is a directory</param>
    // /// <returns>true if exists, otherwise false</returns>
    // public async Task<bool> ExistsOnServer(string path, bool isDirectory)
    // {
    //     try
    //     {
    //         return await _connection.InvokeAsync<bool>(nameof(ExistsOnServer), path, isDirectory);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.WLog($"Failed checking file exists on server: {ex.Message}");
    //         return true; // assume it exists on the server
    //     }
    // }
}