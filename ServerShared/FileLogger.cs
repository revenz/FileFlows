using System.Globalization;
using System.Text.Json;
using System.Collections.Concurrent;

namespace FileFlows.ServerShared;

/// <summary>
/// A Logger that writes its output to file.
/// </summary>
public class FileLogger : ILogWriter
{
    /// <summary>
    /// The prefix used for the log file name.
    /// </summary>
    private string LogPrefix;

    /// <summary>
    /// The path where the log files are stored.
    /// </summary>
    private string LoggingPath;

    /// <summary>
    /// The date of the current log file.
    /// </summary>
    private DateOnly LogDate = DateOnly.MinValue;

    /// <summary>
    /// Caches the log filename to avoid repeated calculations.
    /// </summary>
    private string cachedLogFile = string.Empty; 

    /// <summary>
    /// Semaphore used to ensure thread-safety when writing to the log file.
    /// </summary>
    private readonly SemaphoreSlim semaphore = new(1);

    /// <summary>
    /// A queue to hold log messages before writing them to the log file.
    /// </summary>
    private readonly ConcurrentQueue<string> logQueue = new();

    /// <summary>
    /// Token source used to manage cancellation of the log processing task.
    /// </summary>
    private readonly CancellationTokenSource cts = new();

    /// <summary>
    /// Event used to signal when there are new items in the log queue.
    /// </summary>
    private readonly ManualResetEventSlim logEvent = new(false);

    /// <summary>
    /// Gets an instance of the FileLogger.
    /// </summary>
    public static FileLogger Instance { get; private set; } = null!;

    /// <summary>
    /// Creates a file logger.
    /// </summary>
    /// <param name="loggingPath">The path where to save the log file to.</param>
    /// <param name="logPrefix">The prefix to use for the log file name.</param>
    /// <param name="register">Indicates if this logger should be registered.</param>
    public FileLogger(string loggingPath, string logPrefix, bool register = true)
    {
        this.LoggingPath = loggingPath;
        this.LogPrefix = logPrefix;
        if (register)
        {
            Shared.Logger.Instance.RegisterWriter(this);
            Instance = this;
        }

        Task.Run(ProcessLogQueue, cts.Token);
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="type">The type of log message.</param>
    /// <param name="args">The arguments for the log message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Log(LogType type, params object[] args)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string prefix = type switch
        {
            LogType.Info => $"{date} [INFO] -> ",
            LogType.Error => $"{date} [ERRR] -> ",
            LogType.Warning => $"{date} [WARN] -> ",
            LogType.Debug => $"{date} [DBUG] -> ",
            _ => string.Empty
        };

        string text = string.Join(
            ", ", args.Select(x =>
            {
                if (x == null)
                    return "null";
                if (x.GetType().IsPrimitive)
                    return x.ToString();
                if (x is string str)
                    return str;
                if (x is JsonElement je)
                {
                    return je.ValueKind switch
                    {
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number => je.GetInt64().ToString(),
                        _ => je.ToString()
                    };
                }

                return JsonSerializer.Serialize(x);
            }));

        string message = prefix + text;
        if (message.IndexOf((char)0) >= 0)
        {
            message = message.Replace(new string((char)0, 1), string.Empty);
        }

        logQueue.Enqueue(message);

        // Signal that there's a new item in the queue
        logEvent.Set();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes the log queue.
    /// </summary>
    private async Task ProcessLogQueue()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            // Wait for the signal that the queue is not empty
            logEvent.Wait(cts.Token);  // This blocks until a log is enqueued

            // Reset the event and try dequeueing a message
            logEvent.Reset();

            if (logQueue.TryDequeue(out string? message))
            {
                string logFile = GetLogFilename();
                try
                {
                    await semaphore.WaitAsync(cts.Token);
                    if (File.Exists(logFile) == false)
                    {
                        await File.WriteAllTextAsync(logFile, message + Environment.NewLine);
                    }
                    else
                    {
                        using var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        using StreamWriter sr = new(fs);
                        await sr.WriteLineAsync(message);
                        await sr.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }

    /// <summary>
    /// Gets a tail of the log.
    /// </summary>
    /// <param name="length">The number of lines to fetch.</param>
    /// <param name="logLevel">The log level to filter by.</param>
    /// <returns>A task that returns the tail of the log.</returns>
    public async Task<string> GetTail(int length = 50, LogType logLevel = LogType.Info)
    {
        if (length <= 0 || length > 1000)
            length = 1000;

        await semaphore.WaitAsync();
        try
        {
            return GetTailActual(length, logLevel);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the actual log tail.
    /// </summary>
    /// <param name="length">The size of the tail.</param>
    /// <param name="logLevel">The types of messages to get.</param>
    /// <returns>The log tail.</returns>
    private string GetTailActual(int length, LogType logLevel)
    {
        string logFile = GetLogFilename();
        if (string.IsNullOrEmpty(logFile) || File.Exists(logFile) == false)
            return string.Empty;

        StreamReader reader = new(logFile);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        int count = 0;
        int max = length;
        if (logLevel != LogType.Debug)
            max = 5000;
        while ((count < max) && (reader.BaseStream.Position > 0))
        {
            reader.BaseStream.Position--;
            int c = reader.BaseStream.ReadByte();
            if (reader.BaseStream.Position > 0)
                reader.BaseStream.Position--;
            if (c == Convert.ToInt32('\n'))
            {
                ++count;
            }
        }

        string str = reader.ReadToEnd();
        if (logLevel == LogType.Debug)
            return str;

        string[] arr = str.Replace("\r", "").Split('\n');
        arr = arr.Where(x =>
        {
            if (logLevel < LogType.Debug && x.Contains("DBUG"))
                return false;
            if (logLevel < LogType.Info && x.Contains("INFO"))
                return false;
            if (logLevel < LogType.Warning && x.Contains("WARN"))
                return false;
            return true;
        }).Take(length).ToArray();
        reader.Close();
        return string.Join("\n", arr);
    }

    /// <summary>
    /// Gets the name of the filename to log to.
    /// </summary>
    /// <returns>The name of the filename to log to.</returns>
    public string GetLogFilename()
    {
        // Check if the date has changed or if the cached file is no longer valid
        if (cachedLogFile == string.Empty || 
            new FileInfo(cachedLogFile).Length >= 10_000_000 || 
            DateTime.Now.Date != LogDate.ToDateTime(TimeOnly.MinValue).Date)
        {
            string currentLogFile = Path.Combine(LoggingPath, LogPrefix + "-" + DateTime.Now.ToString("MMMdd"));
            // Update the cached log file and log date
            cachedLogFile = GetNewLogFile(currentLogFile);
            LogDate = DateOnly.FromDateTime(DateTime.Now);
        }
        return cachedLogFile;
    }

    /// <summary>
    /// Gets a new log file name.
    /// </summary>
    /// <param name="baseFile">The base name for the log.</param>
    /// <returns>The new log name.</returns>
    private string GetNewLogFile(string baseFile)
    {
        string latestAcceptableFile = string.Empty;
        for (int i = 99; i >= 0; i--)
        {
            FileInfo fi = new(baseFile + "-" + i.ToString("D2") + ".log");
            if (fi.Exists == false)
            {
                latestAcceptableFile = fi.FullName;
            }
            else if (fi.Length < 10_000_000)
            {
                latestAcceptableFile = fi.FullName;
                break;
            }
            else
            {
                break;
            }
        }
        return latestAcceptableFile;
    }
}