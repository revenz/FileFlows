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
    /// Gets the client
    /// </summary>
    public Client? Client => _client;
    
    // /// <summary>
    // /// Gets or sets if this node is registered
    // /// </summary>
    // public bool Registered { get; private set; }

    /// <summary>
    /// Gets the current connection state
    /// </summary>
    public ConnectionState CurrentStete { get; private set; }

    /// <summary>
    /// Event fired when connection state changes
    /// </summary>
    public event Action<ConnectionState>? OnConnectionUpdated;


    /// <summary>
    /// Gets the node manager instance
    /// </summary>
    public static NodeManager? Instance { get; private set; }

    /// <summary>
    /// Constructs a new instance of the Node Manager
    /// </summary>
    public NodeManager()
    {
        Instance = this;
    }

    /// <summary>
    /// Starts the node processing
    /// </summary>
    public void Start()
    {
        _ = StartWorkers();
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
    private async Task StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");

        StartClient();
        
        var updater = new NodeUpdater();
        
        if (await updater.RunCheck())
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
        try
        {
            StartClient();
            bool success = await _client!.Connection.AwaitConnection(20);
            return (success, success ? string.Empty : "Failed to register node");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Registers the node with the server
    /// </summary>
    /// <returns>whether it was registered</returns>
    private void StartClient()
    {
        var settings = AppSettings.Instance;
        
        if (_client != null)
        {
            _client.Connection.ConnectedUpdated -= ClientOnOnConnectionUpdated;
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

        _client.Connection.ConnectedUpdated += ClientOnOnConnectionUpdated;
        
        RemoteService.AccessToken = settings.AccessToken;

        _client.Start();
        
        RemoteService.ServiceBaseUrl = settings.ServerUrl;
        if (RemoteService.ServiceBaseUrl.EndsWith('/'))
            RemoteService.ServiceBaseUrl = RemoteService.ServiceBaseUrl[..^1];

        Logger.Instance?.ILog("Successfully registered node");
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
