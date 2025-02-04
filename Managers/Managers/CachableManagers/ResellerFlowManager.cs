namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for reseller flows
/// </summary>
public class ResellerFlowManager : CachedManager<ResellerFlow>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => false;

    /// <summary>
    /// Updates all reseller flows with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public async Task UpdateFlowName(Guid uid, string name)
    {
        await DatabaseAccessManager.Instance.ObjectManager.UpdateAllObjectReferences(nameof(ResellerFlow.Flow), uid, name);
        if (UseCache == false || _Data?.Any() != true)
            return;
        foreach (var d in _Data)
        {
            if (d.Flow?.Uid == uid)
                d.Flow.Name = name;
        }
    }
}
