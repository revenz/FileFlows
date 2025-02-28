using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
public class Runner
{
    public Guid Id { get; } = Guid.NewGuid();
    private readonly RunFileArguments _args;
    private readonly Action<Guid> _onCompleted;
    StringBuilder completeLog = new StringBuilder();
    private int _exitCode;
    private string _tempPath;
    private bool _keepFiles;
    private ProcessingNode _node;

    /// <summary>
    /// Initializes a new instance of the <see cref="Runner"/> class.
    /// </summary>
    /// <param name="args">The arguments for runner execution.</param>
    /// <param name="onCompleted">The callback to execute when the runner completes.</param>
    public Runner(RunFileArguments args, ProcessingNode node, string tempPath, Action<Guid> onCompleted)
    {
        _args = args;
        _node = node;
        _tempPath = tempPath;
        _onCompleted = onCompleted;
    }

    /// <summary>
    /// Starts execution of the runner.
    /// </summary>
    public void Start()
    {
        Task.Run(() =>
        {
            try
            {
                //using JsonRpcServer rpcServer = new();
                Execute();
            }
            finally
            {
                try
                {
                    string dir = Path.Combine(_tempPath, "Runner-" + _args.LibraryFile.Uid);
                    if (_args.KeepFailedFiles == false && _keepFiles == false)
                    {
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                            AppendToCompleteLog("Deleted temporary directory: " + dir);
                        }
                    }
                    else
                    {
                        AppendToCompleteLog("Flow failed keeping temporary files in: " + dir);
                    }
                }
                catch (Exception ex)
                {
                    AppendToCompleteLog("Failed to clean up runner directory: " + ex.Message, type: "ERR");
                }
                
                _onCompleted(Id);
            }
        });
    }

    /// <summary>
    /// Executes the runner task
    /// </summary>
    private void Execute()
    {
        var cfgService = ServiceLoader.Load<ConfigurationService>();

        var libFile = _args.LibraryFile;

        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isServer = _node.Uid == CommonVariables.InternalNodeUid;
        var node2 = _node;

        var runnerParameters = new RunnerParameters();
        runnerParameters.Uid = libFile.Uid;
        runnerParameters.NodeUid = node2!.Uid;
        runnerParameters.LibraryFile = libFile.Uid;
        runnerParameters.TempPath = _tempPath;
        runnerParameters.ConfigPath = cfgService.GetConfigurationDirectory();
        runnerParameters.ConfigKey =
            cfgService.GetConfigNoEncrypt(node2) ? "NO_ENCRYPT" : cfgService.GetConfigKey(node2);
        runnerParameters.BaseUrl = RemoteService.ServiceBaseUrl;
        runnerParameters.AccessToken = RemoteService.AccessToken;
        runnerParameters.RemoteNodeUid = RemoteService.NodeUid;
        runnerParameters.IsDocker = Globals.IsDocker;
        runnerParameters.IsInternalServerNode = isServer;
        runnerParameters.Hostname = isServer ? null : _node.Name;
#if(DEBUG)
        runnerParameters.RunnerTempPath = "ff-debug-mode";
#endif
        string json = JsonSerializer.Serialize(runnerParameters);
        string randomString = new string(Enumerable
            .Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 20)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());
        string encrypted = FileFlows.ServerShared.Helpers.Decrypter.Encrypt(json,
            "hVYjHrWvtEq8huShjTkA" + randomString + "oZf4GW3jJtjuNHlMNpl9");
        var parameters = new[] { encrypted, randomString };
        string workingDir = Path.Combine(_tempPath, "Runner-" + libFile.Uid);
#pragma warning restore CS8601 // Possible null reference assignment.

        try
        {
#if (DEBUG)
            (_exitCode, string output) = FlowRunner.Program.RunWithLog(parameters);
            string error = string.Empty;
#else
            using Process process = new Process();
        
            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = GetDotnetLocation();
            process.StartInfo.WorkingDirectory = DirectoryHelper.FlowRunnerDirectory;
            process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
            foreach (var str in parameters)
                process.StartInfo.ArgumentList.Add(str);

            Logger.Instance?.ILog("Executing: " + process.StartInfo.FileName + " " + String.Join(" ", process.StartInfo.ArgumentList.Select(x => "\"" + x + "\"")));
            Logger.Instance?.ILog("Working Directory: " + process.StartInfo.WorkingDirectory);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            _exitCode = process.ExitCode;

#endif
            if (_exitCode == 100)
            {
                _exitCode = 0; // special case
                _keepFiles = true;
            }

            if (string.IsNullOrEmpty(output) == false)
            {
                completeLog.AppendLine(
                    "==============================================================================" +
                    Environment.NewLine +
                    "===                      PROCESSING NODE OUTPUT START                      ===" +
                    Environment.NewLine +
                    "==============================================================================" +
                    Environment.NewLine +
                    output + Environment.NewLine +
                    "==============================================================================" +
                    Environment.NewLine +
                    "===                       PROCESSING NODE OUTPUT END                       ===" +
                    Environment.NewLine +
                    "==============================================================================");
            }

            if (string.IsNullOrEmpty(error) == false)
            {
                completeLog.AppendLine(
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
                return;

            Logger.Instance?.ELog("Error executing runner: Exit code: " + _exitCode);
            if (Enum.IsDefined(typeof(FileStatus), _exitCode))
                libFile.Status = (FileStatus)_exitCode;
            else
            {
                libFile.Status = FileStatus.ProcessingFailed;
                Logger.Instance?.ILog("Invalid exit code, setting file as failed");
            }
        }
        catch (Exception ex)
        {
            AppendToCompleteLog(
                "Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace, type: "ERR");
            libFile.Status = FileStatus.ProcessingFailed;
            _exitCode = (int)FileStatus.ProcessingFailed;
        }
    }

    /// <summary>
    /// Adds a message to the complete log with a formatted date
    /// </summary>
    /// <param name="completeLog">the complete log</param>
    /// <param name="message">the message to add</param>
    private void AppendToCompleteLog(string message, string type = "INFO")
        => completeLog.AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} [{type}] -> {message}");

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
