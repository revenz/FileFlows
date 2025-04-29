using FileFlows.FlowRunner.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using Humanizer;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Executes a flow
/// </summary>
public class ExecuteFlow : Node
{
    /// <summary>
    /// Gets or sets the flow to execute
    /// </summary>
    public Flow Flow { get; set; }
    
    /// <summary>
    /// Gets or sets the runner executing this flow
    /// </summary>
    public Runner Runner { get; set; }
    
    /// <summary>
    /// Gets or sets the depth this flow is executing
    /// </summary>
    public int FlowDepthLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the properties for this sub flow that were entered into the fields from the user
    /// </summary>
    public IDictionary<string, object> Properties { get; set; }

    /// <summary>
    /// Loads this flows properties into the variables
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <param name="restoring">if the properties are being restored, happens after another sub flow was executed</param>
    private void LoadPropertiesInVariables(NodeParameters args, bool restoring = false)
    {
        if (Properties?.Any() != true)
            return;

        args.Logger?.ILog((restoring ? "Restoring" : "Loading") + " Flow Properties into variables");
        foreach (var prop in Properties)
        {
            args.Logger?.ILog(prop.Key + " = " + prop.Value);
            args.Variables[prop.Key] = prop.Value;
        }
    }

    /// <summary>
    /// Executes the flow
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the output from the flow</returns>
    public override int Execute(NodeParameters args)
    {
        // add this flows parts to the total
        Runner.IncreaseTotalParts(Flow.Parts.Count);
        
        if (Flow.Parts?.FirstOrDefault()?.FlowElementUid?.EndsWith("." + nameof(Startup)) == true)
            FlowDepthLevel = -1; // special case for startup flow, set depth to -1 so first flow is at 0
        
        LoadPropertiesInVariables(args);
        LoadFlowVariables(args, Flow.Properties?.Variables);

        int COMPLETED_OUTPUT = RunnerCodes.Completed;

        if (Flow.Type == FlowType.Failure)
        {
            COMPLETED_OUTPUT = RunnerCodes.Failure;
            args.UpdateVariables(new Dictionary<string, object>
            {
                { "FailedNode", Runner.runInstance.LibraryFile.ExecutedNodes.Last()?.NodeName },
                { "FailedElement", Runner.runInstance.LibraryFile.ExecutedNodes.Last()?.NodeName },
                { "FlowName", Runner.ExecutedFlows.LastOrDefault()?.Name ?? Flow.Name },
                { "FailureReason", args.FailureReason?.EmptyAsNull() }
            });
        }

        Runner.runInstance.RpcClient.RunnerInfo.SetFlow(Flow);
        Runner.ExecutedFlows.Add(Flow);
       
        // find the first node
        var part = Flow.Parts.FirstOrDefault(x => x.Inputs == 0);
        if (part == null)
        {
            args.Logger?.ELog("Failed to find Input node");
            return -1;
        }

        // if (Runner.StepChanged(part.Name).Failed(out string error))
        // {
        //     args.Logger?.ELog(error);
        //     return RunnerCodes.TerminalExit; // this is a terminal exit, cannot continue
        // }


        int count = 0;
        while(++count < Math.Min(Runner.runInstance.Properties.Config.MaxNodes * 2, 300))
        {
            if (Runner.CancellationToken.IsCancellationRequested || Runner.Canceled)
            {
                args.Logger?.WLog("Flow was canceled");
                return RunnerCodes.RunCanceled;
            }
            if (part == null) // always false, but just in case code changes and this is no longer always false
            {
                args.Logger?.WLog("Flow part was null");
                return RunnerCodes.Failure;
            }

            DateTime nodeStartTime = DateTime.UtcNow;
            Node? currentFlowElement = null;
            TemporaryLogger? loadFELogger = new(); // log this to a string, so we can include it in the flow element start
            try
            {
                var lfeResult = new FlowHelper(Runner.runInstance).LoadFlowElement(loadFELogger, part, args.Variables, Runner);
                if(lfeResult.Failed(out var lfeError) || lfeResult.Value == null)
                {
                    if(string.IsNullOrWhiteSpace(lfeError) == false)
                        args.Logger?.ELog(lfeError);
                    
                    // happens when canceled or when the node failed to load
                    args.FailureReason = part.Name == "FileFlows.VideoNodes.VideoFile"
                        ? "Video Plugin missing, download the from the Plugins page"
                        : lfeError?.EmptyAsNull() ?? "Failed to load flow element: " + part.Name +
                          "\nEnsure you have the required plugins installed.";

                    Runner.CurrentFlowElement = null;
                    args.Logger?.ELog(args.FailureReason);
                    return RunnerCodes.Failure;
                }

                currentFlowElement = lfeResult.Value;
                Runner.CurrentFlowElement = currentFlowElement;

                if (currentFlowElement is SubFlowOutput subOutput)
                    return subOutput.Output;

                if (part.FlowElementUid?.EndsWith("FlowInput") == true)
                    part.Name = Flow.Name; // entering this flow


                if (Runner.runInstance.RpcClient.RunnerInfo.StepChanged(GetStepName(part), DontCountStep(part))
                    .Result.Failed(out var stepChangeError))
                {
                    args.FailureReason = stepChangeError;
                    args.Logger?.ELog(stepChangeError);
                    return RunnerCodes.TerminalExit; // this is a terminal exit, cannot continue
                }

                if (currentFlowElement is ExecuteFlow sub)
                {
                    sub.Runner = Runner;
                    sub.FlowDepthLevel = FlowDepthLevel + 1;
                    if (sub.Flow.Type == FlowType.Failure)
                        sub.FlowDepthLevel = 1;
                }

                if (LogFlowPart(part))
                {
                    args.Logger?.ILog(new string('=', 70));
                    Runner.runInstance.LibraryFile.ExecutedNodes ??= new();
                    args.Logger?.ILog(
                        $"Executing Flow Element {(Runner.runInstance.LibraryFile.ExecutedNodes.Count + 1)}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? currentFlowElement.Name} [{currentFlowElement.GetType().FullName}]");
                    args.Logger?.ILog(new string('=', 70));
                    args.Logger?.ILog("Working File: " + args.WorkingFile);
                    loadFELogger.WriteToLog(args.Logger);
                    loadFELogger = null;
                }

                nodeStartTime = DateTime.UtcNow;

                // clear the failure reason, if this isn't a failure flow, if it is, we already have the reason for failure
                if (Flow.Type != FlowType.Failure &&
                    part.FlowElementUid?.EndsWith("." + nameof(ExecuteFlow)) == false &&
                    string.IsNullOrWhiteSpace(args.FailureReason) == false)
                {
                    args.Logger?.ILog("Current Flow Part: " + currentFlowElement.Name);
                    args.Logger?.ILog("Clearing failure reason: " + args.FailureReason);
                    args.FailureReason = null;
                }

                if (currentFlowElement.PreExecute(args) == false)
                    throw new Exception("PreExecute failed");
                int output = currentFlowElement.Execute(args);
                if (output is RunnerCodes.TerminalExit or RunnerCodes.RunCanceled)
                    return output; // just finish this, the flow element that caused the terminal exit already was recorded
                
                if (Runner.Canceled)
                    return RunnerCodes.RunCanceled;
                
                RecordFlowElementFinish(args, nodeStartTime, output, part, currentFlowElement);
                
                
                if (output == RunnerCodes.Failure && part.ErrorConnection == null)
                {
                    // the execution failed                     
                    args.Logger?.ELog("Flow Element returned error code: " + currentFlowElement!.Name);
                    return RunnerCodes.Failure;
                }

                var outputNode = output == RunnerCodes.Failure
                    ? part.ErrorConnection
                    : part.OutputConnections?.FirstOrDefault(x => x.Output == output);
                
                if (outputNode == null)
                {
                    if(string.IsNullOrWhiteSpace(Flow.Name) == false)
                        args.Logger?.ILog($"Flow '{Flow.Name}' completed");
                    else
                        args.Logger?.ILog($"Flow completed");
                    // flow has completed
                    return COMPLETED_OUTPUT;
                }
                
                var newPart = Flow.Parts.FirstOrDefault(x => x.Uid == outputNode.InputNode);
                
                if (newPart == null)
                {
                    // couldn't find the connection, maybe bad data, but flow has now finished
                    args.Logger?.WLog("Couldn't find output, flow completed: " + outputNode?.Output);
                    return outputNode?.Output == -1 ? RunnerCodes.Failure : RunnerCodes.Completed;
                }
                
                if(part.Type == FlowElementType.SubFlow)
                    LoadPropertiesInVariables(args, true);

                part = newPart;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrWhiteSpace(args.FailureReason))
                    args.FailureReason = ex.Message;

                if (loadFELogger != null)
                {
                    loadFELogger.WriteToLog(args.Logger);
                    loadFELogger = null;
                }
                
                args.Result = NodeResult.Failure;
                args.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Runner.runInstance.Properties.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                if(currentFlowElement != null)
                    RecordFlowElementFinish(args, nodeStartTime, RunnerCodes.Failure, part, currentFlowElement);
                return RunnerCodes.Failure;
            }
        }

