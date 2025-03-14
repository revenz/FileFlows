using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Plugin;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;

namespace FileFlows.Client.Services;

/// <summary>
/// Service for connecting to the SignalR server and handling incoming messages and commands.
/// </summary>
public partial class ClientService
{
    /// <summary>
    /// The SignalR hub connection.
    /// </summary>
    private HubConnection _hubConnection;

    /// <summary>
    /// Event raised when the client is connected to the SignalR server.
    /// </summary>
    public event Action Connected;

    /// <summary>
    /// Event raised when the client is disconnected from the SignalR server.
    /// </summary>
    public event Action Disconnected;

    /// <summary>
    /// Event raised when the executors have bene updated
    /// </summary>
    public event Action<List<FlowExecutorInfoMinified>> ExecutorsUpdated;
    /// <summary>
    /// Event raised when the system info has bene updated
    /// </summary>
    public event Action<SystemInfo> SystemInfoUpdated;
    /// <summary>
    /// Events raised when the node status summaries are updated
    /// </summary>
    public event Action<List<NodeStatusSummary>> NodeStatusSummaryUpdated;
    /// <summary>
    /// Event raised when the system is paused/unpaused
    /// </summary>
    public event Action<bool> SystemPausedUpdated;

    /// <summary>
    /// Event raised when the file overview has bene updated
    /// </summary>
    public event Action<FileOverviewData> FileOverviewUpdated;
    
    /// <summary>
    /// Event raised when the file overview has bene updated
    /// </summary>
    public event Action<UpdateInfo> UpdatesUpdateInfo;
    
    /// <summary>
    /// Event raised when the file status have bene updated
    /// </summary>
    public event Action<List<LibraryStatus>> FileStatusUpdated;
    
    /// <summary>
    /// Gets the current system info
    /// </summary>
    private SystemInfo? CurrentSystemInfo { get; set; }
    /// <summary>
    /// Gets or sets the tags in the system
    /// </summary>
    private List<Tag> Tags { get; set; }
    /// <summary>
    /// Gets the current node status summaries
    /// </summary>
    private List<NodeStatusSummary>? CurrentNodeStatusSummaries { get; set; }
    /// <summary>
    /// Gets the current file overview data
    /// </summary>
    private FileOverviewData? CurrentFileOverData { get; set; }
    
    /// <summary>
    /// Gets or sets the current update info
    /// </summary>
    private UpdateInfo CurrentUpdatesInfo { get; set; }
    /// <summary>
    /// Gets or sets the current executor info
    /// </summary>
    public List<FlowExecutorInfoMinified>? CurrentExecutorInfoMinified { get; set; }


    /// <summary>
    /// Starts the client service asynchronously.
    /// </summary>
    public async Task StartAsync()
    {
        await ConnectAsync();
    }

