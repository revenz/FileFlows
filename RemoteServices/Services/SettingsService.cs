using System.Web;

namespace FileFlows.RemoteServices;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsService : RemoteService, ISettingsService
{
    /// <inheritdoc />
    public async Task<Version> GetServerVersion()
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/remote/system/version");
            if (result.Success == false || result.Data == null)
                throw new Exception(result.Body);
            return new Version(result.Data);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get server version: " + ex.Message);
            return new Version(Globals.Version);
        }
    }

    /// <inheritdoc />
    public async Task<Settings> Get()
    {
        try
        {
            var result = await HttpHelper.Get<Settings>($"{ServiceBaseUrl}/remote/configuration/settings");
            if (result.Success == false || result.Data == null)
                throw new Exception(result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get server version: " + ex.Message);
            return null!;
        }
    }

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public async Task<int> GetCurrentConfigurationRevision()
    {
        const string errorPrefix = "Failed to get FileFlows current configuration revision: ";
        try
        {
            var result = await HttpHelper.Get<int>($"{ServiceBaseUrl}/remote/configuration/revision");
            if (result.Success == false)
                throw new Exception(errorPrefix + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            if(ex.Message.StartsWith(errorPrefix))
                Logger.Instance?.WLog(ex.Message);
            else
                Logger.Instance?.WLog("Failed to get FileFlows current configuration revision: " + ex.Message);
            return -1;
        }
    }

    /// <summary>
    /// Gets the current configuration revision
    /// </summary>
    /// <returns>the current configuration revision</returns>
    public async Task<ConfigurationRevision?> GetCurrentConfiguration()
    {
        try
        {
            var result = await HttpHelper.Get<ConfigurationRevision>($"{ServiceBaseUrl}/remote/configuration/current-config");
            if (result.Success == false)
                throw new Exception("Failed to get FileFlows current configuration: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get FileFlows current configuration: " + ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> DownloadPlugin(string name, string destinationPath)
    {
        try
        {
            string url = $"{ServiceBaseUrl}/remote/configuration/download-plugin/{HttpUtility.UrlEncode(name)}";
            var result = await HttpHelper.Get<byte[]>(url);
            if (result.Success == false)
                return Result<string>.Fail($"Failed to download plugin '{name}': " + result.Body);
            string output = Path.Combine(destinationPath, name);
            await File.WriteAllBytesAsync(output, result.Data!);
            return output;
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to download plugin '{name}': " + ex.Message);
        }
    }
}
