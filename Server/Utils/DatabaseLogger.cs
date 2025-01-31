// using FileFlows.Plugin;
// using FileFlows.Services;
//
// namespace FileFlows.Server.Utils;
//
// /// <summary>
// /// Log writer that writes log messages to an external database
// /// </summary>
// public class DatabaseLogger:ILogWriter
// {
//     private DatabaseLogService _service;
//     /// <summary>
//     /// Constructs an instance of the Database Logger
//     /// </summary>
//     public DatabaseLogger()
//     {
//         _service = ServiceLoader.Load<DatabaseLogService>();
//         Logger.Instance.RegisterWriter(this);
//     }
//
//     /// <summary>
//     /// Logs a message
//     /// </summary>
//     /// <param name="type">the type of log message</param>
//     /// <param name="args">the arguments for the log message</param>
//     public Task Log(LogType type, params object[] args) => Log(Guid.Empty, type, args);
//     
//     /// <summary>
//     /// Logs a message
//     /// </summary>
//     /// <param name="clientUid">The UID of the client logging this message</param>
//     /// <param name="type">the type of log message</param>
//     /// <param name="args">the arguments for the log message</param>
//     public async Task Log(Guid clientUid, LogType type, params object[] args)
//     {
//         string message = string.Join(", ", args.Select(x =>
//             x == null ? "null" :
//             x.GetType().IsPrimitive ? x.ToString() :
//             x is string ? x.ToString() :
//             JsonSerializer.Serialize(x)));
//         message = message.Replace("\\u0026", "\"");
//         message = message.Replace("\\u0027", "'");
//         message = message.Replace("\\n", "\n");
//         if (message.StartsWith("\"") && message.EndsWith("\""))
//             message = message[1..^1];
//         
//         await _service.Log(clientUid, type, message);
//     }
// }