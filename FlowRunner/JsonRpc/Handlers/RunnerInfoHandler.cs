using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc.Handlers;

public class RunnerInfoHandler(JsonRpcClient client, int maxFlowParts)
{
    private RunnerInfo runInfo = new();
    
    /// <summary>
    /// Called when the current flow step changes, ie it moves to a different node to execute
    /// </summary>
    /// <param name="partName">the step part name</param>
    /// <param name="dontCountTowardsTotal">if this step should not count towards the total number of steps allowed</param>
    internal async Task<Result<bool>> StepChanged(string partName, bool dontCountTowardsTotal = false)
    {
        ++runInfo.ExecutedSteps;
        if (dontCountTowardsTotal == false)
            ++runInfo.ExecutedStepsCountedTowardsTotal;
        
        if (runInfo.ExecutedStepsCountedTowardsTotal > maxFlowParts)
            return Result<bool>.Fail("Exceeded maximum number of flow elements to process");

        // remove old additional info
        var aiKeys = runInfo.AdditionalInfos?.Keys?.ToArray() ?? [];
        foreach (var kv in aiKeys)
        {
            if (--runInfo.AdditionalInfos[kv].Steps < 1)
                runInfo.AdditionalInfos.Remove(kv);
        }
        
        runInfo.CurrentPartName = partName;
        runInfo.CurrentPart = runInfo.ExecutedSteps;
        runInfo.CurrentPartPercent = 0;
        await UpdateRunnerInfo();
        return true;
    }

    /// <summary>
    /// Updates the currently steps completed percentage
    /// </summary>
    /// <param name="percentage">the percentage</param>
    internal async Task UpdatePartPercentage(float percentage)
    {
        runInfo.CurrentPartPercent = percentage;
        await UpdateRunnerInfo();
    }

    /// <summary>
    /// Updates the flow executing info
    /// </summary>
    /// <returns>a task to await</returns>
    public async Task UpdateRunnerInfo()
        => await client.SendRequest(nameof(UpdateRunnerInfo), new FlowExecutorInfo()
        {
            Uid = client.LibraryFile.Uid,
            NodeUid = client.Node.Uid,
            NodeName = client.Node.Name,
            RelativeFile = client.LibraryFile.RelativePath,
            Library = client.LibraryFile.Library,
            AdditionalInfos = runInfo.AdditionalInfos,
            CurrentPart = runInfo.CurrentPart,
            CurrentPartPercent = runInfo.CurrentPartPercent,
            TotalParts = runInfo.TotalParts,
            CurrentPartName = runInfo.CurrentPartName,
            WorkingFile = client.LibraryFile.Name
        });

    /// <summary>
    /// Records additional info which is used to display Encoder, Decoder, FPS etc on the dashboard
    /// </summary>
    /// <param name="name">the name of the info, eg Encoder, Decoder, FPS</param>
    /// <param name="value">the new value to display</param>
    /// <param name="steps">how many steps to keep this value shown for</param>
    /// <param name="expiry">when this value will expire and should be removed if not updated again</param>
    public async Task RecordAdditionalInfo(string name, object value, int steps, TimeSpan? expiry)
    {
        runInfo.AdditionalInfos ??= new();
        if (value == null)
        {
            if (runInfo.AdditionalInfos.ContainsKey(name) == false)
                return; // nothing to do

            runInfo.AdditionalInfos.Remove(name);
        }
        else
        {
            if (value is TimeSpan ts)
                value = Plugin.Helpers.TimeHelper.ToHumanReadableString(ts);
            
            runInfo.AdditionalInfos[name] = new()
            {
                Value = value,
                Expiry = expiry ?? new TimeSpan(0, 1, 0),
                Steps = steps
            };
        }

        await UpdateRunnerInfo();
    }

    /// <summary>
    /// Sends an update to increase the total parts
    /// </summary>
    /// <param name="additional">the number of parts to add </param>
    public void IncreaseTotalParts(int additional)
        => runInfo.TotalParts += additional;
    
    
    /// <summary>
    /// Records the execution of a flow node
    /// </summary>
    /// <param name="nodeName">the name of the flow node</param>
    /// <param name="nodeUid">the UID of the flow node</param>
    /// <param name="output">the output after executing the flow node</param>
    /// <param name="duration">how long it took to execution</param>
    /// <param name="part">the flow node part</param>
    /// <param name="flowDepth">the depth of the executed flow</param>
    public async Task RecordNodeExecution(string nodeName, string nodeUid, int output, TimeSpan duration, FlowPart part, int flowDepth)
    {
        client.LibraryFile.ExecutedNodes ??= new List<ExecutedNode>();
        client.LibraryFile.ExecutedNodes.Add(new ExecutedNode
        {
            NodeName = nodeName,
            NodeUid = part.Type == FlowElementType.Script ? "ScriptNode" : nodeUid,
            FlowPartUid = part.Uid,
            
            Output = output,
            ProcessingTime = duration,
            Depth = flowDepth,
        });
        
        await client.LibraryFileHandler.UpdateLibraryFile(client.LibraryFile);

        await UpdateRunnerInfo();
    }
}