using System.Text.RegularExpressions;

namespace FileFlows.Plugin;

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The result of a process execution
/// </summary>
public struct ProcessResult
{
    /// <summary>
    /// If the processed completed, or if it was aborted
    /// </summary>
    public bool Completed;
    /// <summary>
    /// The exit code of the process
    /// </summary>
    public int? ExitCode;
    /// <summary>
    /// The complete output, standard and error outputs of the process as the messages were received
    /// </summary>
    public string Output;

    /// <summary>
    /// The standard output from the process
    /// </summary>
    public string StandardOutput;
    /// <summary>
    /// The error output from the process
    /// </summary>
    public string StandardError;
}

/// <summary>
/// Basic arguments used to execute a process
/// </summary>
public class ExecuteBasicArgs
{
    /// <summary>
    /// Gets or sets the command to execute
    /// </summary>
    public string Command { get; set; }
    /// <summary>
    /// Gets or sets the arguments of the command
    /// </summary>
    public string Arguments { get; set; }
    /// <summary>
    /// Gets or sets the arguments of the command as a list and will be correctly escaped
    /// </summary>
    public string[] ArgumentList { get; set; }
    /// <summary>
    /// Gets or sets the timeout in seconds of the process
    /// </summary>
    public int Timeout { get; set; }
    /// <summary>
    /// Gets or sets the working directory of the process
    /// </summary>
    public string WorkingDirectory { get; set; }
}

/// <summary>
/// Arguments used to execute a process
/// </summary>
public class ExecuteArgs
{
    /// <summary>
    /// Gets or sets the command to execute
    /// </summary>
    public string Command { get; set; }
    /// <summary>
    /// Gets or sets the arguments of the command
    /// </summary>
    public string Arguments { get; set; }
    /// <summary>
    /// Gets or sets the arguments of the command as a list and will be correctly escaped
    /// </summary>
    public string[] ArgumentList { get; set; }
    /// <summary>
    /// Gets or sets the timeout in seconds of the process
    /// </summary>
    public int Timeout { get; set; }
    /// <summary>
    /// When silent, nothing will be logged
    /// </summary>
    public bool Silent { get; set; }
    /// <summary>
    /// Gets or sets the working directory of the process
    /// </summary>
    public string WorkingDirectory { get; set; }
    /// <summary>
    /// A delegate that is used when output is received from an executing process
    /// </summary>
    public delegate void OutputRecievedEvent(string output);
    
    /// <summary>
    /// An event that is called when there is standard output from a process
    /// </summary>
    public event OutputRecievedEvent StandardOutput;
    
    /// <summary>
    /// An event that is called when there is standard output from a process
    /// </summary>
    public event Action<string> Output;
    
    /// <summary>
    /// An event that is called when there is error output from a process
    /// </summary>
    public event Action<string> Error;
    
    /// <summary>
    /// An event that is called when there is error output from a process
    /// </summary>
    public event OutputRecievedEvent ErrorOutput;

    /// <summary>
    /// Optional extra variables to pass into the process
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = [];

    /// <summary>
    /// Gets environmental variables to set on the process
    /// </summary>
    public Dictionary<string, string> EnvironmentalVariables { get; init; } = new ();

    /// <summary>
    /// Called when there is standard output received and invokes the StandardOutput event
    /// </summary>
    /// <param name="output">the output string received</param>\
    internal void OnStandardOutput(string output)
    {
        StandardOutput?.Invoke(output);
        Output?.Invoke(output);

    }

    /// <summary>
    /// Called when there is error output received and invokes the ErrorOutput event
    /// </summary>
    /// <param name="output">the error string received</param>
    internal void OnErrorOutput(string output)
    {
        ErrorOutput?.Invoke(output);
        Error?.Invoke(output);
    }
}

/// <summary>
/// A helper class that handles executing processes and reading their output
/// </summary>
public interface IProcessHelper
{
    /// <summary>
    /// Occurs when standard output receives a new line.
    /// </summary>
    event Action<string> OnStandardOutputReceived;

    /// <summary>
    /// Occurs when standard error receives a new line.
    /// </summary>
    event Action<string> OnErrorOutputReceived;
    
