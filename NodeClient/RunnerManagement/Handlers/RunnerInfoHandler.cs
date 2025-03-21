using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

public class RunnerInfoHandler
{
    private JsonRpcServer rpcServer;
    private RunnerManager runnerManager;
    public RunnerInfoHandler(RunnerManager runnerManager, JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        this.runnerManager = runnerManager;
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
        runnerManager.UpdateRunner(info);
    }
    
}