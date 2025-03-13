using FileFlows.FlowRunner.JsonRpc;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner;

/// <summary>
/// Runner properties
/// </summary>
public class RunnerProperties
{
    /// <summary>
    /// Constructs a new instance of the Runner Parameters
    /// </summary>
    /// <param name="rpcClient">the RPC client</param>
    public RunnerProperties(JsonRpcClient rpcClient)
    {
        RpcClient = rpcClient;
#if(DEBUG)
        Logger = new ( rpcClient);
#else
        Logger = new ();
#endif
        Logger.ILog("Flow Runner Version: " + Globals.Version);
    }
    
    /// <summary>
    /// Gets the runner instance UID
    /// </summary>
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets the Node UID
    /// </summary>
    public Guid NodeUid { get; set; }

    /// <summary>
    /// The flow logger
    /// </summary>
    public FlowLogger Logger { get; init; }
    
    /// <summary>
    /// Gets or sets the configuration that is currently being executed
    /// </summary>
    public ConfigurationRevision Config { get; set; }
    
    /// <summary>
    /// Gets or sets the directory where the configuration is saved
    /// </summary>
    public string ConfigDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the working directory
    /// </summary>
    public string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the processing node this is running on
    /// </summary>
    public ProcessingNode ProcessingNode { get; set; }

    /// <summary>
    /// Gest the RPC client
    /// </summary>
    public JsonRpcClient RpcClient { get; init; }

    /// <summary>
    /// Gets the library file
    /// </summary>
    public LibraryFile LibraryFile => RpcClient.LibraryFile;

    /// <summary>
    /// Gets or sets the flow that started this run
    /// </summary>
    public Flow StartingFlow { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a remote file
    /// </summary>
    public bool IsRemote { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a directory
    /// </summary>
    public bool IsDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the path of the library the file is in
    /// </summary>
    public string LibraryPath { get; set; }
}