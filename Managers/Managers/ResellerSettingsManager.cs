using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// An instance of the Reseller Settings Service which allows accessing of the resseller settings
/// </summary>
public class ResellerSettingsManager
{
    private static FairSemaphore _semaphore = new(1);
    // Special case, we always cache the settings, as it is constantly looked up
    private static ResellerSettings? Instance;
    private static Guid _Uid = new Guid("c85e56cc-6648-4815-b85e-dc780201a5d6");

    static ResellerSettingsManager()
    {
        Instance = DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<ResellerSettings>().Result;
        if (Instance != null)
            return;
        Instance = new ResellerSettings
        {
            Uid = _Uid,
            Name = nameof(ResellerSettings),
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now,
        };
        DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, auditDetails: AuditDetails.ForServer()).Wait();
    }

    /// <summary>
    /// Gets or sets if caching should be used
    /// </summary>
    internal static bool UseCache => true; 
    
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public ResellerSettings Get() => Instance!;
    
    /// <summary>
    /// Updates the reseller settings
    /// </summary>
    /// <param name="model">the reseller settings</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Update(ResellerSettings model, AuditDetails? auditDetails)
    {
        await _semaphore.WaitAsync();
        try
        {
            model.Uid = _Uid;
            model.Name = nameof(ResellerSettings.Name);
            Instance = model;
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, auditDetails);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}