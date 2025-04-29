namespace FileFlows.Shared;

/// <summary>
/// Common Variables
/// </summary>
public class CommonVariables
{
    /// <summary>
    /// The name of the internal processing node
    /// </summary>
    public const string InternalNodeName = "FileFlowsServer";

    /// <summary>
    /// The UID of the internal processing node
    /// </summary>
    public static readonly Guid InternalNodeUid = new ("bf47da28-051e-452e-ad21-c6a3f477fea9");

    /// <summary>
    /// The name of the manual library
    /// </summary>
    public static readonly string ManualLibrary = "Manually Added";
    /// <summary>
    /// The UID for the manual library
    /// </summary>
    public static readonly Guid ManualLibraryUid = new("22222222-2222-2222-2222-222222222222");
    
    /// <summary>
    /// The name of the internal processing node
    /// </summary>
    public const string OperatorFileFlowsServerName = "FileFlows Server";
    /// <summary>
    /// The UID of the internal processing node
    /// </summary>
    public static readonly Guid OperatorFileFlowsServerUid = new Guid("07ecd8bd-79a6-454d-8033-e64693fc2f7b");
    
    /// <summary>
    /// Dummy password to use in place of passwords
    /// </summary>
    public const string DUMMY_PASSWORD = "************";
}