using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

/// <summary>
/// Statistics handler
/// </summary>
public class StatisticsHandler
{
    private JsonRpcServer rpcServer;
    private HubConnection _connection;

    /// <summary>
    /// Constructs a new instance of the handler
    /// </summary>
    /// <param name="rpcServer">the RPC server</param>
    /// <param name="rpcRegister">the RPC register</param>
    public StatisticsHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _connection = rpcServer._client._connection;
        rpcRegister.Register<RecordRunningTotalModel>(nameof(RecordRunningTotal), RecordRunningTotal);
        rpcRegister.Register<RecordAverageModel>(nameof(RecordAverage), RecordAverage);
    }

    /// <summary>
    /// Records a running total statistic
    /// </summary>
    /// <param name="model">the model of the data</param>
    public void RecordRunningTotal(RecordRunningTotalModel model)
        => _ = _connection.SendAsync(nameof(RecordRunningTotal), model.Name, model.Value);

    /// <summary>
    /// Records a average 
    /// </summary>
    /// <param name="model">the model of the data</param>
    public void RecordAverage(RecordAverageModel model)
        => _ = _connection.SendAsync(nameof(RecordAverage), model.Name, model.Value);

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