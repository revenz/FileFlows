using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
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
/// <param name="onCompleted">The callback to execute when the runner completes.</param>
public class Runner(Client client, RunFileArguments args, ProcessingNode node, string tempPath, Action<Guid> onCompleted)
{
    public Guid Id { get; } = Guid.NewGuid();
    StringBuilder runLog = new StringBuilder();
    private int _exitCode;
    private bool _keepFiles;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _runnerTask;


    /// <summary>
    /// Starts execution of the runner.
    /// </summary>
    public void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            // move the file as Processing
            var lf = args.LibraryFile;
            lf.Status = FileStatus.Processing;
            await client.FileStartProcessing(lf.Uid);
            try
            {
                lf = await Execute(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error in file: " + ex);
            }
            finally
            {
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
                
                // Finish the file processing.
                await client.FileFinishProcessing(lf, runLog.ToString());
                
                onCompleted(Id);
            }
        });
    }

    private async Task<LibraryFile> Execute(CancellationToken ctx)
    {
        var cfgService = ServiceLoader.Load<ConfigurationService>();
        var libFile = args.LibraryFile;

        bool isServer = node.Uid == CommonVariables.InternalNodeUid;
        var node2 = node;

        libFile.ExecutedNodes = [];
        libFile.NodeName = node.Name;
        libFile.NodeUid = node.Uid;
        libFile.Node = new()
        {
            Name = node.Name,
            Uid = node.Uid,
            Type = node.GetType().FullName!
        };

        var flow = cfgService.CurrentConfig?.Flows.FirstOrDefault(x => x.Uid == args.FlowUid);
        if (flow == null)
        {
            libFile.Status = FileStatus.FlowNotFound;
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
            if (string.IsNullOrEmpty(message) == false)
                runLog.AppendLine(message);
            _ = Task.Run(async () =>
            {
                await logSemaphore.WaitAsync(ctx);
                try
                {
                    await client.FileLogAppend(libFile.Uid, message);
                }
                finally
                {
                    logSemaphore.Release();
                }
            }, ctx);
        });

        rpcServer.Start();

        try
        {
            // Determine the correct directory based on the environment
            string workingDirectory = DirectoryHelper.FlowRunnerDirectory;
            if (debugMode)
            {
                // Adjust this as per your project's target framework
                workingDirectory = Path.Combine(Directory.GetCurrentDirectory().Replace("Server", "FlowRunner"), "bin", "Debug", "net8.0");
#if(DEBUG)
                // _exitCode = (int)FlowRunner.Program.RunInternal(rpcServer.PipeName);
                // libFile = rpcServer.GetProcessedFile();
                // libFile.Status = (FileStatus)_exitCode;
                // return libFile;
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
                
                // Attach event handlers to capture output
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.WriteLine(args.Data); // Write to the console
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.Error.WriteLine(args.Data); // Write error to the console
                    }
                };
            }

            process.Start();

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

            // After process exits, handle result
            var lf = rpcServer.GetProcessedFile();
            if(lf.Status == FileStatus.Processing)
                lf.Status = (FileStatus)_exitCode;
            if (lf.Status == FileStatus.Processing || lf.Status == FileStatus.Unprocessed)
                lf.Status = FileStatus.ProcessingFailed;
            
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
        await Task.WhenAny(Task.Delay(Timeout.Infinite, ctx), process.WaitForExitAsync());
        if (ctx.IsCancellationRequested)
        {
            runLog.AppendLine("Abort triggered.");
            rpcServer.Abort(); // Attempt graceful shutdown via the rpc server
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
     public void Abort()
     {
         // Abort the run
         _cancellationTokenSource?.Cancel();
     }
}