    /// <summary>
    /// Connects to the SignalR server.
    /// </summary>
    private async Task ConnectAsync()
    {
        while (true) // Retry indefinitely
        {
            string url = ServerUri;
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .Build();

                _hubConnection.Closed += async (exception) =>
                {
                    Disconnected?.Invoke();
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Delay before reconnecting
                    await ConnectAsync();
                };

                _hubConnection.On<ToastData>("Toast", HandleToast);
                _hubConnection.On<Dictionary<Guid, FlowExecutorInfoMinified>>("UpdateExecutors", (executors) =>
                {
                    CurrentExecutorInfoMinified = executors?.Values?.ToList() ?? [];
                    UpdateExecutors(executors);
                });
                _hubConnection.On<List<LibraryStatus>>("UpdateFileStatus", UpdateFileStatus);
                _hubConnection.On<LibraryFile>("StartProcessing", StartProcessing);
                _hubConnection.On<LibraryFile>("FinishProcessing", FinishProcessing);
                _hubConnection.On<SystemInfo>("SystemInfo", (info) =>
                {
                    CurrentSystemInfo = info;
                    SystemInfoUpdated?.Invoke(info);
                });
                _hubConnection.On<List<NodeStatusSummary>>("UpdateNodeStatusSummaries", (info) =>
                {
                    CurrentNodeStatusSummaries = info;
                    NodeStatusSummaryUpdated?.Invoke(info);
                });
                _hubConnection.On<FileOverviewData>("FileOverviewUpdate", (data) =>
                {
                    CurrentFileOverData = data;
                    _cacheService.Clear("LibraryFilesAllData");
                    _cacheService.Clear("LibraryFilesMonthData");
                    FileOverviewUpdated?.Invoke(data);
                });
                _hubConnection.On<UpdateInfo>("UpdatesUpdateInfo", (data) =>
                {
                    CurrentUpdatesInfo = data;
                    UpdatesUpdateInfo?.Invoke(data);
                });
                _hubConnection.On<List<Tag>>("TagsUpdated", (data) =>
                {
                    Tags = data;
                });
                
                _hubConnection.On<int>("SystemPaused", UpdateSystemPaused);
                _hubConnection.On<NotificationData>("Notification", HandleNotification);

                await _hubConnection.StartAsync();

                Connected?.Invoke();

                return; // Connected successfully, exit the method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the SignalR server: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5)); // Delay before reconnecting
            }
        }
    }

    /// <summary>
    /// Handles the toast data received from the SignalR server.
    /// </summary>
    /// <param name="data">The toast data.</param>
    private void HandleToast(ToastData data)
    {
        switch (data.Type)
        {
            case LogType.Info:
                Toast.ShowInfo(data.Message);
                break;
            case LogType.Debug:
                Toast.ShowSuccess(data.Message);
                break;
            case LogType.Warning:
                Toast.ShowWarning(data.Message);
                break;
            case LogType.Error:
                Toast.ShowError(data.Message);
                break;
        }
    }

    /// <summary>
    /// Handles the notification data received from the SignalR server.
    /// </summary>
    /// <param name="data">The notification data.</param>
    private void HandleNotification(NotificationData data)
    {
        // _ = _profileService.Refresh();
        switch (data.Severity)
        {
            case NotificationSeverity.Critical:
            case NotificationSeverity.Error: 
                Toast.ShowError(data.Title);
                break;
            case NotificationSeverity.Warning: 
                Toast.ShowWarning(data.Title);
                break;
            case NotificationSeverity.Information: 
                Toast.ShowInfo(data.Title);
                break;
        }
    }

    /// <summary>
    /// Called when the executors have changed
    /// </summary>
    /// <param name="executors">the executors</param>
    private void UpdateExecutors(Dictionary<Guid, FlowExecutorInfoMinified> executors)
    {   
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
        cacheEntryOptions.SetPriority(CacheItemPriority.High);
        cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        _cache.Set("FlowExecutorInfo", executors.Values.ToList(), cacheEntryOptions);

        var list = executors.Values.ToList();
        ExecutorsUpdated?.Invoke(list);
        FireJsEvent("UpdateExecutors", list);
    }

    private void UpdateFileStatus(List<LibraryStatus> data)
    {
        FileStatusUpdated?.Invoke(data);
    }
    
    /// <summary>
    /// Called when the system is paused/unpaused
    /// </summary>
    /// <param name="minutes">how many minutes to pause the system for</param>
    private void UpdateSystemPaused(int minutes)
    {
        SetPausedFor(minutes);
    }
    
    /// <summary>
    /// Called when a file starts processing
    /// </summary>
    /// <param name="file">the file</param>
    private void StartProcessing(LibraryFile file)
        => FireJsEvent("StartProcessing", file);
    
    /// <summary>
    /// Called when a file is finished processing
    /// </summary>
    /// <param name="file">the file</param>
    private void FinishProcessing(LibraryFile file)
        => FireJsEvent("FinishProcessing", file);
        
    
    /// <summary>
    /// Represents the toast data received from the SignalR server.
    /// </summary>
    private class ToastData
    {
        /// <summary>
        /// Gets or sets the type of the toast.
        /// </summary>
        public LogType Type { get; set; }

        /// <summary>
        /// Gets or sets the toast message.
        /// </summary>
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Represents the Notification data received from the SignalR server.
    /// </summary>
    private class NotificationData
    {
        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        public NotificationSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title { get; set; }
    }
}
