// using FileFlows.LibraryUtils;
// using Microsoft.AspNetCore.SignalR;
//
// namespace FileFlows.WebServer.Hubs;
//
// /// <summary>
// /// Signalr Hub for executing flows
// /// </summary>
// public class FlowHub : Hub
// {
//     /// <inheritdoc />
//     public override Task OnConnectedAsync()
//     {
//         Logger.Instance.ILog($"FlowHub Client connected: {Context.ConnectionId}");
//         return base.OnConnectedAsync();
//     }
//     
//     /// <inheritdoc />
//     public override async Task OnDisconnectedAsync(Exception? exception)
//     {
//         if (exception != null)
//             Logger.Instance.ELog($"FlowHub Client disconnected with error: {exception.Message}");
//         else
//             Logger.Instance.ILog($"FlowHub Client disconnected: {Context.ConnectionId}");
//         
//         await base.OnDisconnectedAsync(exception);
//     }
//
//     /// <summary>
//     /// Logs a message
//     /// </summary>
//     /// <param name="runnerUid">the UID of the flow runner</param>
//     /// <param name="libraryFileUid">the UID of the library file</param>
//     /// <param name="message">the message to log</param>
//     public async Task LogMessage(Guid runnerUid, Guid libraryFileUid, string message)
//     {
//         try
//         {
//             if (runnerUid == Guid.Empty)
//                 return; // test message
//             
//             await LibraryFileLogHelper.AppendToLog(libraryFileUid, message);
//         }
//         catch (Exception)
//         {
//             // Ignored
//         }
//     }
//
//     // /// <summary>
//     // /// Receives a hello from the flow runner, indicating its still alive and executing
//     // /// </summary>
//     // /// <param name="runnerUid">the UID of the flow runner</param>
//     // /// <param name="infoJson">the flow execution info serialized</param>
//     // /// <returns>if the hello was successful or not</returns>
//     // public async Task<bool> Hello(Guid runnerUid, string infoJson)
//     // {
//     //     try
//     //     {
//     //         FlowExecutorInfo? info = string.IsNullOrEmpty(infoJson)
//     //             ? null
//     //             : JsonSerializer.Deserialize<FlowExecutorInfo>(infoJson);
//     //         return await ServiceLoader.Load<FlowRunnerService>().Hello(runnerUid, info);
//     //     }
//     //     catch(Exception ex)
//     //     {
//     //         Logger.Instance.ELog("Error in hello: " + ex.Message + Environment.NewLine + ex.StackTrace);
//     //         return false;
//     //     }
//     // }
//
//     /// <summary>
//     /// Tells the server to ignore the specified path when scanning
//     /// </summary>
//     /// <param name="path">the Path to ignore</param>
//     public void LibraryIgnorePath(string path)
//     {
//         try
//         {
//             Logger.Instance.ILog("Ignoring Path from library scanning: " + path);
//             WatchedLibraryNew.IgnorePath(path);
//         }
//         catch(Exception ex)
//         {
//             Logger.Instance.ELog("Failed to ignore path from library scanning: " + path + " => " + ex.Message);
//             // Ignored
//         }
//     }
// }