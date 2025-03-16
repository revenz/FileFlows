using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.NodeClient.Handlers;

/// <summary>
/// Basic handler of node/runner communication
/// </summary>
public class BasicHandler
{
    private JsonRpcServer rpcServer;

    /// <summary>
    /// Constructs a new instance of the Basic Handler
    /// </summary>
    /// <param name="rpcServer">the RPC server</param>
    /// <param name="rpcRegister">the RPC register</param>
    public BasicHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        #if(DEBUG)
        rpcRegister.Register(nameof(LogMessage), LogMessage);
        #endif
        rpcRegister.Register(nameof(GetRunnerParameters), GetRunnerParameters);
        rpcRegister.Register(nameof(GetNode), GetNode);
    }

    /// <summary>
    /// Get the runner parameters
    /// </summary>
    /// <returns>the runner parameters</returns>
    public RunnerParameters GetRunnerParameters()
        => rpcServer.runnerParameters;

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the node executing this runner</returns>
    public ProcessingNode GetNode()
        => rpcServer._client.Node!;
    
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="message">the message to log</param>
    public void LogMessage(string message)
    {
        if(string.IsNullOrEmpty(message) == false)
            rpcServer._logMessage(message);
    }
}