    /// <summary>
    /// Executes a shell command
    /// </summary>
    /// <param name="args">the arguments of the shell command</param>
    /// <returns>the processing result of the executed command</returns>
    Task<ProcessResult> ExecuteShellCommand(ExecuteArgs args);
}


/// <summary>
/// A helper class to execute shell commands and capture their output in real time.
/// </summary>
public class ProcessHelper : IProcessHelper
{
    private Process? process;
    private readonly ILogger Logger;
    private ExecuteArgs Args;

    private StringBuilder standardOutputBuilder, errorBuilder, outputBuilder;

    private bool Fake;
    private CancellationToken _cancellationToken;

    /// <summary>
    /// Occurs when standard output receives a new line.
    /// </summary>
    public event Action<string> OnStandardOutputReceived;

    /// <summary>
    /// Occurs when standard error receives a new line.
    /// </summary>
    public event Action<string> OnErrorOutputReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger used for logging process execution details.</param>
    /// <param name="cancellationToken">The cancellation token to allow stopping the process.</param>
    /// <param name="fake">Indicates if this is a fake process helper (used for testing).</param>
    public ProcessHelper(ILogger logger, CancellationToken cancellationToken, bool fake)
    {
        this.Logger = logger;
        this.Fake = fake;
        _cancellationToken = cancellationToken;
        cancellationToken.Register(Kill);
    }

    /// <summary>
    /// Kills the process if its running
    /// </summary>
    public void Kill()
    {
        Logger.WLog("Process Helper received the abort command");
        if (this.process != null)
        {
            Logger.WLog("Killing actively running process");
            this.process.Kill();
            Logger.WLog("Process killed");
            this.process = null;
        }
    }

