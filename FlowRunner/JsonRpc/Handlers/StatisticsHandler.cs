namespace FileFlows.FlowRunner.JsonRpc.Handlers;

/// <summary>
/// Statistics handler
/// </summary>
/// <param name="client">the JSON RPC Client</param>
public class StatisticsHandler(JsonRpcClient client)
{
    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public Task RecordRunningTotal(string name, string value)
        => client.SendRequest(nameof(RecordRunningTotal), new { Name = name, Value = value});
    
    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="name">the name of the statistic</param>
    /// <param name="value">the value of the statistic</param>
    public Task RecordAverage(string name, int value)
        => client.SendRequest(nameof(RecordAverage), new { Name = name, Value = value});
}