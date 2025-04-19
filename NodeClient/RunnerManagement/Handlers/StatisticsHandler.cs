using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

/// <summary>
/// Statistics handler
/// </summary>
public class StatisticsHandler
{
    private JsonRpcServer rpcServer;
    private ClientConnection _client;

    /// <summary>
    /// Constructs a new instance of the handler
    /// </summary>
    /// <param name="rpcServer">the RPC server</param>
    /// <param name="rpcRegister">the RPC register</param>
    public StatisticsHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _client = rpcServer._client.Connection;
        rpcRegister.Register<RecordRunningTotalModel>(nameof(RecordRunningTotal), RecordRunningTotal);
        rpcRegister.Register<RecordAverageModel>(nameof(RecordAverage), RecordAverage);
    }

    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="model">the model of the data</param>
    public void RecordRunningTotal(RecordRunningTotalModel model)
    {
        if(_client.AwaitConnection().GetAwaiter().GetResult())
            _ = _client.SendAsync(nameof(RecordRunningTotal), model.Name, model.Value);
    }

    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="model">the model of the data</param>
    public void RecordAverage(RecordAverageModel model)
    {
        if(_client.AwaitConnection().GetAwaiter().GetResult())
            _ = _client.SendAsync(nameof(RecordAverage), model.Name, model.Value);
    }

    /// <summary>
    /// Running total model
    /// </summary>
    /// <param name="Name">the name of the statistic</param>
    /// <param name="Value">the value</param>
    public record RecordRunningTotalModel(string Name, string Value);

    /// <summary>
    /// Running average model
    /// </summary>
    /// <param name="Name">the name of the statistic</param>
    /// <param name="Value">the value</param>
    public record RecordAverageModel(string Name, int Value);
}