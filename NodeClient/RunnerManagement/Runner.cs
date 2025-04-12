using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Acornima.Ast;
using FileFlows.Common;
using FileFlows.Helpers;
using FileFlows.RemoteServices;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.NodeClient;

/// <summary>
/// Represents a runner responsible for executing a task.
/// </summary>
/// <param name="client">the client starting this run</param>
/// <param name="args">The arguments for runner execution.</param>
/// <param name="node">The node this is running on</param>
/// <param name="tempPath">The temp path for this run</param>
/// <param name="onCompleted">The callback to execute when the runner completes, this removes the active runner</param>
public class Runner(Client client, RunFileArguments args, ProcessingNode node, string tempPath, Action<Guid> onCompleted)
{
    public readonly Guid Id = args.LibraryFile.Uid;
    
    StringBuilder runLog = new StringBuilder();
    private int _exitCode;
    private bool _keepFiles;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _runnerTask;
    private bool _isRunning;
    /// <summary>
    /// Gets if the runner is running
    /// </summary>
    public bool IsRunning => _isRunning;
    private bool _aborted = false;
    private Task _updateTask;
    private bool AbortDueToNoOutput;
    readonly TimeSpan noOutputTimeout = TimeSpan.FromSeconds(30);
    DateTime lastOutputTime = DateTime.UtcNow;

    public FlowExecutorInfo Info { get; set; } = new()
    {
        LibraryFile = args.LibraryFile,
        NodeUid = node.Uid,
        NodeName = node.Name,
        StartedAt = DateTime.UtcNow,
        CurrentPartName = "Startup"
    };

    /// <summary>
    /// Gets or sets if the file has finished processing.
    /// I.e. it has told th server it is finished
    /// </summary>
    public bool FinishedProcessing { get; set; }