    /// <summary>
    /// Executes a shell command and returns the process result.
    /// </summary>
    /// <param name="args">The arguments for the shell command.</param>
    /// <returns>A <see cref="ProcessResult"/> containing execution details.</returns>
    public async Task<ProcessResult> ExecuteShellCommand(ExecuteArgs args)
    {
        if (Fake) return new ProcessResult();

        var result = new ProcessResult();
        this.Args = args;

        using var process = new Process();
        this.process = process;

        process.StartInfo.FileName = args.Command;
        if (args.ArgumentList?.Any() == true)
        {
            args.Arguments = string.Empty;
            foreach (var arg in args.ArgumentList)
            {
                process.StartInfo.ArgumentList.Add(arg);
                args.Arguments += arg.Contains(' ') ? $"\"{arg}\" " : $"{arg} ";
            }

            args.Arguments = args.Arguments.Trim();
        }
        else if (!string.IsNullOrEmpty(args.Arguments))
        {
            process.StartInfo.Arguments = args.Arguments;
        }

        if (!string.IsNullOrEmpty(args.WorkingDirectory))
            process.StartInfo.WorkingDirectory = args.WorkingDirectory;

        if (args.EnvironmentalVariables != null && args.EnvironmentalVariables.Count > 0)
        {
            foreach (var ev in args.EnvironmentalVariables)
                process.StartInfo.EnvironmentVariables[ev.Key] = ev.Value;
        }

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.EnableRaisingEvents = true;

        if (OperatingSystem.IsWindows() && args.Variables != null && args.Variables.TryGetValue("Infinity", out var oInfinity))
        {
            try
            {
                long affinityMask = oInfinity switch
                {
                    int i => i,
                    long l => l,
                    string s when long.TryParse(s, out var parsed) => parsed,
                    _ => -1
                };

                if (affinityMask > 0)
                {
                    Logger.ILog($"Using Infinity '{affinityMask}' for process");
                    process.ProcessorAffinity = (IntPtr)affinityMask;
                }
            }
            catch (Exception ex)
            {
                // log or ignore as needed
                Logger.WLog($"Failed getting infinity: {ex}");
            }
        }

        if (!args.Silent)
        {
            Logger?.ILog(new string('-', 70));
            Logger?.ILog($"Executing: {args.Command} {args.Arguments}");
            if (!string.IsNullOrEmpty(args.WorkingDirectory))
                Logger?.ILog($"Working Directory: {args.WorkingDirectory}");
            Logger?.ILog(new string('-', 70));
        }

        var tcs = new TaskCompletionSource<int>();

        process.Exited += (s, e) => { tcs.TrySetResult(process.ExitCode); };

        outputBuilder = new();
        standardOutputBuilder = new();
        errorBuilder = new();

        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived += OnErrorDataReceived;

        bool isStarted;
        try
        {
            isStarted = process.Start();
        }
        catch (Exception error)
        {
            result.Completed = true;
            result.ExitCode = -1;
            result.Output = error.Message;
            this.process = null;
            return result;
        }

        if (isStarted == false)
        {
            this.process = null;
            return result;
        }


        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource linkedCts;

        if (args.Timeout > 0)
        {
            timeoutCts = new CancellationTokenSource(args.Timeout * 1000);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, timeoutCts.Token);
        }
        else
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        }


        try
        {
            var cancellationTask = Task.Delay(Timeout.Infinite, linkedCts.Token);

            var completedTask = await Task.WhenAny(tcs.Task, cancellationTask);
            if (completedTask == tcs.Task)
            {
                // ⬇️ Ensure all output has finished being read
                await process.WaitForExitAsync();
                process.CancelOutputRead();
                process.CancelErrorRead();

                result.Completed = true;
                result.ExitCode = process.ExitCode;
            }
            else
            {
                result.Completed = false;
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true); // Kill the process and any children
                    }
                }
                catch (Exception ex)
                {
                    Logger.WLog("Error killing process: " + ex.Message);
                }
            }
            result.StandardError = errorBuilder.ToString();
            result.StandardOutput = standardOutputBuilder.ToString();
            result.Output = outputBuilder.ToString().EmptyAsNull() ?? result.StandardError;
        }
        finally
        {
            timeoutCts?.Dispose();
            linkedCts?.Dispose();
        }

        this.process = null;
        return result;
    }

    private string ProcessLastOutputLine;

    /// <summary>
    /// Handles standard output received from the process.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        
        string line = RemoveAnsiCodes(e.Data);
        OnStandardOutputReceived?.Invoke(line);
        Args?.OnStandardOutput(line);
        if (ProcessLastOutputLine != line)
        {
            if (Args?.Silent != true)
                Logger?.Raw(line);
            outputBuilder.AppendLine(line);
            standardOutputBuilder.AppendLine(line);
        }
        ProcessLastOutputLine = line;
        
    }

    /// <summary>
    /// Handles error output received from the process.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
    
        string line = RemoveAnsiCodes(e.Data);
        OnErrorOutputReceived?.Invoke(line);
        Args?.OnErrorOutput(line);
        if (ProcessLastOutputLine != line)
        {
            if (Args?.Silent != true)
                Logger?.Raw(line);
            outputBuilder.AppendLine(line);
            errorBuilder.AppendLine(line);
        }
        ProcessLastOutputLine = line;
    }

    /// <summary>
    /// Waits for a process to exit or timeout.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the process exited, false if it was canceled or timed out.</returns>
    private static async Task<bool> WaitForExitAsync(Process process, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        var finishTask = process.WaitForExitAsync();

        // If timeout is zero, create a "forever" task that can be canceled via cancellationToken
        var delayTask = timeoutMilliseconds > 0 
            ? Task.Delay(timeoutMilliseconds, cancellationToken) 
            : Task.Delay(Timeout.Infinite, cancellationToken);

        // Wait for either the process to exit or the timeout (if timeout is non-zero)
        var completedTask = await Task.WhenAny(finishTask, delayTask);

        // If the process has exited, we return true, otherwise false for timeout or cancellation
        return completedTask == finishTask && process.HasExited;
    }

    /// <summary>
    /// Removes ANSI escape codes from a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The cleaned string.</returns>
    private static string RemoveAnsiCodes(string input)
    {
        return new Regex(@"(.[3[\d]m)|([\u0000-\u0019])").Replace(input, string.Empty)
            .Replace("[" + '', string.Empty)
            .Replace(''.ToString(), string.Empty);
    }

}
