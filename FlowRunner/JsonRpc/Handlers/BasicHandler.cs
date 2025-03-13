using FileFlows.FlowRunner.JsonRpc;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc.Handlers;

public class BasicHandler(JsonRpcClient client)
{

    /// <summary>
    /// Retrieves the runner parameters from the server.
    /// </summary>
    /// <returns>The <see cref="RunnerParameters"/> containing the parameters.</returns>
    public async Task<RunnerParameters> GetRunnerParameters()
    {
        var result = await client.SendRequest<RunnerParameters>(nameof(GetRunnerParameters));
        client.LibraryFile = result.LibraryFile;
        return result;
    }

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the node executing this runner</returns>
    public async Task<ProcessingNode> GetNode()
    {
        client.Node ??= await client.SendRequest<ProcessingNode>(nameof(GetNode));
        return client.Node;
    }

#if(DEBUG)
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="message">the message being logged</param>
    /// <returns>a task to await</returns>
    public async Task LogMessage(object message)
        => await client.SendRequest(nameof(LogMessage), message);
#endif
}