    /// <summary>
    /// Starts execution of the runner.
    /// </summary>
    public void Start(LibraryFile lf)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = true;
        _runnerTask = StartRunnerAsync(lf);
    }

    private async Task StartRunnerAsync(LibraryFile lf)
    {
        StartUpdateTimer();
        try
        {
            lf = await Execute(lf, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"[{lf.Uid}]: Error in file: " + ex);
        }

        StopUpdateTimer();

        try
        {
            string dir = Path.Combine(tempPath, "Runner-" + args.LibraryFile.Uid);
            if (args.KeepFailedFiles == false && _keepFiles == false)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    AppendToRunLog("Deleted temporary directory: " + dir);
                }
            }
            else
            {
                AppendToRunLog("Flow failed keeping temporary files in: " + dir);
            }
        }
        catch (Exception ex)
        {
            AppendToRunLog("Failed to clean up runner directory: " + ex.Message, type: "ERR");
        }

        try
        {
            // Finish the file processing.
            AppendToRunLog("Finishing file: " + lf.Status);
            FinishedProcessing = true;
            await client.FileFinishProcessing(lf, runLog.ToString());
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"[{lf.Uid}] Failed to notify server of file finishing:{lf.Name}\n{ex}");
        }
        finally
        {
            Logger.Instance.ILog($"[{lf.Uid}] Finishing Runner: " + lf.Status + " : " + lf.Name);
            onCompleted(Id);
            _isRunning = false;
        }
    }

    private async Task<LibraryFile> Execute(LibraryFile libFile, CancellationToken ctx)
    {
        var cfgService = ServiceLoader.Load<ConfigurationService>();

        bool isServer = node.Uid == CommonVariables.InternalNodeUid;
        var node2 = node;

        var flow = cfgService.CurrentConfig?.Flows.FirstOrDefault(x => x.Uid == args.FlowUid);
        if (flow == null)
        {
            libFile.Status = FileStatus.ProcessingFailed;
            libFile.FailureReason = "Flow not found";
            return libFile;
        }

        var runnerParameters = new RunnerParameters()
        {
            LibraryFile = libFile,
            Flow = flow
        };
        runnerParameters.Uid = libFile.Uid;
        runnerParameters.MaxFlowParts = cfgService.CurrentConfig?.MaxNodes ?? 30;
        runnerParameters.NodeUid = node2!.Uid;
        runnerParameters.TempPath = tempPath;
        runnerParameters.WorkingDirectory = Path.Combine(tempPath, "Runner-" + args.LibraryFile.Uid);
        if (CreateWorkingDirectory(runnerParameters.WorkingDirectory) == false)
        {
            libFile.Status = FileStatus.ProcessingFailed;
            return libFile;
        }

        runnerParameters.ConfigPath = cfgService.GetConfigurationDirectory();
        runnerParameters.BaseUrl = RemoteService.ServiceBaseUrl;
        runnerParameters.AccessToken = RemoteService.AccessToken;
        runnerParameters.RemoteNodeUid = RemoteService.NodeUid;
        runnerParameters.IsDocker = Globals.IsDocker;
        runnerParameters.IsInternalServerNode = isServer;
        runnerParameters.Hostname = isServer ? null : node.Name;

        bool debugMode = false;
#if(DEBUG)
        debugMode = true;
        runnerParameters.RunnerTempPath = "ff-debug-mode";
#endif

        runLog.AppendLine(
            "==============================================================================" +
            Environment.NewLine +
            "===                      PROCESSING NODE OUTPUT START                      ===" +
            Environment.NewLine +
            "==============================================================================");
        await client.FileLogAppend(libFile.Uid, runLog.ToString(), true);
        
        var logSemaphore = new SemaphoreSlim(1, 1);
        using JsonRpcServer rpcServer = new(client, runnerParameters, (message) =>
        {
            if (string.IsNullOrEmpty(message))
                return;
            #if(DEBUG)
            Console.WriteLine("Runner: " + message);
            Logger.Instance.ILog("Runner: " + message);
            #endif
            runLog.AppendLine(message);
            if (debugMode)
            {
                _ = Task.Run(async () =>
                {
                    if (await logSemaphore.WaitAsync(TimeSpan.FromSeconds(1), ctx)) // Avoid deadlocks
                    {
                        try
                        {
                            await client.FileLogAppend(libFile.Uid, message);
                        }
                        finally
                        {
                            logSemaphore.Release();
                        }
                    }
                }, ctx);
            }
        });

        rpcServer.Start();
        
        try
        {
            #if(DEBUG)
            // Adjust this as per your project's target framework
            string workingDirectory = Path.Combine(Directory.GetCurrentDirectory().Replace("Server", "FlowRunner"), "bin", "Debug", "net8.0");
            #else
            // Determine the correct directory based on the environment
            string workingDirectory = DirectoryHelper.FlowRunnerDirectory;
            #endif
            if (debugMode)
            {
#if(DEBUG)
                _exitCode = (int)FlowRunner.Program.RunInternal(rpcServer.PipeName);
                libFile = rpcServer.GetProcessedFile();
                libFile.Status = (FileStatus)_exitCode;
                return libFile;
#endif
            }

            // Start process
            using Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = GetDotnetLocation(),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
            process.StartInfo.ArgumentList.Add(rpcServer.PipeName);
            // If in debug mode, add a flag to signal FlowRunner to wait for a debugger
            if (debugMode)
            {
                process.StartInfo.ArgumentList.Add("--debug");
            }
            
            
            // Attach event handlers to capture output
            process.OutputDataReceived += async (sender, args) =>
            {
                if (args.Data == null)
                    return;
                lastOutputTime = DateTime.UtcNow; // Reset timeout timer
                
                if (args.Data.Trim().StartsWith("Heartbeat: "))
                    return; // Ignore heartbeats in logging
                
                Console.WriteLine($"[{libFile.Uid}]: " + args.Data); // Write error to the console
                runLog.AppendLine(args.Data);

                if (await logSemaphore.WaitAsync(TimeSpan.FromSeconds(1))) // Avoid deadlocks
                {
                    try
                    {
                        await client.FileLogAppend(libFile.Uid, args.Data);
                    }
                    finally
                    {
                        logSemaphore.Release();
                    }
                }
            };
            process.ErrorDataReceived += async (sender, args) =>
            {
                if (args.Data == null)
                    return;
                lastOutputTime = DateTime.UtcNow; // Reset timeout timer
                
                await Console.Error.WriteLineAsync($"[{libFile.Uid}]: " + args.Data); // Write error to the console
                runLog.AppendLine(args.Data);

                if (await logSemaphore.WaitAsync(TimeSpan.FromSeconds(1))) // Avoid deadlocks
                {
                    try
                    {
                        await client.FileLogAppend(libFile.Uid, args.Data);
                    }
                    finally
                    {
                        logSemaphore.Release();
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            lastOutputTime = DateTime.UtcNow;
            
            // Wait for the cancellation token to be triggered (abort request)
            var abortCancellationTask = WaitForAbortAsync(ctx, rpcServer, process);

            // Continue the process while it's running or until cancellation happens
            var processExitTask = process.WaitForExitAsync();

            // Wait for either the process to exit or the cancellation to occur
            await Task.WhenAny(processExitTask, abortCancellationTask);

            // If the cancellation task completes, attempt graceful shutdown
            if (abortCancellationTask.IsCompleted)
            {
                // Gracefully attempt to abort the process
                var taskDelay = Task.Delay(TimeSpan.FromSeconds(20));
                var completedTask = await Task.WhenAny(processExitTask, taskDelay);

                // If the taskDelay completes first, kill the process
                if (completedTask == taskDelay)
                {
                    process.Kill(); // Kill process if not exiting gracefully after 20 seconds
                    runLog.AppendLine("Process killed after 20 seconds due to failure to exit gracefully.");
                }

                // Ensure the process exits
                await processExitTask;
            }

            _exitCode = process.ExitCode;

            // After process exits, handle result
            var lf = rpcServer.GetProcessedFile();

            if (_aborted && lf.Status != FileStatus.Processed)
            {
                lf.Status = FileStatus.ProcessingFailed;
                lf.FailureReason = "Aborted by user";
            }
            else
            {
                if (lf.Status == FileStatus.Processing)
                    lf.Status = (FileStatus)_exitCode;
                if (lf.Status == FileStatus.Processing || lf.Status == FileStatus.Unprocessed)
                    lf.Status = FileStatus.ProcessingFailed;
            }

            
            if (Enum.IsDefined(typeof(FileStatus), _exitCode))
            {
                Console.WriteLine($"[{libFile.Uid}]: _exitCode {_exitCode} is a valid FileStatus value.");
            }
            else
            {
                lf.Status = FileStatus.ProcessingFailed;
                if (AbortDueToNoOutput)
                    lf.FailureReason = $"Process terminated due to no output received in {noOutputTimeout}.";
                else
                    lf.FailureReason = $"Unexpected exit code: {_exitCode}";
            }
            
            return lf;
        }
        catch (Exception ex)
        {
            runLog.AppendLine($"Error: {ex.Message}");
            libFile.Status = FileStatus.ProcessingFailed;
            return libFile;
        }

    }

    // This method will be called when the cancellation token is triggered
    private async Task WaitForAbortAsync(CancellationToken ctx, JsonRpcServer rpcServer, Process process)
    {
        while (!process.HasExited)
        {
            await Task.Delay(5000, ctx).ConfigureAwait(false); // Check every 5 seconds

            if (DateTime.UtcNow - lastOutputTime > noOutputTimeout)
            {
                AbortDueToNoOutput = true;
                runLog.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERRR] -> Process terminated due to no output received in {noOutputTimeout}.");
                rpcServer.Abort();
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        await Task.Delay(5000).ConfigureAwait(false); // Ensure the process exits
                    }
                }
                catch (Exception ex)
                {
                    runLog.AppendLine($"Error terminating process: {ex.Message}");
                }
                return;
            }

            if (ctx.IsCancellationRequested)
            {
                runLog.AppendLine("Abort triggered.");
                rpcServer.Abort();
                return;
            }
        }
    }


    /// <summary>
    /// Creates the runners working directory
    /// </summary>
    /// <param name="dir">the path of the directory to create</param>
    /// <returns>true if successful, otherwise false</returns>
    private bool CreateWorkingDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }

            return true;
        }
        catch (Exception)
        {
            // this can throw if mapping inside a docker container is not valid, or the mapped location has become unavailable

            if (Globals.IsDocker)
            {
                runLog.AppendLine(
"""
==========================================================================================
Failed to create working directory, this is likely caused by the mapped '/temp' directory is missing or has become unavailable from the host machine
==========================================================================================
""".Trim()
                );
            }

            return false;
        }
    }

    /// <summary>
    /// Adds a message to the run log with a formatted date
    /// </summary>
    /// <param name="message">the message to add</param>
    /// <param name="type">the message type</param>
    private void AppendToRunLog(string message, string type = "INFO")
        => runLog.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} [{type}] -> {message}");

    private static string? Dotnet;
    
     /// <summary>
     /// Gets the location of dotnet to use to start the flow runner
     /// </summary>
     /// <returns>the location of dotnet to use to start the flow runner</returns>
     private string GetDotnetLocation()
     {
         if(string.IsNullOrEmpty(Dotnet))
         {
             if (Globals.IsWindows == false && File.Exists("/dotnet/dotnet"))
                 Dotnet = "/dotnet/dotnet"; // location of docker
             else if (Globals.IsWindows == false && File.Exists("/root/.dotnet/dotnet"))
                 Dotnet = "/root/.dotnet/dotnet"; // location of legacy docker
             else
             {
                 #if(DEBUG)
                 return "/usr/bin/dotnet";
                 #endif
                 Dotnet = "dotnet"; // assume in PATH
             }
         }
         return Dotnet;
     }

     /// <summary>
     /// Aborts the runner
     /// </summary>
     public async Task Abort()
     {
         Info.Aborted = true;
         _aborted = true;
         StopUpdateTimer();
         // Abort the run
         _cancellationTokenSource?.Cancel();
         
         // Wait for the runner to finish if it's still running
         while (_isRunning)
         {
             await Task.Delay(100);  // You can adjust the delay as needed to avoid tight looping
         }
     }
     
     
     /// <summary>
     /// Starts a timer that updates the file processing status every 20 seconds.
     /// </summary>
     private void StartUpdateTimer()
     {
         if (_cancellationTokenSource == null)
             return;
         
         _updateTask = Task.Run(async () =>
         {
             while (!_cancellationTokenSource.Token.IsCancellationRequested)
             {
                 try
                 {
                     await Task.Delay(TimeSpan.FromSeconds(20), _cancellationTokenSource.Token);
                     client.TriggerStatusUpdate();
                 }
                 catch (OperationCanceledException)
                 {
                     // Expected when cancellation is requested, just exit
                     break;
                 }
                 catch (Exception)
                 {
                     // Ignored
                 }
             }
         }, _cancellationTokenSource.Token);
     }

     /// <summary>
     /// Stops the update timer and ensures it exits cleanly.
     /// </summary>
     private async void StopUpdateTimer()
     {
         if (_updateTask == null)
             return;

         _cancellationTokenSource?.Cancel();
         try
         {
             await _updateTask;
         }
         catch (OperationCanceledException)
         {
             // Expected on cancellation
         }
         _updateTask = null;
     }
}
