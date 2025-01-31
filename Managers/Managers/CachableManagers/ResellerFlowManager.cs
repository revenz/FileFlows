namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for reseller flows
/// </summary>
public class ResellerFlowManager : CachedManager<ResellerFlow>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => false;
}
