// using System.Text;
// using System.Text.Json;
// using FileFlows.Plugin;
// using FileFlows.RemoteServices;
// using FileFlows.ServerShared;
// using FileFlows.ServerShared.Models;
// using FileFlows.ServerShared.Services;
// using FileFlows.ServerShared.Workers;
// using FileFlows.Shared.Models;
//
// namespace FileFlows.Node.SignalAre;
//
// /// <summary>
// /// Manager for manging the runneer
// /// </summary>
// public class RunnerManager
// {
//     private readonly List<Guid> ExecutingRunners = new ();
//
//     /// <summary>
//     /// Tries to run a file
//     /// </summary>
//     /// <param name="args">the run file arguments</param>
//     /// <returns>true if can run the file</returns>
//     public bool Run(RunFileArguments args)
//     {
//         if (UpdaterWorker.UpdatePending)
//             return false;
//
//         var node = args.Node;
//
//         string tempPath = node.TempPath?.EmptyAsNull() ?? AppSettings.ForcedTempPath?.EmptyAsNull() ?? string.Empty;
//         if (string.IsNullOrEmpty(tempPath))
//         {
//             Logger.Instance?.ELog($"Temp Path not set on node '{node.Name}', cannot process");
//             return false;
//         }
//
//         if (Directory.Exists(tempPath) == false)
//         {
//             try
//             {
//                 Directory.CreateDirectory(tempPath);
//             }
//             catch (Exception)
//             {
//                 Logger.Instance?.ELog(
//                     $"Temp Path does not exist on on node '{node.Name}', and failed to create it: {tempPath}");
//                 return false;
//             }
//         }
//
//         var cfgService = ServiceLoader.Load<ConfigurationService>();
//         if (cfgService.CurrentConfig?.Revision != args.ConfigRevision)
//             return false;
//
//
//         if (args.CanRunPreExecuteCheck && node.PreExecuteScript != null)
//         {
//             if (PreExecuteScriptTest(node) == false)
//                 return false;
//         }
//
//         // start the run instance now
//         _ = RunActual(args, tempPath);
//
//         return true;
//     }
//
//     private async Task RunActual(RunFileArguments args, string tempPath)
//     {
//         var cfgService = ServiceLoader.Load<ConfigurationService>();
//
//         var node = args.Node;
//
//         var libFile = args.LibraryFile;
//
//         bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
//         bool isServer = node.Uid == CommonVariables.InternalNodeUid;
//         ExecutingRunners.Add(libFile.Uid);
//         var node2 = node;
//
//         int exitCode = 0;
//         StringBuilder completeLog = new StringBuilder();
//         bool keepFiles = false;
//         try
//         {
//             var runnerParameters = new RunnerParameters();
//             runnerParameters.Uid = libFile.Uid;
//             runnerParameters.NodeUid = node2!.Uid;
//             runnerParameters.LibraryFile = libFile.Uid;
//             runnerParameters.TempPath = tempPath;
//             runnerParameters.ConfigPath = cfgService.GetConfigurationDirectory();
//             runnerParameters.ConfigKey =
//                 cfgService.GetConfigNoEncrypt(node2) ? "NO_ENCRYPT" : cfgService.GetConfigKey(node2);
//             runnerParameters.BaseUrl = RemoteService.ServiceBaseUrl;
//             runnerParameters.AccessToken = RemoteService.AccessToken;
//             runnerParameters.RemoteNodeUid = RemoteService.NodeUid;
//             runnerParameters.IsDocker = Globals.IsDocker;
//             runnerParameters.IsInternalServerNode = isServer;
//             runnerParameters.Hostname = isServer ? null : AppSettings.Instance.HostName;
// #if(DEBUG)
//             runnerParameters.RunnerTempPath = "ff-debug-mode";
// #endif
//             string json = JsonSerializer.Serialize(runnerParameters);
//             string randomString = new string(Enumerable
//                 .Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 20)
//                 .Select(s => s[new Random().Next(s.Length)]).ToArray());
//             string encrypted = FileFlows.ServerShared.Helpers.Decrypter.Encrypt(json,
//                 "hVYjHrWvtEq8huShjTkA" + randomString + "oZf4GW3jJtjuNHlMNpl9");
//             var parameters = new[] { encrypted, randomString };
//             string workingDir = Path.Combine(tempPath, "Runner-" + libFile.Uid);
// #pragma warning restore CS8601 // Possible null reference assignment.
//
//             try
//             {
// #if (DEBUG)
//                 (exitCode, string output) = FlowRunner.Program.RunWithLog(parameters);
//                 string error = string.Empty;
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
// #endif
//                 if (exitCode == 100)
//                 {
//                     exitCode = 0; // special case
//                     keepFiles = true;
//                 }
//
//                 if (string.IsNullOrEmpty(output) == false)
//                 {
//                     completeLog.AppendLine(
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         "===                      PROCESSING NODE OUTPUT START                      ===" +
//                         Environment.NewLine +
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         output + Environment.NewLine +
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         "===                       PROCESSING NODE OUTPUT END                       ===" +
//                         Environment.NewLine +
//                         "==============================================================================");
//                 }
//
//                 if (string.IsNullOrEmpty(error) == false)
//                 {
//                     completeLog.AppendLine(
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         "===                   PROCESSING NODE ERROR OUTPUT START                   ===" +
//                         Environment.NewLine +
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         error + Environment.NewLine +
//                         "==============================================================================" +
//                         Environment.NewLine +
//                         "===                    PROCESSING NODE ERROR OUTPUT END                    ===" +
//                         Environment.NewLine +
//                         "==============================================================================");
//                 }
//
//                 if (exitCode is 0 or (int)FileStatus.ReprocessByFlow)
//                     return;
//
//                 Logger.Instance?.ELog("Error executing runner: Exit code: " + exitCode);
//                 if (Enum.IsDefined(typeof(FileStatus), exitCode))
//                     libFile.Status = (FileStatus)exitCode;
//                 else
//                 {
//                     libFile.Status = FileStatus.ProcessingFailed;
//                     Logger.Instance?.ILog("Invalid exit code, setting file as failed");
//                 }
//
//                 FinishWork(libFile.Uid, node2, libFile);
//             }
//             catch (Exception ex)
//             {
//                 AppendToCompleteLog(completeLog,
//                     "Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace, type: "ERR");
//                 libFile.Status = FileStatus.ProcessingFailed;
//                 FinishWork(libFile.Uid, node2, libFile);
//                 exitCode = (int)FileStatus.ProcessingFailed;
//             }
//         }
//         finally
//         {
//             ExecutingRunners.Remove(libFile.Uid);
//             try
//             {
//                 string dir = Path.Combine(tempPath, "Runner-" + libFile.Uid);
//                 if (keepFiles == false || CurrentConfigurationKeepFailedFlowFiles == false)
//                 {
//                     if (Directory.Exists(dir))
//                     {
//                         Directory.Delete(dir, true);
//                         AppendToCompleteLog(completeLog, "Deleted temporary directory: " + dir);
//                     }
//                 }
//                 else
//                 {
//                     AppendToCompleteLog(completeLog, "Flow failed keeping temporary files in: " + dir);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 AppendToCompleteLog(completeLog, "Failed to clean up runner directory: " + ex.Message, type: "ERR");
//             }
//
//             SaveLog(libFile, completeLog.ToString());
//         }
//
//     }
//     //
//     //
//     // private bool PreExecuteScriptTest(ProcessingNode node)
//     // {
//     //     var script = CurrentConfig?.SystemScripts?.FirstOrDefault(x => node.PreExecuteScript!.Value == x.Uid);
//     //     if (script != null && script.Language != ScriptLanguage.JavaScript)
//     //     {
//     //         StringLogger logger = new();
//     //         var seResult = new ScriptExecutor().Execute(new()
//     //         {
//     //             Code = script.Code,
//     //             TempPath = Path.GetTempPath(),
//     //             Language = script.Language,
//     //             Logger = logger,
//     //             ScriptType = ScriptType.System,
//     //             NotificationCallback = (severity, title, message) =>
//     //             {
//     //                 _ = ServiceLoader.Load<INotificationService>()
//     //                     .Record((NotificationSeverity)severity, title, message);
//     //             } 
//     //         });
//     //         if (seResult.Failed(out var error))
//     //         {
//     //             _ = ServiceLoader.Load<INotificationService>().Record(NotificationSeverity.Warning,
//     //                 $"Failed executing pre-execute script '{script.Name}'",
//     //                 $"Failed to execute on node '{node.Name}': {error}");
//     //             Logger.Instance.WLog("Failed running pre-execute script: " + error + "\n" + logger);
//     //             return false;
//     //         }
//     //         bool preExecuteResult = seResult.Value == 1;
//     //         
//     //         if(preExecuteResult == false)
//     //             Logger.Instance.ILog("Pre-Execute Script Returned False:\n" + logger);
//     //         else
//     //             Logger.Instance.ILog("Pre-Execute Script Successful:\n" + logger);
//     //         
//     //         return preExecuteResult;
//     //     }
//     //     string scriptDir = Path.Combine(GetConfigurationDirectory(), "Scripts");
//     //     string sharedDir = Path.Combine(scriptDir, "Shared");
//     //     string jsFile = Path.Combine(scriptDir, "System", node.PreExecuteScript + ".js");
//     //     if (File.Exists(jsFile) == false)
//     //     {
//     //         Logger.Instance.ELog("Failed to locate pre-execute script: " + node.PreExecuteScript);
//     //         return false;
//     //     }
//     //
//     //     Logger.Instance.ILog("Loading Pre-Execute Script: " + jsFile);
//     //     string code = File.ReadAllText(jsFile);
//     //     if (string.IsNullOrWhiteSpace(code))
//     //     {
//     //         Logger.Instance.ELog("Failed to load pre-execute script code");
//     //         return false;
//     //     }
//     //
//     //     var variableService = ServiceLoader.Load<IVariableService>();
//     //     var variables = variableService.GetAllAsync().Result?.ToDictionary(x => x.Name, x => (object)x.Value) ?? new ();
//     //     variables["FileFlows.Url"] = RemoteService.ServiceBaseUrl;
//     //     var result = ScriptExecutor.Execute(code, variables, sharedDirectory: sharedDir);
//     //     if (result.Success == false)
//     //     {
//     //         Logger.Instance.ELog("Pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
//     //         return false;
//     //     }
//     //     Logger.Instance.ILog("Pre-Execute script returned: " + (result.ReturnValue == null ? "" : System.Text.Json.JsonSerializer.Serialize(result.ReturnValue)));
//     //     
//     //
//     //     if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "exit")
//     //     {
//     //         Logger.Instance.ILog("Exiting");
//     //         return false;
//     //     }
//     //
//     //     if (result.ReturnValue?.ToString()?.ToLowerInvariant() == "restart")
//     //     {
//     //         if (Globals.IsDocker == false)
//     //         {
//     //             Logger.Instance.WLog("Requested restart but not running inside docker");
//     //             return false;
//     //         }
//     //
//     //         if (HasActiveRunners)
//     //         {
//     //             Logger.Instance.WLog("Requested restart, but there are executing runners");
//     //             return false;
//     //         }
//     //         
//     //         Logger.Instance.ILog("Restarting...");
//     //         Thread.Sleep(5_000);
//     //         Environment.Exit(0);
//     //         return false;
//     //     }
//     //     
//     //     if (result.ReturnValue as bool? == false)
//     //     {
//     //         Logger.Instance.ELog("Output from pre-execute script failed: " + result.ReturnValue + "\n" + result.Log);
//     //         return false;
//     //     }
//     //     Logger.Instance.ILog("Pre-execute script passed: \n"+ result.Log.Replace("\\n", "\n"));
//     //     return true;
//     // }
//
// }