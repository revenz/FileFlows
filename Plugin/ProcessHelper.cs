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
    /// The output of the process
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
    /// Cancels the running process
    /// </summary>
    void Cancel();

    /// <summary>
    /// Executes a shell command
    /// </summary>
    /// <param name="args">the arguments of the shell command</param>
    /// <returns>the processing result of the executed command</returns>
    Task<ProcessResult> ExecuteShellCommand(ExecuteArgs args);
}

/// <summary>
/// A helper class that handles executing processes and reading their output
/// </summary>
public class ProcessHelper : IProcessHelper
{
    private Process process;
    private readonly ILogger Logger;
    private ExecuteArgs Args;

    StringBuilder outputBuilder, errorBuilder;
    TaskCompletionSource<bool> outputCloseEvent, errorCloseEvent;

    private bool Fake;
    private CancellationToken _cancellationToken;

    /// <summary>
    /// Constructs an instance of the process helper
    /// </summary>
    /// <param name="logger">the logger used in the process helper</param>
    /// <param name="cancellationToken">the cancellation token to use on any spawned processes</param>
    /// <param name="fake">if this is a fake process helper, used in unit test or a demo system</param>
    public ProcessHelper(ILogger logger, CancellationToken cancellationToken, bool fake)
    {
        this.Logger = logger;
        this.Fake = fake;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Cancels the running process
    /// </summary>
    public void Cancel()
    {
        if (Fake) return;
        try
        {
            if (this.process != null)
            {
                this.process.Kill();
                this.process = null;
            }

        }
        catch (Exception) { }
    }

    /// <summary>
    /// Executes a shell command
    /// </summary>
    /// <param name="args">the arguments of the shell command</param>
    /// <returns>the processing result of the executed command</returns>
    public async Task<ProcessResult> ExecuteShellCommand(ExecuteArgs args)
    {
        if (Fake) return new ProcessResult();  

        var result = new ProcessResult();
        this.Args = args;

        using (var process = new Process())
        {
            this.process = process;

            process.StartInfo.FileName = args.Command;                
            if (args.ArgumentList?.Any() == true)
            {
                args.Arguments = string.Empty;
                foreach (var arg in args.ArgumentList)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                    if (arg.IndexOf(' ') > 0)
                        args.Arguments += "\"" + arg + "\" ";
                    else
                        args.Arguments += arg + " ";
                }
                args.Arguments = args.Arguments.Trim();
            }
            else if (string.IsNullOrEmpty(args.Arguments) == false)
            {
                process.StartInfo.Arguments = args.Arguments;
            }

            if (string.IsNullOrEmpty(args.WorkingDirectory) == false)
                process.StartInfo.WorkingDirectory = args.WorkingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            if (args.Silent == false)
            {
                Logger?.ILog(new string('-', 70));
                Logger?.ILog($"Executing: {args.Command} {args.Arguments}");
                if (string.IsNullOrEmpty(args.WorkingDirectory) == false)
                    Logger?.ILog($"Working Directory: {args.WorkingDirectory}");
                Logger?.ILog(new string('-', 70));
            }

            outputBuilder = new StringBuilder();
            outputCloseEvent = new TaskCompletionSource<bool>();

            process.OutputDataReceived += OnOutputDataReceived;

            errorBuilder = new StringBuilder();
            errorCloseEvent = new TaskCompletionSource<bool>();

            process.ErrorDataReceived += OnErrorDataReceived;

            bool isStarted;

            try
            {
                isStarted = process.Start();
            }
            catch (Exception error)
            {
                // Usually it occurs when an executable file is not found or is not executable

                result.Completed = true;
                result.ExitCode = -1;
                result.Output = error.Message;

                isStarted = false;
            }

            if (isStarted)
            {
                // Reads the output stream first and then waits because deadlocks are possible
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var timeoutMilliseconds = args.Timeout * 1000;

                // Creates task to wait for process exit using timeout
                var waitForExit = WaitForExitAsync(process, timeoutMilliseconds, _cancellationToken);

                // Create task to wait for process exit and closing all output streams
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                // Waits process completion and then checks it was not completed by timeout
                if (
                    (
                        (args.Timeout > 0 && await Task.WhenAny(Task.Delay(timeoutMilliseconds), processTask) == processTask) ||
                        (args.Timeout == 0 && await Task.WhenAny(processTask) == processTask)
                    )
                     && waitForExit.Result)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;

                    result.StandardError = errorBuilder.ToString();
                    result.StandardOutput = outputBuilder.ToString();    

                    // Adds process output if it was completed with error
                    if (process.ExitCode != 0)
                    {
                        result.Output = $"{outputBuilder}{errorBuilder}";
                    }
                    else
                    {
                        result.Output = outputBuilder.ToString();
                    }
                }
                else
                {
                    try
                    {
                        // Kill hung process if cancellation was requested or process exceeded timeout
                        if (!_cancellationToken.IsCancellationRequested)
                            process.Kill();
                        result.StandardError = errorBuilder.ToString();
                        result.StandardOutput = outputBuilder.ToString();  
                        result.Output = result.StandardOutput?.EmptyAsNull() ?? result.StandardError;
                    }
                    catch
                    {
                    }
                }
            }
        }
        process = null;

        return result;
    }

    private string ProcessLastOutputLine;

    /// <summary>
    /// Called when a process received standard data in its output
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the arguments for the event</param>
    void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        // The output stream has been closed i.e. the process has terminated
        if (e.Data == null)
        {
            outputCloseEvent.SetResult(true);
        }
        else
        {
            // remove ansi codes
            string line = new Regex(@"(.[3[\d]m)|([\u0000-\u0019])").Replace(e.Data, string.Empty)
                .Replace("[" + '', string.Empty)
                .Replace(''.ToString(), string.Empty);
            Args?.OnStandardOutput(line);
            if (ProcessLastOutputLine != line)
            {
                if (Args?.Silent != true)
                    Logger?.Raw(line);
                outputBuilder.AppendLine(line);
            }
            ProcessLastOutputLine = line;
        }
    }

    /// <summary>
    /// Called when a process received error data in its output
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="e">the arguments for the event</param>
    void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        // The error stream has been closed i.e. the process has terminated
        if (e.Data == null)
        {
            errorCloseEvent.SetResult(true);
        }
        else
        {
            // remove ansi codes
            string line = new Regex(@".[3[\d]m").Replace(e.Data, string.Empty).Replace("[" + '', string.Empty);
            Args?.OnErrorOutput(line);
            if (ProcessLastOutputLine != line)
            {
                if (Args?.Silent != true)
                    Logger?.Raw(line);
                outputBuilder.AppendLine(line);
            }
            ProcessLastOutputLine = line;
        }
    }

    /// <summary>
    /// Waits for a process to exit
    /// </summary>
    /// <param name="process">the process to wait for</param>
    /// <param name="timeoutMilliseconds">how long to wait before failing</param>
    /// <param name="cancellationToken">the cancellation token to support cancellation</param>
    /// <returns>if the process completed before the timeout</returns>
    private static async Task<bool> WaitForExitAsync(Process process, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) => tcs.TrySetResult(true);

        using (cancellationToken.Register(() => tcs.TrySetResult(false)))
        {
            var delayTask = Task.Delay(timeoutMilliseconds, cancellationToken);
            var exitTask = tcs.Task;

            var completedTask = await Task.WhenAny(exitTask, delayTask);
            return completedTask == exitTask && process.HasExited;
        }
    }
}

