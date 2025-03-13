using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

public class RunnerInfoHandler
{
    private JsonRpcServer rpcServer;
    public RunnerInfoHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        rpcRegister.Register<FlowExecutorInfo>(nameof(UpdateRunnerInfo), UpdateRunnerInfo);
    }
    
    /// <summary>
    /// Updates the runner info
    /// </summary>
    /// <param name="info"></param>
    public void UpdateRunnerInfo(FlowExecutorInfo info)
    {
        if (info == null)
            return;
        
        rpcServer._flowExecutorInfo = info;
        rpcServer._client._connection.InvokeAsync("FileUpdateInfo", info);
    }
    
}