using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsManager
{
    private static FairSemaphore _semaphore = new(1);
    // Special case, we always cache the settings, as it is constantly looked up
    private static Settings? Instance;

    static SettingsManager()
    {
        Instance = DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<Settings>().Result;
        if (Instance != null)
            return;
        Instance = new Settings
        {
            Name = "Settings",
            AutoUpdatePlugins = true,
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now
        };
        DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, auditDetails: AuditDetails.ForServer()).Wait();
    }

    /// <summary>
    /// Gets or sets if caching should be used
    /// </summary>
    internal static bool UseCache => true; // Instance.Cache?.UseCache != false;
    
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public Task<Settings> Get() => Task.FromResult(Instance)!;

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public Task<int> GetCurrentConfigurationRevision()
        => Task.FromResult(Instance!.Revision);
    
    /// <summary>
    /// Increments the revision
    /// </summary>
    public async Task RevisionIncrement()
    {
        await _semaphore.WaitAsync();
        try
        {
            Instance!.Revision += 1;
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, null);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to increment revision: " + ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Updates the settings
    /// </summary>
    /// <param name="model">the settings</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Update(Settings model, AuditDetails? auditDetails)
    {
        await _semaphore.WaitAsync();
        try
        {
            model.Revision = Math.Max(model.Revision, Instance!.Revision) +  1;
            Instance = model;
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(Instance, auditDetails);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Gets the open number of database connections
    /// </summary>
    /// <returns>the number of connections</returns>
    public int GetOpenConnections()
        => DatabaseAccessManager.Instance.GetOpenConnections();
}