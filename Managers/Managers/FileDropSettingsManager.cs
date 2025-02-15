using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// An instance of the File Drop Settings Service which allows accessing of the resseller settings
/// </summary>
public class FileDropSettingsManager
{
    private static FairSemaphore _semaphore = new(1);
    // Special case, we always cache the settings, as it is constantly looked up
    private static FileDropSettings? Instance;
    private static Guid _Uid = new Guid("c85e56cc-6648-4815-b85e-dc780201a5d6");

    static FileDropSettingsManager()
    {
        Instance = DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<FileDropSettings>().Result;
        if (Instance != null)
            return;
        Instance = new FileDropSettings
        {
            Uid = _Uid,
            Name = nameof(FileDropSettings),
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
    public FileDropSettings Get() => Instance!;
    
    /// <summary>
    /// Updates the file drop settings
    /// </summary>
    /// <param name="model">the file drop settings</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Update(FileDropSettings model, AuditDetails? auditDetails)
    {
        await _semaphore.WaitAsync();
        try
        {
            model.Uid = _Uid;
            model.Name = nameof(FileDropSettings.Name);
            Instance = model;
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, auditDetails);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}