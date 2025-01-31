using System.Text.Json;
using Logger = FileFlows.Shared.Logger;
using FileFlows.Plugin;
using System.Dynamic;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server;

/// <summary>
/// A special node that is not shown in the UI and only created
/// by the Flow Runner to execute a script.
/// This Node exists in this plugin to make use of the Javascript executor
/// </summary>
public class ScriptNode:Node
{
    /// <summary>
    /// Gets the number of inputs of this node
    /// </summary>
    public override int Inputs => 1;

    /// <summary>
    /// Gets or sets the model to pass to the node
    /// </summary>
    public ExpandoObject? Model { get; set; }

    /// <summary>
    /// Gets or sets the Script to execute
    /// </summary>
    public Script? Script { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the flow element, used to look for variable overwrites
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// Executes the script node
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    public override int Execute(NodeParameters args)
    {
        if (Script == null)
        {
            args.FailureReason = "Script not found";
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        switch (Script.Language)
        {
            case ScriptLanguage.JavaScript:
                return ExecuteJavaScript(args);
            default:
                args.Logger?.ILog($"Executing {Script.Language} Script");
                return ExecuteScript(args);
        }
    }

    /// <summary>
    /// Executes a script
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    private int ExecuteScript(NodeParameters args)
    {
        var result = args.ScriptExecutor.Execute(new()
        {
            Args = args,
            Logger = args.Logger!,
            ScriptType = ScriptType.Flow,
            TempPath = args.TempPath,
            Code = args.ReplaceVariables(Script!.Code),
            Language = Script.Language
        });
        if (result.Failed(out var error))
        {
            args.FailureReason = error;
            args.Logger?.ELog(error);
            return -1;
        }

        if (result.Value > Script.Outputs.Count)
        {
            args.FailureReason = "Unexpected output: " + result.Value;
            args.Logger?.ELog(args.FailureReason);
            return -1;
        }

        return result.Value;
    }

    /// <summary>
    /// Executes a JavaScript script
    /// </summary>
    /// <param name="args">the NodeParameters passed into this from the flow runner</param>
    /// <returns>the output node to call next</returns>
    private int ExecuteJavaScript(NodeParameters args)
    {
        // build up the entry point
        string epParams = string.Join(", ", Script!.Parameters?.Select(x => x.Name)?.ToArray() ?? []);
        // all scripts must contain the "Script" method we then add this to call that 
        //string entryPoint = $"Script({epParams});";
        string entryPoint = $"var scriptResult = Script({epParams});\nexport const result = scriptResult;";

        var execArgs = new Plugin.Models.ScriptExecutionArgs
        {
            Args = args,
            ScriptType = ScriptType.Flow,
            Logger = args.Logger!,
            Code = (Script.Code + "\n\n" + entryPoint).Replace("\t", "   ").Trim(),
            AdditionalArguments = new (),
            NotificationCallback = (severity, title, message) =>
            {
                var service = ServerShared.Services.ServiceLoader.Load<INotificationService>();
                _ = service.Record((NotificationSeverity)severity, title, message);
            }
        };

        if (Script.Parameters?.Any() == true)
        {
            var dictModel = Model as IDictionary<string, object>;
            foreach (var p in Script.Parameters) 
            {
                try
                {
                    var value = dictModel?.TryGetValue(p.Name, out var value1) is true ? value1 : null;
                    if (args != null && args.Variables.TryGetValue($"{Uid}.{p.Name}", out object? oOverride))
                        value = oOverride;
                    if (value is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.String)
                        {
                            var str = je.GetString();
                            if (string.IsNullOrWhiteSpace(str) == false)
                            {
                                Logger.Instance.ILog("Parameter is string replacing variables: " + str);
                                var replaced = args?.ReplaceVariables(str);
                                if (replaced != str)
                                {
                                    Logger.Instance.ILog("Variables replaced: " + replaced);
                                    value = replaced;
                                }
                            }
                        }
                        else if (je.ValueKind == JsonValueKind.True)
                            value = true;
                        else if (je.ValueKind == JsonValueKind.False)
                            value = false;
                        else if (je.ValueKind == JsonValueKind.Number)
                            value = double.Parse(je.ToString());
                        else if (je.ValueKind == JsonValueKind.Null)
                            value = null;
                    }

                    execArgs.AdditionalArguments.Add(p.Name, value!);
                }
                catch (Exception ex)
                {
                    args?.Logger?.WLog("Failed to set parameter: " + p.Name + " => " + ex.Message +
                                       Environment.NewLine + ex.StackTrace);
                }
            }
        }

        return args?.ScriptExecutor?.Execute(execArgs) ?? 0;
    }
}
