using System.Globalization;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Text;

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
    /// Gets an instance of the FileLogger.
    /// </summary>
    public static FileLogger Instance { get; private set; } = null!;
    
    private readonly ConcurrentQueue<(string, DateTime)> logMessages = new();
    private readonly Timer logFlushTimer;
    private long currentFileSize = 0;
    private const int MaxFileSize = 10 * 1024 * 1024; // 10MB limit
    private const int MaxFileSizeMargin = 2 * 1024; // Allow a margin of 2KB over 10MB


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
        // Flush logs every 5 seconds
        logFlushTimer = new Timer(FlushLogBatch!, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    private async void FlushLogBatch(object state)
    {
        // Collect logs that need to be written
        var logsToWrite = new List<(string message, DateTime timestamp)>();
        while (logMessages.TryDequeue(out var logMessage))
        {
            logsToWrite.Add(logMessage);
        }

        if (logsToWrite.Count == 0)
            return;

        // Determine the proper file for today
        var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var logFileName = $"log_{dateString}.txt";

        // Create or append to the log file
        string logFilePath = Path.Combine("Logs", logFileName);
        var logFileSize = currentFileSize;

        try
        {
            // Check if file exists and determine current size
            if (File.Exists(logFilePath))
            {
                var fileInfo = new FileInfo(logFilePath);
                logFileSize = fileInfo.Length;
            }

            // If the current file size exceeds the limit, create a new file for the next day
            if (logFileSize > MaxFileSize - MaxFileSizeMargin)
            {
                // New day file, or next file based on the date
                dateString = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
                logFileName = $"log_{dateString}.txt";
                logFilePath = Path.Combine("Logs", logFileName);
                currentFileSize = 0; // Reset file size for the new log file
            }

            // Append messages to the log file
            await using var fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            await using var writer = new StreamWriter(fs, Encoding.UTF8);
            foreach (var log in logsToWrite)
            {
                await writer.WriteLineAsync(log.message);
                currentFileSize += Encoding.UTF8.GetByteCount(log.message + "\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while writing to log file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="type">The type of log message.</param>
    /// <param name="args">The arguments for the log message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task Log(LogType type, params object[] args)
    {
        var dateValue = DateTime.Now;
        string date = dateValue.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
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
        logMessages.Enqueue((message, dateValue));
        
        return Task.CompletedTask;
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
    /// <param name="date">optional date to use</param>
    /// <returns>The name of the filename to log to.</returns>
    public string GetLogFilename(DateTime? date  = null)
    {
        date ??= DateTime.Now;
        // Check if the date has changed or if the cached file is no longer valid
        if (cachedLogFile == string.Empty || 
            new FileInfo(cachedLogFile).Length >= 10_000_000 || 
            date != LogDate.ToDateTime(TimeOnly.MinValue).Date)
        {
            string currentLogFile = Path.Combine(LoggingPath, LogPrefix + "-" + date.Value.ToString("MMMdd"));
            // Update the cached log file and log date
            cachedLogFile = GetNewLogFile(currentLogFile);
            LogDate = DateOnly.FromDateTime(date.Value);
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