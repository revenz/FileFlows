// using FileFlows.Plugin;
// using FileFlows.Shared;
// using FileFlows.Shared.Models;
// using Microsoft.AspNetCore.SignalR.Client;
//
// namespace FileFlows.FlowRunner;
//
// /// <summary>
// /// Interface used for the flow runner to communicate with the FileFlows server
// /// </summary>
// public interface IFlowRunnerCommunicator
// {
//     /// <summary>
//     /// Logs a message to the FileFlows server
//     /// </summary>
//     /// <param name="runnerUid">the UID of the flow runner</param>
//     /// <param name="message">the message to log</param>
//     /// <returns>a completed task</returns>
//     Task LogMessage(Guid runnerUid, string message);
// }
//
// /// <summary>
// /// A communicator by the flow runner to communicate with the FileFlows server
// /// </summary>
// public class FlowRunnerCommunicator : IFlowRunnerCommunicator, IAsyncDisposable
// {
//     /// <summary>
//     /// Gets or sets the URL to the signalr endpoint on the FileFlows server
//     /// </summary>
//     public static string SignalrUrl { get; set; }
//     
//     /// <summary>
//     /// The signalr hub connection
//     /// </summary>
//     HubConnection connection;
//     
//     /// <summary>
//     /// The UID of the executing library file
//     /// </summary>
//     private Guid LibraryFileUid;
//     
//     /// <summary>
//     /// Delegate used when the flow is being canceled
//     /// </summary>
//     public delegate void Cancel();
//     
//     /// <summary>
//     /// Event used when the flow is being canceled
//     /// </summary>
//     public event Cancel OnCancel;
//
//     /// <summary>
//     /// The run instance running this
//     /// </summary>
//     private readonly RunInstance runInstance;
//
//     /// <summary>
//     /// Indicates whether the connection is established.
//     /// </summary>
//     public bool IsConnected => connection?.State == HubConnectionState.Connected;
//
//
//     /// <summary>
//     /// Creates an instance of the flow runner communicator
//     /// </summary>
//     /// <param name="runInstance">the run instance running this</param>
//     /// <param name="libraryFileUid">the UID of the library file being executed</param>
//     /// <exception cref="Exception">throws an exception if cannot connect to the server</exception>
//     public FlowRunnerCommunicator(RunInstance runInstance, Guid libraryFileUid)
//     {
//         this.runInstance = runInstance;
//         this.LibraryFileUid = libraryFileUid;
//         runInstance.LogInfo("SignalrUrl: " + SignalrUrl);
//
//         // Build the SignalR connection
//         connection = new HubConnectionBuilder()
//             .WithUrl(new Uri(SignalrUrl))
//             .WithAutomaticReconnect()
//             .Build();
//
//         connection.Closed += Connection_Closed;
//         connection.On<Guid>("AbortFlow", OnAbortFlow);
//     }
//
//     /// <summary>
//     /// Initializes the SignalR connection to the server asynchronously and tests the connection.
//     /// </summary>
//     /// <returns>True if the connection was successful; otherwise, false.</returns>
//     public async Task<bool> InitializeAsync()
//     {
//         return await StartConnectionAsync();
//     }
//
//     /// <summary>
//     /// Starts the SignalR connection asynchronously and sends a test message to the server.
//     /// </summary>
//     /// <returns>True if the connection was successful; otherwise, false.</returns>
//     private async Task<bool> StartConnectionAsync()
//     {
//         try
//         {
//             await connection.StartAsync();
//             if (connection.State == HubConnectionState.Disconnected)
//             {
//                 runInstance.LogError("Initial connection failed; attempting reconnection...");
//                 await Connection_Closed(new Exception("Initial connection failed."));
//                 return false;
//             }
//
//             // Send a test message to the server
//             await LogMessage(Guid.Empty, "Connected");
//             runInstance.LogInfo("Successfully connected to the server and sent test message.");
//             return true;
//         }
//         catch (Exception ex)
//         {
//             runInstance.LogError($"Error starting connection: {ex.Message}");
//             return false;
//         }
//     }
//     
//     /// <summary>
//     /// Handles the "AbortFlow" event from the SignalR server.
//     /// </summary>
//     /// <param name="uid">The UID of the flow that should be aborted.</param>
//     private void OnAbortFlow(Guid uid)
//     {
//         if (uid != LibraryFileUid)
//             return;
//         OnCancel?.Invoke();
//     }
//     
//     /// <summary>
//     /// Closes the Signalr connection to the server
//     /// </summary>
//     public void Close()
//     {
//         try
//         {
//             connection?.DisposeAsync();
//         }
//         catch (Exception)
//         {
//             // Ignore any exceptions here  
//         } 
//     }
//
//     /// <summary>
//     /// Called when the Signalr connection is closed
//     /// </summary>
//     /// <param name="arg">the connection exception</param>
//     /// <returns>a completed task</returns>
//     private async Task Connection_Closed(Exception? arg)
//     {
//         if (arg != null)
//         {
//             runInstance.LogError($"Connection closed with error: {arg.Message}");
//             while (arg.InnerException != null)
//             {
//                 arg = arg.InnerException;
//                 runInstance.LogError($"Connection closed with inner exception: {arg.Message}");
//             }
//         }
//         else
//             runInstance.LogInfo("Connection closed.");
//
//         var retryCount = 0;
//         var maxRetries = 5;
//         var retryDelay = TimeSpan.FromSeconds(5);
//
//         while (retryCount < maxRetries)
//         {
//             try
//             {
//                 await Task.Delay(retryDelay);
//                 await connection.StartAsync();
//                 runInstance.LogInfo("Reconnected to the server.");
//                 return;
//             }
//             catch (Exception ex)
//             {
//                 if (ex.Message.Contains("disposed object"))
//                     return;
//                 runInstance.LogError($"Reconnection attempt {retryCount + 1} failed: {ex.Message}");
//                 retryCount++;
//                 retryDelay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
//             }
//         }
//
//         runInstance.LogError("Failed to reconnect after multiple attempts.");
//     }
//
//     /// <summary>
//     /// Logs a message to the FileFlows server
//     /// </summary>
//     /// <param name="runnerUid">the UID of the flow runner</param>
//     /// <param name="message">the message to log</param>
//     /// <returns>a completed task</returns>
//     public async Task LogMessage(Guid runnerUid, string message)
//     {
//         try
//         {
//             await connection.InvokeAsync("LogMessage", runnerUid, LibraryFileUid, message);
//         } 
//         catch (Exception)
//         {
//             // silently fail here, we store the log in memory if one message fails its not a biggie
//             // once the flow is complete we send the entire log to the server to update
//         }
//     }
//
//     /// <summary>
//     /// Sends a hello to the server saying this runner is still executing
//     /// </summary>
//     /// <param name="runnerUid">the UID of the flow runner</param>
//     /// <param name="info">The flow execution info</param>
//     /// <param name="args">the node parameters</param>
//     public async Task<bool> Hello(Guid runnerUid, FlowExecutorInfo info, NodeParameters args)
//     {
//         try
//         {
//             bool helloResult = await connection.InvokeAsync<bool>("Hello", runnerUid, JsonSerializer.Serialize(new
//             {
//                 info.Library,
//                 info.Uid,
//                 info.CurrentPart,
//                 info.InitialSize,
//                 info.IsDirectory,
//                 info.LastUpdate,
//                 info.LibraryFile,
//                 info.LibraryPath,
//                 info.NodeName,
//                 info.NodeUid,
//                 info.RelativeFile,
//                 info.StartedAt,
//                 info.TotalParts,
//                 info.WorkingFile,
//                 info.CurrentPartName,
//                 info.CurrentPartPercent
//             }));
//             if(helloResult == false)
//                 args?.Logger?.WLog("Received a false from the hello request to the server");
//             return helloResult;
//         }
//         catch(Exception ex)
//         {
//             args?.Logger?.ELog("Failed to send hello to server: " + ex.Message);
//             return false;
//         }
//     }
//
//     /// <summary>
//     /// Loads an instance of the FlowRunnerCommunicator
//     /// </summary>
//     /// <param name="runInstance">The run instance running this</param>
//     /// <param name="libraryFileUid">the UID of the library file being processed</param>
//     /// <returns>an instance of the FlowRunnerCommunicator</returns>
//     public static FlowRunnerCommunicator Load(RunInstance runInstance, Guid libraryFileUid)
//     {
//         return new FlowRunnerCommunicator(runInstance, libraryFileUid);
//
//     }
//     
//     /// <summary>
//     /// Disposes of the FlowRunnerCommunicator
//     /// </summary>
//     public async ValueTask DisposeAsync()
//     {
//         if (connection != null)
//         {
//             try
//             {
//                 await connection.DisposeAsync();
//             }
//             catch (Exception ex)
//             {
//                 runInstance.LogError($"Error disposing SignalR connection: {ex.Message}");
//             }
//         }
//     }
//
//     /// <summary>
//     /// Tells the server to ignore the specified path when scanning
//     /// </summary>
//     /// <param name="path">the Path to ignore</param>
//     public async Task LibraryIgnorePath(string path)
//     {
//         try
//         {
//             await connection.InvokeAsync<bool>("LibraryIgnorePath", path);
//         }
//         catch(Exception)
//         {
//             // Ignored
//         }
//     }
// }