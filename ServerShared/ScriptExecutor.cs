using System.Diagnostics;
using System.Text;
using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.ScriptExecution;
using FileFlows.Shared.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace FileFlows.ServerShared;

/// <summary>
/// A JavaScript code executor
/// </summary>
public class ScriptExecutor : IScriptExecutor
{
    /// <summary>
    /// The HTTP Client used in scripts for requests
    /// </summary>
    private static HttpClient httpClient = new();

    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);

    /// <summary>
    /// Gets or sets the shared directory 
    /// </summary>
    public string SharedDirectory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the URL to the FileFlows server 
    /// </summary>
    public string FileFlowsUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the plugin method invoker
    /// This allows plugins to expose static functions that can be called from functions/scripts
    /// </summary>
    public Func<string, string, object[], object> PluginMethodInvoker { get; set; } = null!;

    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    public async Task<Result<int>> Execute(ScriptExecutionArgs execArgs)
    {
        return execArgs.Language switch
        {
            ScriptLanguage.Batch => ExecuteBatch(execArgs),
            ScriptLanguage.PowerShell => ExecutePowerShell(execArgs),
            ScriptLanguage.Shell => ExecuteShell(execArgs),
            ScriptLanguage.CSharp => await ExecuteCSharp(execArgs),
            _ => ExecuteJavaScript(execArgs)
        };
    }


    /// <summary>
    /// Executes a PowerShell script
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    private Result<int> ExecutePowerShell(ScriptExecutionArgs args)
    {
        if (OperatingSystem.IsWindows() == false)
            return Result<int>.Fail("Cannot run a PowerShell file on a non Windows system");

        var ps1File = Path.Combine(args.TempPath, Guid.NewGuid() + ".ps1");

        try
        {
            var code = args.Code;
            if (args.Args != null)
                code = args.Args.ReplaceVariables(code);
            args.Logger?.ILog("Executing code: \n" + code);
            File.WriteAllText(ps1File, code);
            args.Logger?.ILog($"Temporary PowerShell file created: {ps1File}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1File}\"",
                WorkingDirectory = args.TempPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();
            int exitCode = process.ExitCode;

            if (string.IsNullOrWhiteSpace(standardOutput) == false)
                args.Logger?.ILog($"Standard Output:\n{standardOutput}");
            if (string.IsNullOrWhiteSpace(standardError) == false)
                args.Logger?.WLog($"Standard Error:\n{standardError}");
            args.Logger?.ILog($"Exit Code: {exitCode}");

            return exitCode;
        }
        catch (Exception ex)
        {
            return Result<int>.Fail("Failed executing PowerShell script: " + ex.Message);
        }
    }



    /// <summary>
    /// Executes a Batch script
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    private Result<int> ExecuteBatch(ScriptExecutionArgs args)
    {
        if (OperatingSystem.IsWindows() == false)
            return Result<int>.Fail("Cannot run a .bat file on a non Windows system");

        var batFile = Path.Combine(args.TempPath, Guid.NewGuid() + ".bat");

        try
        {
            var code = "@echo off" + Environment.NewLine + args.Code;
            if (args.Args != null)
                code = args.Args.ReplaceVariables(code);
            args.Logger?.ILog("Executing code: \n" + code);
            File.WriteAllText(batFile, code);
            args.Logger?.ILog($"Temporary bat file created: {batFile}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = batFile,
                WorkingDirectory = args.TempPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();
            int exitCode = process.ExitCode;

            if (string.IsNullOrWhiteSpace(standardOutput) == false)
                args.Logger?.ILog($"Standard Output:\n{standardOutput}");
            if (string.IsNullOrWhiteSpace(standardError) == false)
                args.Logger?.WLog($"Standard Error:\n{standardError}");
            args.Logger?.ILog($"Exit Code: {exitCode}");

            return exitCode;
        }
        catch (Exception ex)
        {
            return Result<int>.Fail("Failed executing bat script: " + ex.Message);
        }
    }

    /// <summary>
    /// Executes a Shell script
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    private int ExecuteShell(ScriptExecutionArgs args)
    {
        if (OperatingSystem.IsWindows())
            return Result<int>.Fail("Cannot run a SH script on a Windows system");

        var shFile = Path.Combine(args.TempPath, Guid.NewGuid() + ".sh");

        try
        {
            var code = args.Code;
            if (args.Args != null)
                code = args.Args.ReplaceVariables(code);
            args.Logger?.ILog("Executing code: \n" + code);
            File.WriteAllText(shFile, code);
            args.Logger?.ILog($"Temporary SH file created: {shFile}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                ArgumentList = { shFile },
                WorkingDirectory = args.TempPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();
            int exitCode = process.ExitCode;

            if (!string.IsNullOrWhiteSpace(standardOutput))
                args.Logger?.ILog($"Standard Output:\n{standardOutput}");
            if (!string.IsNullOrWhiteSpace(standardError))
                args.Logger?.WLog($"Standard Error:\n{standardError}");
            args.Logger?.ILog($"Exit Code: {exitCode}");

            return exitCode;
        }
        catch (Exception ex)
        {
            return Result<int>.Fail("Failed executing SH script: " + ex.Message);
        }
    }

    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    private Result<int> ExecuteJavaScript(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return Result<int>.Fail("No code"); // no code, flow cannot continue doesnt know what to do

        var args = execArgs.Args;

        Executor executor = new();
        executor.Logger = new ScriptExecution.Logger();
        executor.Logger.ELogAction = (largs) => execArgs.Logger?.ELog(largs);
        executor.Logger.WLogAction = (largs) => execArgs.Logger?.WLog(largs);
        executor.Logger.ILogAction = (largs) => execArgs.Logger?.ILog(largs);
        executor.Logger.DLogAction = (largs) => execArgs.Logger?.DLog(largs);
        executor.HttpClient = httpClient;
        if (string.IsNullOrWhiteSpace(FileFlowsUrl) == false)
            args.Variables["FileFlows.Url"] = FileFlowsUrl;

        executor.Variables = args.Variables;
        executor.SharedDirectory = SharedDirectory;

        executor.Code = execArgs.Code;
        if (execArgs.ScriptType == ScriptType.Flow &&
            executor.Code.IndexOf("function Script", StringComparison.Ordinal) < 0)
        {
            if (executor.Code.IndexOf("await ", StringComparison.Ordinal) >= 0)
            {
                executor.Code = "async function Script() {\n" + executor.Code + "\n}\n";
                executor.Code += $"var scriptResult = await Script();\nexport const result = scriptResult;";
            }
            else
            {
                executor.Code = "function Script() {\n" + executor.Code + "\n}\n";
                executor.Code += $"var scriptResult = Script();\nexport const result = scriptResult;";
            }
        }

        executor.ProcessExecutor = new ScriptProcessExecutor(args);
        foreach (var arg in execArgs.AdditionalArguments ?? new())
            executor.AdditionalArguments.Add(arg.Key, arg.Value);
        executor.AdditionalArguments["Flow"] = args;
        executor.AdditionalArguments["CacheStore"] = args.Cache;

        executor.AdditionalArguments["PluginMethod"] = PluginMethodInvoker;

        executor.SharedDirectory = SharedDirectory;
        try
        {
            object? result = executor.Execute();
            if (result is int iOutput)
                return iOutput;
            return -1;
        }
        catch (Exception ex)
        {
            args.Logger?.ELog("Failed executing: " + ex.Message);
            return Result<int>.Fail("Failed executing: " + ex.Message);
        }
    }

    /// <summary>
    /// Executes C#
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    private async Task<Result<int>> ExecuteCSharp(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return Result<int>.Fail("No code"); // no code, flow cannot continue doesnt know what to do

        // Get only the assemblies that are not dynamically generated
        var staticAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && assembly.FullName?.Contains('-') != true)
            .ToArray();

        // Configure script options
        var scriptOptions = ScriptOptions.Default
            .WithReferences(staticAssemblies) // Add necessary references
            .WithImports(
                "System", // Basic system types
                "System.Linq", // LINQ queries
                "System.Collections.Generic", // Generic collections like List, Dictionary
                "System.Text", // String manipulation
                "System.Text.RegularExpressions", // Regular expressions
                "System.Diagnostics", // Processes
                "System.IO", // File and stream operations
                "System.Globalization", // Culture-specific operations
                "FileFlows.Plugin"
            );

        try
        {
            // Execute the script with the provided logger
            var result = await CSharpScript.RunAsync(execArgs.Code, scriptOptions, new Globals
            {
                Logger = execArgs.Logger,
                Flow = execArgs.Args,
                Variables = execArgs.Args?.Variables ?? new(),
                NotificationDelegate = execArgs.NotificationCallback
            });
            if (result.ReturnValue is int iOutput)
                return iOutput;
            if (result.ReturnValue is bool bValue)
                return bValue ? 1 : 0;
            return -1;
        }
        catch (CompilationErrorException e)
        {
            return Result<int>.Fail($"Compilation errors: {string.Join(Environment.NewLine, e.Diagnostics)}");
        }
        catch (Exception e)
        {
            return Result<int>.Fail($"Runtime error: {e.Message}");
        }
    }

    /// <summary>
    /// The CSharp Globals to use in the Script execution
    /// </summary>
    public class Globals
    {
        /// <summary>
        /// Gets or sets the variables
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = null!;

        /// <summary>
        /// Gets or sets the logger
        /// </summary>
        public ILogger Logger { get; set; } = null!;

        /// <summary>
        /// Gets or sets the flow
        /// </summary>
        public NodeParameters Flow { get; set; } = null!;

        /// <summary>
        /// Gets or sets the notification delegate
        /// </summary>
        public ScriptExecutionArgs.NotificationDelegate NotificationDelegate { get; set; } = null!;

        /// <summary>
        /// Sends a notification
        /// </summary>
        /// <param name="severity">the string notification severity</param>
        /// <param name="title">the title of the notification</param>
        /// <param name="message">the message of the notification</param>
        public void SendNotification(string severity, string title, string? message = null)
        {
            NotificationSeverity ns = severity.ToLowerInvariant() switch
            {
                "critical" => NotificationSeverity.Critical,
                "error" => NotificationSeverity.Error,
                "warning" => NotificationSeverity.Warning,
                _ => NotificationSeverity.Information
            };

            NotificationDelegate?.Invoke(ns, title, message);
        }
    }


    /// <summary>
    /// Executes code and returns the result
    /// </summary>
    /// <param name="code">the code to execute</param>
    /// <param name="variables">any variables to be passed to the executor</param>
    /// <param name="sharedDirectory">[Optional] the shared script directory to look in</param>
    /// <param name="dontLogCode">If the code should be included in the log if the execution fails</param>
    /// <returns>the result of the execution</returns>
    public static FileFlowsTaskRun Execute(string code, Dictionary<string, object> variables,
        string? sharedDirectory = null, bool dontLogCode = false)
    {
        Executor executor = new Executor();
        executor.Code = code;
        executor.SharedDirectory = sharedDirectory; //.EmptyAsNull() ?? DirectoryHelper.ScriptsDirectoryShared;
        executor.HttpClient = httpClient;
        executor.Logger = new ScriptExecution.Logger();
        executor.DontLogCode = dontLogCode;
        StringBuilder sbLog = new();
        executor.Logger.DLogAction = (args) => StringBuilderLog(sbLog, LogType.Debug, args);
        executor.Logger.ILogAction = (args) => StringBuilderLog(sbLog, LogType.Info, args);
        executor.Logger.WLogAction = (args) => StringBuilderLog(sbLog, LogType.Warning, args);
        executor.Logger.ELogAction = (args) => StringBuilderLog(sbLog, LogType.Error, args);
        executor.Variables = variables;
        try
        {
            object? returnValue = executor.Execute();
            return new FileFlowsTaskRun()
            {
                Log = FixLog(sbLog),
                Success = ((returnValue as bool? ?? true as bool?)!).Value,
                ReturnValue = returnValue
            };
        }
        catch (Exception ex)
        {
            return new FileFlowsTaskRun()
            {
                Log = FixLog(sbLog),
                Success = false,
                ReturnValue = ex.Message
            };
        }

        string FixLog(StringBuilder sb)
            => sb.ToString()
                .Replace("\\n", "\n").Trim();
    }




    private static void StringBuilderLog(StringBuilder builder, LogType type, params object[] args)
    {
        string typeString = type switch
        {
            LogType.Debug => "[DBUG] ",
            LogType.Info => "[INFO] ",
            LogType.Warning => "[WARN] ",
            LogType.Error => "[ERRR] ",
            _ => "",
        };
        string message = typeString + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        builder.AppendLine(message);
    }

    /// <summary>
    /// Script Process Executor that executes the process using node parameters
    /// </summary>
    class ScriptProcessExecutor : IProcessExecutor
    {
        private NodeParameters NodeParameters;

        /// <summary>
        /// Constructs an instance of a Script Process Executor 
        /// </summary>
        /// <param name="nodeParameters">the node parameters</param>
        public ScriptProcessExecutor(NodeParameters nodeParameters)
        {
            this.NodeParameters = nodeParameters;
        }

        /// <summary>
        /// Executes the process
        /// </summary>
        /// <param name="args">the arguments to execute</param>
        /// <returns>the result of the execution</returns>
        public ProcessExecuteResult Execute(ProcessExecuteArgs args)
        {
            var result = NodeParameters.Execute(new ExecuteArgs()
            {
                Arguments = args.Arguments,
                Command = args.Command,
                Timeout = args.Timeout,
                ArgumentList = args.ArgumentList,
                WorkingDirectory = args.WorkingDirectory
            });
            return new ProcessExecuteResult()
            {
                Completed = result.Completed,
                Output = result.Output,
                ExitCode = result.ExitCode,
                StandardError = result.StandardError,
                StandardOutput = result.StandardOutput,
            };
        }
    }
}