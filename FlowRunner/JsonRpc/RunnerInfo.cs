using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc;

public class RunnerInfo
{
    
    public int TotalParts { get; set; }
    public float CurrentPartPercent {get;set;}
    public string CurrentPartName {get;set;}
    public int CurrentPart {get;set;}

    /// <summary>
    /// The number of flow elements that currently have been executed
    /// </summary>
    public int ExecutedSteps { get; set; }

    /// <summary>
    /// The number of flow elements that have been executed and that count towards the total allowed
    /// We dont count steps like startup, enter sub flow, sub flow output etc
    /// </summary>
    public int ExecutedStepsCountedTowardsTotal { get; set; }

    /// <summary>
    /// Gets or sets any additional info to pass to the runner
    /// </summary>
    public Dictionary<string, AdditionalInfo> AdditionalInfos { get; set; }
}