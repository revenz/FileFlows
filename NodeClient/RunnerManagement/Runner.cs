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


    /// <summary>
    /// Starts execution of the runner.
    /// </summary>
    public void Start()
    {
        _ = Task.Run(async () =>
        {
            // move the file as Processing
            var lf = args.LibraryFile;
            lf.Status = FileStatus.Processing;
            await client.FileStartProcessing(lf.Uid);
            try
            {
                lf = Execute();
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

    /// <summary>
    /// Executes the runner task
    /// </summary>
    private LibraryFile Execute()
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
        
        using JsonRpcServer rpcServer = new(client, runnerParameters, (message) =>
        {
            if(string.IsNullOrEmpty(message) == false)
                runLog.AppendLine(message);
        });
        
        rpcServer.Start();
        try
        {
            string error = string.Empty;
            if (debugMode)
            {
                _exitCode = (int)FlowRunner.Program.RunInternal(rpcServer.PipeName).Result;
            }
            else
            {
                using Process process = new Process();

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = GetDotnetLocation(),
                    WorkingDirectory = DirectoryHelper.FlowRunnerDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Add arguments
                process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
                process.StartInfo.ArgumentList.Add(rpcServer.PipeName);

                Logger.Instance?.ILog("Executing: " + process.StartInfo.FileName + " " + 
                                      String.Join(" ", process.StartInfo.ArgumentList.Select(x => "\"" + x + "\"")));
                Logger.Instance?.ILog("Working Directory: " + process.StartInfo.WorkingDirectory);

                // Capture output asynchronously
                process.OutputDataReceived += (sender, e) => 
                { 
                    if (string.IsNullOrEmpty(e.Data) == false) 
                        runLog.AppendLine(e.Data); 
                };

                StringBuilder errorOutput = new StringBuilder();
                process.ErrorDataReceived += (sender, e) => 
                { 
                    if (string.IsNullOrEmpty(e.Data) == false) 
                        errorOutput.AppendLine(e.Data); 
                };
                error = errorOutput.ToString();

                process.Start();

                // Begin async read operations
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process exit
                process.WaitForExit();

                _exitCode = process.ExitCode;
            }
            if (_exitCode == 100)
            {
                _exitCode = 0; // special case
                _keepFiles = true;
            }

            runLog.AppendLine(
                "==============================================================================" +
                Environment.NewLine +
                "===                       PROCESSING NODE OUTPUT END                       ===" +
                Environment.NewLine +
                "==============================================================================");

            if (string.IsNullOrEmpty(error) == false)
            {
                runLog.AppendLine(
                    "==============================================================================" +
                    Environment.NewLine +
                    "===                   PROCESSING NODE ERROR OUTPUT START                   ===" +
                    Environment.NewLine +
                    "==============================================================================" +
                    Environment.NewLine +
                    error + Environment.NewLine +
                    "==============================================================================" +
                    Environment.NewLine +
                    "===                    PROCESSING NODE ERROR OUTPUT END                    ===" +
                    Environment.NewLine +
                    "==============================================================================");
            }

            if (_exitCode is 0 or (int)FileStatus.ReprocessByFlow)
                return rpcServer.GetProcessedFile();

            runLog.AppendLine("Error executing runner: Exit code: " + _exitCode);
            if (Enum.IsDefined(typeof(FileStatus), _exitCode))
                libFile.Status = (FileStatus)_exitCode;
            else
            {
                libFile.Status = FileStatus.ProcessingFailed;
                runLog.AppendLine("Invalid exit code, setting file as failed");
            }
        }
        catch (Exception ex)
        {
            AppendToRunLog(
                "Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace, type: "ERR");
            libFile.Status = FileStatus.ProcessingFailed;
            _exitCode = (int)FileStatus.ProcessingFailed;
        }
        finally
        {
            rpcServer.Stop();
        }

        return rpcServer.GetProcessedFile();
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
                 Dotnet = "dotnet";// assume in PATH
         }
         return Dotnet;
     }
}
