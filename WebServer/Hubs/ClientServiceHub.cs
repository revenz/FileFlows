using FileFlows.Plugin;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Controller for WebSocket communication with clients.
/// </summary>
public class ClientServiceHub : Hub
{
    /// <summary>
    /// Broadcast a message
    /// </summary>
    /// <param name="command">the command to send in the message</param>
    /// <param name="data">the data to go along with the command</param>
    public async Task BroadcastMessage(string command, string data)
    {
        await Clients.All.SendAsync("ReceiveMessage", command, data);
    }
}

/// <summary>
/// Manager for the client service hub
/// </summary>
public class ClientServiceManager : IClientService
{
    /// <summary>
    /// The hub context
    /// </summary>
    private readonly IHubContext<ClientServiceHub> _hubContext;
    
    /// <summary>
    /// Gets the static instance of the Client Service Manager
    /// </summary>
    public static ClientServiceManager Instance { get; private set; }
    
    /// <summary>
    /// Creates an instance of the Client Service Manager
    /// </summary>
    /// <param name="hubContext">the hub context</param>
    public ClientServiceManager(IHubContext<ClientServiceHub> hubContext)
    {
        _hubContext = hubContext;
        Instance = this;
    }

    /// <summary>
    /// Sends a toast to the clients
    /// </summary>
    /// <param name="type">the type of toast to show</param>
    /// <param name="message">the message of the toast</param>
    public void SendToast(LogType type, string message)
        => _hubContext.Clients.All.SendAsync("Toast", new { Type = type, Message = message });

    /// <summary>
    /// Sends a notification to the clients
    /// </summary>
    /// <param name="severity">the severity of notification</param>
    /// <param name="title">the title of the notification</param>
    public void SendNotification(NotificationSeverity severity, string title)
        => _hubContext.Clients.All.SendAsync("Notification", new { Severity = severity, Title = title });

    /// <summary>
    /// A semaphore to ensure only one update is set at a time
    /// </summary>
    private SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1);
    
    /// <summary>
    /// Update executes
    /// </summary>
    /// <param name="executors">the executors</param>
    public async Task UpdateExecutors(Dictionary<Guid, FlowExecutorInfoMinified> executors)
    {
        if (await UpdateSemaphore.WaitAsync(50) == false)
            return;

        try
        {
            await _hubContext.Clients.All.SendAsync("UpdateExecutors", executors);
            await Task.Delay(500); // creates a 500 ms delay between messages to the client
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed updating executors: " + ex.Message);
        }
        finally
        {
            UpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates the file status
    /// </summary>
    public async Task UpdateFileStatus()
    {
        var status = await ServiceLoader.Load<LibraryFileService>().GetStatus();
        await _hubContext.Clients.All.SendAsync("UpdateFileStatus", status);
    }
    
    /// <summary>
    /// Updates the status summaries of the nodes
    /// </summary>
    public async Task UpdateNodeStatusSummaries()
    {
        var status = await ServiceLoader.Load<NodeService>().GetStatusSummaries();
        await _hubContext.Clients.All.SendAsync("UpdateNodeStatusSummaries", status);
    }
    
    /// <summary>
    /// Called when a system is paused/unpaused
    /// </summary>
    /// <param name="minutes">how many minutes to pause the system for</param>
    public void SystemPaused(int minutes)
        => _hubContext.Clients.All.SendAsync("SystemPaused", minutes);
    
    /// <summary>
    /// Updates the system info
    /// </summary>
    /// <param name="info">the system info</param>
    public void UpdateSystemInfo(SystemInfo info)
        => _hubContext.Clients.All.SendAsync("SystemInfo", info);

    /// <summary>
    /// Called when a file starts processing
    /// </summary>
    /// <param name="file">the file that's starting processing</param>
    public void StartProcessing(LibraryFile file)
        => _hubContext.Clients.All.SendAsync("StartProcessing", file);
    
    /// <summary>
    /// Called when a file finish processing
    /// </summary>
    /// <param name="file">the file that's finished processing</param>
    public void FinishProcessing(LibraryFile file)
        => _hubContext.Clients.All.SendAsync("FinishProcessing", file);

    /// <summary>
    /// Sends a update to the file overview
    /// </summary>
    /// <param name="data">the update data</param>
    public void FileOverviewUpdate(FileOverviewData data)
        => _hubContext.Clients.All.SendAsync("FileOverviewUpdate", data);

    /// <summary>
    /// Sends a update about updates available to the system
    /// </summary>
    /// <param name="data">the update data</param>
    public void UpdatesUpdate(UpdateInfo data)
        => _hubContext.Clients.All.SendAsync("UpdatesUpdateInfo", data);
    
    /// <summary>
    /// Sends a update about tags available int the system
    /// </summary>
    /// <param name="tags">the tags in the system</param>
    public void TagsUpdated(List<Tag> tags)
        => _hubContext.Clients.All.SendAsync("TagsUpdated", tags);
}