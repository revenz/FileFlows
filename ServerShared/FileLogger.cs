using System.Globalization;
using System.Text;
using System.Text.Json;
using FileFlows.Plugin;
using FileFlows.Shared;
using Jint.Runtime;

namespace FileFlows.ServerShared;

/// <summary>
/// A Logger that writes its output to file
/// </summary>
public class FileLogger : ILogWriter
{
    private string LogPrefix;
    private string LoggingPath;

    private DateOnly LogDate = DateOnly.MinValue;

    private SemaphoreSlim mutex = new SemaphoreSlim(1);

    private long CurrentLogSize = 0;
    private DateOnly currentLogDate = DateOnly.MinValue;
    private string? logFile;
    private const long MaxLogSize = 10 * 1024 * 1024;

    /// <summary>
    /// Gets an instance of the FileLogger
    /// </summary>
    public static FileLogger Instance { get; private set; } = null!;

    /// <summary>
    /// Creates a file logger
    /// </summary>
    /// <param name="loggingPath">The path where to save the log file to</param>
    /// <param name="logPrefix">The prefix to use for the log file name</param>
    /// <param name="register">if this logger should be registered</param>
    public FileLogger(string loggingPath, string logPrefix, bool register = true)
    {
        this.LoggingPath = loggingPath;
        this.LogPrefix = logPrefix;
        if (register)
        {
            Shared.Logger.Instance.RegisterWriter(this);
            Instance = this;
        }
    }
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public async Task Log(LogType type, params object[] args)
    {
        string message = LogHelper.FormatMessage(type, args);
        Console.WriteLine(message);
        await LogRaw([message]);
    }
    
    /// <summary>
    /// Logs a raw message
    /// </summary>
    /// <param name="message">the messages to log</param>
    public async Task LogRaw(string[] message)
    {
        await mutex.WaitAsync();
        try
        {
            Console.WriteLine(message);
            
            string messages = string.Join("\n", message);

            long size = Encoding.UTF8.GetByteCount(messages) + 1;

            if (logFile == null || currentLogDate.DayOfYear != DateTime.Now.DayOfYear || CurrentLogSize + size > MaxLogSize)
            {
                logFile = GetLogFilename();
                currentLogDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.Exists == false)
                {
                    CurrentLogSize = 0;
                    fileInfo.Create();
                }
                else
                {
                    CurrentLogSize = fileInfo.Length;
                }
            }
            
            using var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sr = new StreamWriter(fs);
            await sr.WriteLineAsync(messages);
            await sr.FlushAsync();
                

            CurrentLogSize += size;
        }
        finally
        {
            mutex.Release();
        }
    }

    /// <summary>
    /// Gets a tail of the log
    /// </summary>
    /// <param name="length">The number of lines to fetch</param>
    /// <param name="logLevel">the log level</param>
    /// <returns>a tail of the log</returns>
    public async Task<string> GetTail(int length = 50, LogType logLevel = LogType.Info)
    {
        if (length <= 0 || length > 1000)
            length = 1000;

        await mutex.WaitAsync();
        try
        {
            return GetTailActual(length, logLevel);
        }
        finally
        {
            mutex.Release();
        }
    }

    private string GetTailActual(int length, LogType logLevel)
    {
        string logFile = GetLogFilename();
        if (string.IsNullOrEmpty(logFile) || File.Exists(logFile) == false)
            return string.Empty;
        StreamReader reader = new StreamReader(logFile);
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
    /// Gets the name of the filename to log to
    /// </summary>
    /// <returns>the name of the filename to log to</returns>
    public string GetLogFilename()
    {
        string file = Path.Combine(LoggingPath, LogPrefix + "-" + DateTime.Now.ToString("MMMdd"));
        string latestAcceptableFile = string.Empty;
        for (int i = 99; i >= 0; i--)
        {
            FileInfo fi = new(file + "-" + i.ToString("D2") + ".log");
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
