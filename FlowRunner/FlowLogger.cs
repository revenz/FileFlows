using System.Globalization;
using FileFlows.FlowRunner.JsonRpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner;

/// <summary>
/// Logger specifically for the Flow execution
/// </summary>
public class FlowLogger : ILogger
{
    /// <inheritdoc />
    public void Raw(params object[] args) => Log(LogType.Raw, args);
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ELog(params object[] args) => Log(LogType.Error, args);

    /// <summary>
    /// The communicator used to send messages to the server
    /// </summary>
    JsonRpcClient _rpcClient;
    
    /// <summary>
    /// Constructs a new instance of the flow logger
    /// </summary>
    /// <param name="rpcClient">a JSON RPC client</param>
    public FlowLogger(JsonRpcClient rpcClient)
    {
        this._rpcClient = rpcClient;
    }

    /// <summary>
    /// Logs an image
    /// </summary>
    /// <param name="path">the path to the image</param>
    public void Image(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path) == false)
            return;
        try
        {
            // Convert the image to base64 encoded JPEG
            string base64Image;
            // Load the image
            using (var image = SixLabors.ImageSharp.Image.Load(path))
            {
                // Downscale the image while preserving aspect ratio to fit within 640x480
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new(640, 480)
                }));

                using (MemoryStream stream = new MemoryStream())
                {
                    image.SaveAsJpeg(stream);
                    byte[] bytes = stream.ToArray();
                    base64Image = Convert.ToBase64String(bytes);
                }
            }

            string message = "data:image/jpeg;base64," + base64Image + ":640x480";
            
            if(_rpcClient.BasicHandler != null)
                _rpcClient.BasicHandler.LogMessage(message).Wait();
            else
                Console.WriteLine(message);

            //_ = Flush();
        }
        catch (Exception ex)
        {
            WLog($"Failed logging image '{path}': {ex.Message}");
        }
    }

    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void ILog(params object[] args) => Log(LogType.Info, args);
    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    public void WLog(params object[] args) => Log(LogType.Warning, args);

    /// <summary>
    /// Gets or sets the library file this flow is executing
    /// </summary>
    public LibraryFile File { get; set; }
    
    /// <summary>
    /// The type of log message
    /// </summary>
    private enum LogType
    {
        /// <summary>
        /// A error message
        /// </summary>
        Error, 
        /// <summary>
        /// a warning message
        /// </summary>
        Warning,
        /// <summary>
        /// A informational message
        /// </summary>
        Info,
        /// <summary>
        /// A debug message
        /// </summary>
        Debug,
        /// <summary>
        /// A log message with no prefix
        /// </summary>
        Raw
    }

    //private StringBuilder Messages = new StringBuilder();

    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of message to log</param>
    /// <param name="args">the log message arguments</param>
    private void Log(LogType type, params object[] args)
    {
        if (args == null || args.Length == 0)
            return;
        string prefix;

        if (type == LogType.Raw)
        {
            prefix = string.Empty;
        }
        else
        {
            prefix = type switch
            {
                LogType.Info => "INFO",
                LogType.Error => "ERRR",
                LogType.Warning => "WARN",
                LogType.Debug => "DBUG",
                _ => ""
            };
            prefix = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [" + prefix +
                     "] -> ";
        }

        string message = prefix +
                         string.Join(", ", args.Select(x =>
                             x == null ? "null" :
                             x.GetType().IsPrimitive || x is string ? x.ToString() :
                             JsonSerializer.Serialize(x)));
        //log.Add(message);
        Console.WriteLine(message);
        if(_rpcClient.BasicHandler != null)
            _rpcClient.BasicHandler.LogMessage(message).Wait();
    }
    /// <summary>
    /// Gets the last number of log lines
    /// </summary>
    /// <param name="length">The maximum number of lines to grab</param>
    /// <returns>The last number of log lines</returns>
    public string GetTail(int length = 50)
        => string.Empty;
}
