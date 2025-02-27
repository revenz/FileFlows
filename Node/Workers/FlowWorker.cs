// using System.Globalization;
// using System.Text;
// using FileFlows.Plugin;
// using FileFlows.RemoteServices;
// using FileFlows.ServerShared;
// using FileFlows.ServerShared.Models;
// using FileFlows.ServerShared.Helpers;
// using FileFlows.ServerShared.Services;
// using FileFlows.ServerShared.Workers;
// using FileFlows.Shared.Models;
// using JsonSerializer = System.Text.Json.JsonSerializer;
//
// namespace FileFlows.Node.Workers;
//
//
// /// <summary>
// /// A flow worker executes a flow and start a flow runner
// /// </summary>
// public class FlowWorker : Worker
// {
//     /// <summary>
//     /// A unique identifier to identify the flow worker
//     /// This flow worker can have multiple executing processes so this do UID does
//     /// not match the UI of an executor in the UI
//     /// </summary>
//     public readonly Guid Uid = Guid.NewGuid();
//
//     private readonly string _configKeyDefault = Guid.NewGuid().ToString();
//
//     /// <summary>
//     /// Gets if the config encryption key 
//     /// </summary>
//     /// <returns>the configuration encryption key</returns>
//     private string GetConfigKey(ProcessingNode node)
//     {
//         var key = Environment.GetEnvironmentVariable("FF_ENCRYPT");
//         if (string.IsNullOrWhiteSpace(key) == false)
//             return key;
//         key = node?.GetVariable("FF_ENCRYPT");
//         
//         if (string.IsNullOrWhiteSpace(key) == false)
//             return key;
//         
//         return _configKeyDefault;
//     }
//
//     /// <summary>
//     /// The instance of the flow worker 
//     /// </summary>
//     private static FlowWorker? Instance;
//
//     private readonly Mutex mutex = new Mutex();
//     private readonly List<Guid> ExecutingRunners = new ();
//
//     private const int DEFAULT_INTERVAL = 10;
//     
//     /// <summary>
//     /// If this flow worker is running on the server or an external processing node
//     /// </summary>
//     private readonly bool isServer;
//
//     private bool FirstExecute;
//
//     /// <summary>
//     /// The host name of the processing node
//     /// </summary>
//     private string Hostname { get; set; }
//
//     
//     /// <summary>
//     /// Constructs a flow worker instance
//     /// </summary>
//     /// <param name="hostname">the host name of the processing node</param>
//     /// <param name="isServer">if this flow worker is running on the server or an external processing node</param>
//     public FlowWorker(string hostname, bool isServer = false) : base(ScheduleType.Second, DEFAULT_INTERVAL, quiet: true)
//     {
//         FlowWorker.Instance = this;
//         this.isServer = isServer;
//         this.FirstExecute = true;
//         this.Hostname = hostname;
//     }
//
//     /// <summary>
//     /// A function to check if this flow worker is enabled
//     /// </summary>
//     public Func<bool>? IsEnabledCheck { get; set; }
//
//     /// <summary>
//     /// Gets if there are any active runners
//     /// </summary>
//     public static bool HasActiveRunners => Instance?.ExecutingRunners?.Any() == true;
//
//     /// <summary>
//     /// Gets the number of active runners
//     /// </summary>
//     public static int ActiveRunners => Instance?.ExecutingRunners?.Count ?? 0;
//
//     /// <summary>
//     /// Executes the flow worker and start a flow runner if required
//     /// </summary>
//     protected override void Execute()
//     {
//         bool canProcessMore = ExecuteActual(out ProcessingNode? node);
//         // check if the timer has changed
//         if (node == null)
//             return;
//
//         if (canProcessMore)
//         {
//             Initialize(ScheduleType.Minute, 1);
//             Task.Run(async () =>
//             {
//                 // wait one second so the other process can start and actually tell the service its running
//                 int delay = CurrentConfig?.DelayBetweenNextFile ?? 0;
//                 if(delay > 0)
//                     await Task.Delay(delay);
//                 Execute();
//             });
//             return;
//         }
//
//         int newInterval = node.ProcessFileCheckInterval;
//         if (newInterval < 1)
//         {
//             var settingsService = ServiceLoader.Load<IFlowRunnerService>();
//             newInterval = settingsService.GetFileCheckInterval().Result;
//         }
//         
//         if (newInterval == Interval || newInterval < 5)
//             return;
//         Logger.Instance.ILog($"Updating file check interval to {newInterval} seconds");
//         Initialize(ScheduleType.Second, newInterval);
//     }
//     
//     /// <summary>
//     /// Actually executes this worker
//     /// </summary>
//     /// <returns>true if a node started processing and it can instantly start processing more files</returns>
//     private bool ExecuteActual(out ProcessingNode? node)
//     {
//         node = null;
//         if (UpdaterWorker.UpdatePending)
//             return false;
//
//         if (IsEnabledCheck?.Invoke() == false)
//             return false;
//         
//         var nodeService = ServiceLoader.Load<INodeService>();
//         if (nodeService.GetSystemIsRunning().Result != true)
//         {
//             Logger.Instance?.ILog("FileFlows server is paused or not running.");
//             return false;
//         }
//         try
//         {
//             node = isServer ? nodeService.GetServerNodeAsync().Result : nodeService.GetByAddressAsync(this.Hostname).Result;
//             if(node == null)
//             {
//                 Logger.Instance?.ELog("Failed to register node");
//                 return false;
//             }
//         }
//         catch(Exception ex)
//         {
//             Logger.Instance?.ELog("Failed to register node: " + ex.Message);
//             return false;
//         }
//
//
//         if (FirstExecute)
//         {
//             FirstExecute = false;
//             // tell the server to kill any flow executors from this node, in case this node was restarted
//             nodeService.ClearWorkersAsync(node.Uid).Wait();
//         }
//
//         var frService = ServiceLoader.Load<IFlowRunnerService>();
//         var isLicensed = frService.IsLicensed().Result;
//
//         string nodeName = node?.Name == "FileFlowsServer" ? "Internal Processing Node" : (node?.Name ?? "Unknown");
//
//         if (node?.Enabled != true)
//         {
//             if(node?.Name != CommonVariables.InternalNodeName)
//                 Logger.Instance?.DLog($"Node '{nodeName}' is not enabled");
//             return false;
//         }
//
//         if (UpdateConfiguration(node) == false)
//         {
//             Logger.Instance?.WLog("Failed to write configuration for Node, pausing system");
//             nodeService.Pause(30).Wait();
//             return false;
//         }
//
//         int count = ExecutingRunners.Count;
//         if (node?.FlowRunners <= ExecutingRunners.Count)
//         {
//             Logger.Instance?.DLog($"At limit of running executors on '{nodeName}': " + node.FlowRunners);
//             return false; // already maximum executors running
//         }
//
//
//         string tempPath = node?.TempPath?.EmptyAsNull() ?? AppSettings.ForcedTempPath?.EmptyAsNull() ?? string.Empty;
//         if (string.IsNullOrEmpty(tempPath))
//         {
//             Logger.Instance?.ELog($"Temp Path not set on node '{nodeName}', cannot process");
//             return false;
//         }
//         
//         if(Directory.Exists(tempPath) == false)
//         {
//             try
//             {
//                 Directory.CreateDirectory(tempPath);
//             }
//             catch (Exception)
//             {
//                 Logger.Instance?.ELog($"Temp Path does not exist on on node '{nodeName}', and failed to create it: {tempPath}");
//                 return false;
//             }
//         }
//
//         
//         var libFileService = ServiceLoader.Load<ILibraryFileService>();
//         if (isLicensed && node?.PreExecuteScript != null)
//         {
//             if (PreExecuteScriptTest(node) == false)
//             {
//                 int interval = node.ProcessFileCheckInterval;
//                 if (interval < 1)
//                 {
//                     var settingsService = ServiceLoader.Load<IFlowRunnerService>();
//                     interval = settingsService.GetFileCheckInterval().Result;
//                 }
//                 interval = Math.Max(10, interval);
//                 // tell the server, so if this is a higher priority node, it doesn't block processing
//                 libFileService.NodeCannotRun(node.Uid, interval).Wait();
//                 return false;
//             }
//         }
//         var libFileResult = libFileService.GetNext(node?.Name ?? string.Empty, node?.Uid ?? Guid.Empty,node?.Version ?? string.Empty, Uid).Result;
//         if (libFileResult?.Status != NextLibraryFileStatus.Success)
//         {
//             Logger.Instance.ILog("No file found to process, status from server: " + (libFileResult?.Status.ToString() ?? "UNKNOWN"));
//             return false;
//         }
//         if (libFileResult?.File == null)
//             return false; // nothing to process
//         var libFile = libFileResult.File;
//
//         bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
//         AddExecutingRunner(libFile.Uid);
//         var node2 = node;
//         Task.Run(() =>
//         {
//             int exitCode = 0; 
//             StringBuilder completeLog = new StringBuilder();
//             bool keepFiles = false;
//             try
//             {
//                 var runnerParameters = new RunnerParameters();
//                 runnerParameters.Uid = libFile.Uid;
//                 runnerParameters.NodeUid = node2!.Uid;
//                 runnerParameters.LibraryFile = libFile.Uid;
//                 runnerParameters.TempPath = tempPath;
//                 runnerParameters.ConfigPath = GetConfigurationDirectory();
//                 runnerParameters.ConfigKey = GetConfigNoEncrypt(node2) ? "NO_ENCRYPT" : GetConfigKey(node2);
//                 runnerParameters.BaseUrl = RemoteService.ServiceBaseUrl;
//                 runnerParameters.AccessToken = RemoteService.AccessToken;
//                 runnerParameters.RemoteNodeUid = RemoteService.NodeUid;
//                 runnerParameters.IsDocker = Globals.IsDocker;
//                 runnerParameters.IsInternalServerNode = isServer;
//                 runnerParameters.Hostname = isServer ? null : Hostname;
//                 #if(DEBUG)
//                 runnerParameters.RunnerTempPath = "ff-debug-mode"; 
//                 #endif
//                 string json = JsonSerializer.Serialize(runnerParameters);
//                 string randomString = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 20)
//                     .Select(s => s[new Random().Next(s.Length)]).ToArray());
//                 string encrypted = Decrypter.Encrypt(json, "hVYjHrWvtEq8huShjTkA" + randomString + "oZf4GW3jJtjuNHlMNpl9");
//                 var parameters = new[] { encrypted, randomString };
//                 string workingDir = Path.Combine(tempPath, "Runner-" + libFile.Uid);
// #pragma warning restore CS8601 // Possible null reference assignment.
//
//                 try
//                 {
// #if (DEBUG)
//                     (exitCode, string output)  = FlowRunner.Program.RunWithLog(parameters);
//                     string error = string.Empty;
// #else   
//                     using Process process = new Process();
//                 
//                     process.StartInfo = new ProcessStartInfo();
//                     process.StartInfo.FileName = GetDotnetLocation();
//                     process.StartInfo.WorkingDirectory = DirectoryHelper.FlowRunnerDirectory;
//                     process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
//                     foreach (var str in parameters)
//                         process.StartInfo.ArgumentList.Add(str);
//
//                     Logger.Instance?.ILog("Executing: " + process.StartInfo.FileName + " " + String.Join(" ", process.StartInfo.ArgumentList.Select(x => "\"" + x + "\"")));
//                     Logger.Instance?.ILog("Working Directory: " + process.StartInfo.WorkingDirectory);
//
//                     process.StartInfo.UseShellExecute = false;
//                     process.StartInfo.RedirectStandardOutput = true;
//                     process.StartInfo.RedirectStandardError = true;
//                     process.StartInfo.CreateNoWindow = true;
//                     process.Start();
//                     string output = process.StandardOutput.ReadToEnd();
//                     string error = process.StandardError.ReadToEnd();
//                     process.WaitForExit();
//                     exitCode = process.ExitCode;
//
//                 #endif
//                     if (exitCode == 100)
//                     {
//                         exitCode = 0; // special case
//                         keepFiles = true;
//                     }
//                     
//                     if (string.IsNullOrEmpty(output) == false)
//                     {
//                         completeLog.AppendLine(
//                             "==============================================================================" + Environment.NewLine +
//                             "===                      PROCESSING NODE OUTPUT START                      ===" + Environment.NewLine +
//                             "==============================================================================" + Environment.NewLine +
//                             output + Environment.NewLine +
//                             "==============================================================================" + Environment.NewLine +
//                             "===                       PROCESSING NODE OUTPUT END                       ===" + Environment.NewLine +
//                             "==============================================================================");
//                     }
//                     if (string.IsNullOrEmpty(error) == false)
//                     {
//                         completeLog.AppendLine(
//                             "==============================================================================" + Environment.NewLine +
//                             "===                   PROCESSING NODE ERROR OUTPUT START                   ===" + Environment.NewLine +
//                             "==============================================================================" + Environment.NewLine +
//                             error  + Environment.NewLine +
//                             "==============================================================================" + Environment.NewLine +
//                             "===                    PROCESSING NODE ERROR OUTPUT END                    ===" + Environment.NewLine +
//                             "==============================================================================");
//                     }
//
//                     if (exitCode is 0 or (int)FileStatus.ReprocessByFlow)
//                         return;
//                     
//                     Logger.Instance?.ELog("Error executing runner: Exit code: " + exitCode);
//                     if (Enum.IsDefined(typeof(FileStatus), exitCode))
//                         libFile.Status = (FileStatus)exitCode;
//                     else
//                     {
//                         libFile.Status = FileStatus.ProcessingFailed;
//                         Logger.Instance?.ILog("Invalid exit code, setting file as failed");
//                     }
//                     FinishWork(libFile.Uid, node2, libFile);
//                 }
//                 catch (Exception ex)
//                 {
//                     AppendToCompleteLog(completeLog, "Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace, type: "ERR");
//                     libFile.Status = FileStatus.ProcessingFailed;
//                     FinishWork(libFile.Uid, node2, libFile);
//                     exitCode = (int)FileStatus.ProcessingFailed;
//                 }
//             }
//             finally
//             {
//                 RemoveExecutingRunner(libFile.Uid);
//
//                 try
//                 {
//                     string dir = Path.Combine(tempPath, "Runner-" + libFile.Uid);
//                     if (keepFiles == false || CurrentConfigurationKeepFailedFlowFiles == false)
//                     {
//                         if (Directory.Exists(dir))
//                         {
//                             Directory.Delete(dir, true);
//                             AppendToCompleteLog(completeLog, "Deleted temporary directory: " + dir);
//                         }
//                     }
//                     else
//                     {
//                         AppendToCompleteLog(completeLog, "Flow failed keeping temporary files in: " + dir);
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     AppendToCompleteLog(completeLog, "Failed to clean up runner directory: " + ex.Message, type: "ERR");
//                 }
//                 
//                 SaveLog(libFile, completeLog.ToString());
//
//                 Trigger();
//             }
//         });
//         
//         if (count + 1 >= node!.FlowRunners)
//             return false;
//
//         var library = CurrentConfig?.Libraries?.FirstOrDefault(x => x.Uid == libFile.LibraryUid);
//         
//         if (CurrentConfig?.LicenseLevel != FileFlows.Common.LicenseLevel.Basic && library is { MaxRunners: > 0 })
//             return false; // cant process instantly or this could ignore the library limit
//
//         return true;
//     }
//
//     private void FinishWork(Guid processUid, ProcessingNode node, LibraryFile libFile)
//     {
//         FlowExecutorInfo info = new()
//         {
//             Uid = processUid,
//             LibraryFile = libFile,
//             NodeUid = node.Uid,
//             NodeName = node.Name,
//             RelativeFile = libFile.RelativePath,
//             Library = libFile.Library
//         };
//         ServiceLoader.Load<IFlowRunnerService>().Finish(info).Wait();
//     }
//
//     /// <summary>
//     /// Adds a message to the complete log with a formatted date
//     /// </summary>
//     /// <param name="completeLog">the complete log</param>
//     /// <param name="message">the message to add</param>
//     private void AppendToCompleteLog(StringBuilder completeLog, string message, string type = "INFO")
//         => completeLog.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} [{type}] -> {message}");
//
//     private bool PreExecuteScriptTest(ProcessingNode node)
//     {
//         var script = CurrentConfig?.SystemScripts?.FirstOrDefault(x => node.PreExecuteScript!.Value == x.Uid);
//         if (script != null && script.Language != ScriptLanguage.JavaScript)
//         {
//             StringLogger logger = new();
//             var seResult = new ScriptExecutor().Execute(new()
//             {
//                 Code = script.Code,
//                 TempPath = Path.GetTempPath(),
//                 Language = script.Language,
//                 Logger = logger,
//                 ScriptType = ScriptType.System,
//                 NotificationCallback = (severity, title, message) =>
//                 {
//                     _ = ServiceLoader.Load<INotificationService>()
//                         .Record((NotificationSeverity)severity, title, message);
//                 } 
//             });
//             if (seResult.Failed(out var error))
//             {
//                 _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
//                     $"Failed executing pre-execute script '{script.Name}'",
//                     $"Failed to execute on node '{node.Name}': {error}");
//                 Logger.Instance.WLog("Failed running pre-execute script: " + error + "\n" + logger);
//                 return false;
//             }
//             bool preExecuteResult = seResult.Value == 1;
//             
//             if(preExecuteResult == false)
//                 Logger.Instance.ILog("Pre-Execute Script Returned False:\n" + logger);
//             else
//                 Logger.Instance.ILog("Pre-Execute Script Successful:\n" + logger);
//             
//             return preExecuteResult;
//         }
//         string scriptDir = Path.Combine(GetConfigurationDirectory(), "Scripts");
//         string sharedDir = Path.Combine(scriptDir, "Shared");
//         string jsFile = Path.Combine(scriptDir, "System", node.PreExecuteScript + ".js");
//         if (File.Exists(jsFile) == false)
//         {
//             Logger.Instance.ELog("Failed to locate pre-execute script: " + node.PreExecuteScript);
//             return false;
//         }
//
//         Logger.Instance.ILog("Loading Pre-Execute Script: " + jsFile);
//         string code = File.ReadAllText(jsFile);
//         if (string.IsNullOrWhiteSpace(code))
//         {
//             Logger.Instance.ELog("Failed to load pre-execute script code");
//             return false;
//         }
//
//         var variableService = ServiceLoader.Load<IVariableService>();
//         var variables = variableService.GetAllAsync().Result?.ToDictionary(x => x.Name, x => (object)x.Value) ?? new ();
//         variables["FileFlows.Url"] = RemoteService.ServiceBaseUrl;
//         var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDir);
//         if (result.Success == false)
//         {
//             Logger.Instance.ELog("Pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
//             return false;
//         }
//         Logger.Instance.ILog("Pre-Execute script returned: " + (result.ReturnValue == null ? "" : System.Text.Json.JsonSerializer.Serialize(result.ReturnValue)));
//         
//
//         if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "exit")
//         {
//             Logger.Instance.ILog("Exiting");
//             return false;
//         }
//
//         if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "restart")
//         {
//             if (Globals.IsDocker == false)
//             {
//                 Logger.Instance.WLog("Requested restart but not running inside docker");
//                 return false;
//             }
//
//             if (HasActiveRunners)
//             {
//                 Logger.Instance.WLog("Requested restart, but there are executing runners");
//                 return false;
//             }
//             
//             Logger.Instance.ILog("Restarting...");
//             Thread.Sleep(5_000);
//             Environment.Exit(0);
//             return false;
//         }
//         
//         if (result.ReturnValue as bool? == false)
//         {
//             Logger.Instance.ELog("Output from pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
//             return false;
//         }
//         Logger.Instance.ILog("Pre-execute script passed: \n"+ result.Log.Replace("\\n", "\n"));
//         return true;
//     }
//
//     /// <summary>
//     /// Adds an executing runner to the list of currently running flow runners
//     /// </summary>
//     /// <param name="uid">The uid of the flow runner to add</param>
//     private void AddExecutingRunner(Guid uid)
//     {
//         mutex.WaitOne();
//         try
//         {
//             ExecutingRunners.Add(uid);
//         }
//         finally
//         {
//             mutex.ReleaseMutex();   
//         }
//     }
//
//     
//     /// <summary>
//     /// Removes a flow runner from the list of currently executing flow runners
//     /// </summary>
//     /// <param name="uid">The uid of the flow runner to remove</param>
//     private void RemoveExecutingRunner(Guid uid)
//     {
//         Logger.Instance?.ILog($"Removing executing runner[{ExecutingRunners.Count}]: {uid}");
//         mutex.WaitOne();
//         try
//         {
//             if (ExecutingRunners.Contains(uid))
//                 ExecutingRunners.Remove(uid);
//             else
//             {
//                 Logger.Instance?.ILog("Executing runner not in list: " + uid +" => " + string.Join(",", ExecutingRunners.Select(x => x.ToString())));
//             }
//             Logger.Instance?.ILog("Runner count: " + ExecutingRunners.Count);
//         }
//         catch(Exception ex)
//         {
//             Logger.Instance?.ELog("Failed to remove executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace);    
//         }
//         finally
//         {
//             mutex.ReleaseMutex();
//         }
//     }
//
//     /// <summary>
//     /// Saves the flow runner log to the server
//     /// </summary>
//     /// <param name="libFile">The Library File that was processed</param>
//     /// <param name="log">The full flow runner log</param>
//     private void SaveLog(LibraryFile libFile, string log)
//     {
//         if (string.IsNullOrWhiteSpace(log))
//             return;
//         var service = ServiceLoader.Load<ILibraryFileService>();
//         bool saved = service.SaveFullLog(libFile.Uid, log).Result;
//         if (!saved)
//         {
//             // save to main output
//             log = string.Join('\n', log.Split('\n').Select(x => "       " + x).ToArray());
//             Logger.Instance?.DLog(Environment.NewLine + log);
//         }
//     }
//
//     /// <summary>
//     /// The location of dotnet
//     /// </summary>
//     private static string Dotnet = "";
//     
//     /// <summary>
//     /// Gets the location of dotnet to use to start the flow runner
//     /// </summary>
//     /// <returns>the location of dotnet to use to start the flow runner</returns>
//     private string GetDotnetLocation()
//     {
//         if(string.IsNullOrEmpty(Dotnet))
//         {
//             if (Globals.IsWindows == false && File.Exists("/dotnet/dotnet"))
//                 Dotnet = "/dotnet/dotnet"; // location of docker
//             else if (Globals.IsWindows == false && File.Exists("/root/.dotnet/dotnet"))
//                 Dotnet = "/root/.dotnet/dotnet"; // location of legacy docker
//             else
//                 Dotnet = "dotnet";// assume in PATH
//         }
//         return Dotnet;
//     }
//
//
//     /// <summary>
//     /// Gets the executors UIDs
//     /// </summary>
//     /// <returns>the executors UIDs</returns>
//     public Guid[] GetExecutors()
//         => ExecutingRunners?.ToArray() ?? new Guid[] { };
// }
