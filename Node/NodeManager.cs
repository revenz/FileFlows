using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileFlows.Node.Ui;
using FileFlows.NodeClient;
using FileFlows.RemoteServices;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Node;

/// <summary>
/// A manager that handles registering a node with the FileFlows server
/// </summary>
public class NodeManager
{
    /// <summary>
    /// The node client
    /// </summary>
    private Client? _client;
    
    /// <summary>
    /// Gets or sets if this node is registered
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Gets the current connection state
    /// </summary>
    public ConnectionState CurrentStete { get; private set; }

    /// <summary>
    /// Event fired when connection state changes
    /// </summary>
    public event Client.ConnectionUpdated? OnConnectionUpdated;

    /// <summary>
    /// Starts the node processing
    /// </summary>
    public void Start()
    {
        StartWorkers();
    }

    /// <summary>
    /// Stops the node processing
    /// </summary>
    public void Stop()
    {
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Starts the node workers
    /// </summary>
    private void StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");

        _ = Register();
        
        var updater = new NodeUpdater();
        
        if (updater.RunCheck())
            return;

        
        WorkerManager.StartWorkers(
            updater, 
            new LogFileCleaner(),
            new TempFileCleaner(AppSettings.Instance.HostName), 
            new ConfigCleaner()
        );
    }

    /// <summary>
    /// Registers the node with the server
    /// </summary>
    /// <returns>whether it was registered</returns>
    public async Task<(bool Success, string Message)> Register()
    {
        var settings = AppSettings.Instance;
        if (string.IsNullOrEmpty(settings.ServerUrl))
            return (false, "Server URL not set");
        
        if (_client != null)
        {
            _client.OnConnectionUpdated -= ClientOnOnConnectionUpdated;
            _client.Dispose();
            _client = null; // Ensure a fresh instance is created
        }

        _client = new(new()
        {
            ServerUrl = AppSettings.Instance.ServerUrl,
            Hostname = AppSettings.Instance.HostName,
            AccessToken = AppSettings.Instance.AccessToken,
            ForcedTempPath = AppSettings.ForcedTempPath,
            EnvironmentalMappings = AppSettings.EnvironmentalMappings
        }, Logger.Instance!);

        _client.OnConnectionUpdated += ClientOnOnConnectionUpdated;
        
        RemoteService.AccessToken = settings.AccessToken;
        
        try
        {
            await _client.StartAsync();
            if(_client.IsRegistered == false)
                return (false, "Failed to register");
        }
        catch (TaskCanceledException ex)
        {
            Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            return (false, "Connection timed out. Check network and address.");
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            if(ex.Message.StartsWith("A task was canceled"))
                return (false, "Connection timed out. Check network and address.");
            return (false, ex.Message);
        }

        if(_client.NodeUid! != CommonVariables.InternalNodeUid) // internal node uid is already set elsewhere to a unique UID for security
            RemoteService.NodeUid = _client.NodeUid!.Value;
        
        RemoteService.ServiceBaseUrl = settings.ServerUrl;
        if (RemoteService.ServiceBaseUrl.EndsWith('/'))
            RemoteService.ServiceBaseUrl = RemoteService.ServiceBaseUrl[..^1];

        Logger.Instance?.ILog("Successfully registered node");

        settings.Save();
        this.Registered = true;
        return (true, string.Empty);
    }

    /// <summary>
    /// Called when the state changes
    /// </summary>
    /// <param name="state">the new state</param>
    private void ClientOnOnConnectionUpdated(ConnectionState state)
    {
        CurrentStete = state;
        OnConnectionUpdated?.Invoke(state);
    }
}
