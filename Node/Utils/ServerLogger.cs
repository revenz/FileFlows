using System.Collections.Concurrent;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.Node.Utils;

/// <summary>
/// Log writer that sends log messages to the FileFlows server in batches.
/// Logs are queued and sent either every 20 seconds or when at least 20 messages accumulate.
/// The queue is capped at 1000 messages, dropping older messages when necessary.
/// </summary>
public class ServerLogger : ILogWriter, IDisposable
{
    private ConcurrentQueue<string> _LogQueue = new ConcurrentQueue<string>();
    private SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _Cts = new CancellationTokenSource();
    private Task _BackgroundTask;


    /// <summary>
    /// Initializes a new instance of the <see cref="ServerLogger"/> class and starts the background processing task.
    /// </summary>
    public ServerLogger()
    {
        Logger.Instance.RegisterWriter(this);
        _BackgroundTask = Task.Run(ProcessQueue);
    }

    /// <summary>
    /// Logs a message to the queue.
    /// </summary>
    /// <param name="type">The type of log message.</param>
    /// <param name="args">The arguments used to format the log message.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task Log(LogType type, params object[] args)
    {
        if (string.IsNullOrEmpty(AppSettings.Instance.ServerUrl))
            return Task.CompletedTask; // Not registered

        string message = LogHelper.FormatMessage(type, args);
        _LogQueue.Enqueue(message);

        // Trim old messages if the queue exceeds 1000 entries
        while (_LogQueue.Count > 1000)
            _LogQueue.TryDequeue(out _);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes the log queue in the background, sending messages in batches.
    /// Runs every 20 seconds unless the queue reaches 20 messages, in which case it sends immediately.
    /// </summary>
    private async Task ProcessQueue()
    {
        while (!_Cts.Token.IsCancellationRequested)
        {
            if (_LogQueue.Count >= 20)
            {
                await PushMessages();
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(20), _Cts.Token);
                if (_LogQueue.IsEmpty == false)
                {
                    await PushMessages();
                }
            }
        }
    }

    /// <summary>
    /// Sends queued log messages to the FileFlows server in batches of up to 20 messages.
    /// Ensures that no log messages are sent if Manager.Client is null.
    /// </summary>
    private async Task PushMessages()
    {
        if (NodeManager.Instance?.Client == null)
            return;

        if (!_LogQueue.IsEmpty)
        {
            await _Semaphore.WaitAsync();
            try
            {
                List<string> messages = new ();
                while (messages.Count < 20 && _LogQueue.TryDequeue(out var msg))
                    messages.Add(msg);

                if (messages.Count > 0)
                    await NodeManager.Instance.Client.Log(messages.ToArray());
            }
            finally
            {
                _Semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Disposes of the logger, stopping the background processing task and releasing resources.
    /// </summary>
    public void Dispose()
    {
        _Cts.Cancel();
        try
        {
            _BackgroundTask?.Wait();
        }
        catch
        {
            // Ignored
        }
        finally
        {
            _Cts.Dispose();
            _Semaphore.Dispose();
        }
    }
}