        args.FailureReason = "Too many flow elements in flow, processing aborted.";
        args.Logger?.ELog(args.FailureReason);
        return RunnerCodes.TerminalExit;
    }

    /// <summary>
    /// Gets the step name for the current part
    /// </summary>
    /// <param name="part">the part</param>
    /// <returns>the step name</returns>
    private string GetStepName(FlowPart part)
    {
        if (string.IsNullOrWhiteSpace(part.Name) == false)
            return part.Name;
        int lastIndex = part.FlowElementUid.LastIndexOf(".", StringComparison.InvariantCulture);
        if (lastIndex > 0)
            return part.FlowElementUid[(lastIndex + 1)..].Humanize();
        lastIndex = part.FlowElementUid.LastIndexOf(":", StringComparison.InvariantCulture);
        if (lastIndex > 0)
            return part.FlowElementUid[(lastIndex + 1)..];
        return part.FlowElementUid;
    }

    /// <summary>
    /// Gets if the flow part should count towards the total
    /// </summary>
    /// <param name="part">the flow part to check</param>
    /// <returns>true if the flow part does NOT count towards total</returns>
    private bool DontCountStep(FlowPart part)
    {
        if (part.FlowElementUid.EndsWith("." + nameof(ExecuteFlow)))
            return true;
        if (part.FlowElementUid.EndsWith("." + nameof(Startup)))
            return true;
        if (part.FlowElementUid.EndsWith("." + nameof(FileDownloader)))
            return true;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowInput)))
            return true;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowOutput)))
            return true;
        
        return false;
    }


    /// <summary>
    /// Records a flow element finishes
    /// </summary>
    /// <param name="args">the node arguments</param>
    /// <param name="startTime">the date the flow element started</param>
    /// <param name="output">the output from the flow element</param>
    /// <param name="part">the part the flow element was created from</param>
    /// <param name="flowElement">the flow element that was executed</param>
    void RecordFlowElementFinish(NodeParameters args, DateTime startTime, int output, FlowPart part, Node flowElement)
    {
        if(DontRecordFlowPart(part))
            return; // we dont record this output, the flow element that called the flow will record this for us
        
        TimeSpan executionTime = DateTime.UtcNow.Subtract(startTime);
        string feName = part.Name?.EmptyAsNull() ?? part.Label?.EmptyAsNull() ?? flowElement.Name;
        string feElementUid = part.FlowElementUid;

        int depthAdjustment = 0;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowInput)))
        {
            // record this as the sub flow
            depthAdjustment = -1;
            feName = Flow.Name;
            feElementUid = "FlowStart";
        }
        else if (part.FlowElementUid.StartsWith("SubFlow:"))
        {
            feName = (part.Label?.EmptyAsNull() ?? part.Name)!;
            feElementUid = "FlowEnd";
        }
        else if (part.FlowElementUid == Globals.FlowFailureInputUid)
        {
            // this is the input for a Failure Flow
            // in this case we want to record the name of the failure flow as the feName
            var failureFlow =
                Runner.runInstance.Properties.Config.Flows?.FirstOrDefault(x => x is { Type: FlowType.Failure, Default: true });
            if (failureFlow != null)
                feName = failureFlow.Name;
            // so the failure elements appear 'beneath this one'
            depthAdjustment = -1;
        }
        else if (part.Type == FlowElementType.Script)
        {
            feName = "Script: " + feName;
        }

        Runner.runInstance.RpcClient.RunnerInfo.RecordNodeExecution(feName, feElementUid, output, executionTime, part, FlowDepthLevel + depthAdjustment).Wait();

        if (LogFlowPart(part) && part.FlowElementUid?.StartsWith("SubFlow:") != true)
        {
            args.Logger?.ILog("Flow Element execution time: " + executionTime);
            args.Logger?.ILog("Flow Element output: " + output);
            args.Logger?.ILog(new string('=', 70));
        }
    }

    
    /// <summary>
    /// Loads flow variables into the node parameters variables
    /// </summary>
    /// <param name="flowVariables">the variables</param>
    private void LoadFlowVariables(NodeParameters args, Dictionary<string, object> flowVariables)
    {
        if (flowVariables?.Any() != true)
            return;
        
        foreach (var variable in flowVariables)
        {
            object value = variable.Value;
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.False)
                    value = false;
                else if (je.ValueKind == JsonValueKind.True)
                    value = true;
                else if (je.ValueKind == JsonValueKind.Number)
                    value = je.GetInt32();
                else if (je.ValueKind == JsonValueKind.String)
                    value = je.GetString() ?? string.Empty;
                else
                    continue; // bad type
            }
            args.Variables[variable.Key] = value;
        }

    }

    /// <summary>
    /// Checks if a flow part should be recorded or not
    /// </summary>
    /// <param name="part">the flow part to check</param>
    /// <returns>true to not record this flow part, otherwise false</returns>
    private bool DontRecordFlowPart(FlowPart part)
    {
        return part.FlowElementUid?.EndsWith(nameof(SubFlowOutput)) == true ||
               part.FlowElementUid?.EndsWith("." + nameof(ExecuteFlow)) == true;
    }
    /// <summary>
    /// Checks if a flow part should be recorded or not
    /// </summary>
    /// <param name="part">the flow part to check</param>
    /// <returns>true to not record this flow part, otherwise false</returns>
    private bool LogFlowPart(FlowPart part)
    {
        if(DontRecordFlowPart(part))
            return false;
        if (part.FlowElementUid.EndsWith(nameof(SubFlowInput)))
            return false;
        return true;
    }
}