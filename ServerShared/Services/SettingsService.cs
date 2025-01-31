using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for the Settings service which allows accessing of all the system settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the version of the server
    /// </summary>
    /// <returns>the version of the server</returns>
    Task<Version> GetServerVersion();

    /// <summary>
    /// Getst he settings
    /// </summary>
    /// <returns>the settings</returns>
    Task<Settings> Get();
    
    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    Task<int> GetCurrentConfigurationRevision();
    
    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    Task<ConfigurationRevision?> GetCurrentConfiguration();

    /// <summary>
    /// Downloads a plugin to the destination
    /// </summary>
    /// <returns>A task to await</returns>
    Task<Result<string>> DownloadPlugin(string name, string destinationPath);
}
