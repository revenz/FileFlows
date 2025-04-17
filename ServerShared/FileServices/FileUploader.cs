using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using FileFlows.Plugin;
using HttpMethod = System.Net.Http.HttpMethod;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Provides methods to upload files along with their hashes for integrity verification.
/// </summary>
public class FileUploader
{
    /// <summary>
    /// The logger used for log messages
    /// </summary>
    private readonly ILogger logger;
    
    /// <summary>
    /// The URL where the file will be uploaded.
    /// </summary>
    private readonly string serverUrl;
    
    /// <summary>
    /// The UID of the executor for the authentication
    /// </summary>
    private readonly Guid executorUid;
    /// <summary>
    /// The api token
    /// </summary>
    private readonly string _accessToken;
    /// <summary>
    /// The remote node UID
    /// </summary>
    private readonly Guid RemoteNodeUid;
    
    /// <summary>
    /// Constructs an instance of the file uploader
    /// </summary>
    /// <param name="logger">the logger to use in the file uploader</param>
    /// <param name="serverUrl">The URL where the file will be uploaded.</param>
    /// <param name="executorUid">The UID of the executor for the authentication</param>
    /// <param name="accessToken">the Access token to use</param>
    /// <param name="remoteNodeUid">the UID of the remote node</param>
    public FileUploader(ILogger logger, string serverUrl, Guid executorUid, string accessToken, Guid remoteNodeUid)
    {
        this.logger = logger;
        this.serverUrl = serverUrl;
        this.executorUid = executorUid;
        this._accessToken = accessToken;
        this.RemoteNodeUid = remoteNodeUid;
    }
    
    /// <summary>
    /// Static HttpClient instance used for file uploading operations.
    /// </summary>
    private static readonly HttpClient _client;

    /// <summary>
    /// Static constructor
    /// </summary>
    static FileUploader()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        _client = new HttpClient(handler);
        _client.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// Checks if the server path can be written to
    /// </summary>
    /// <param name="serverPath">the path on the server</param>
    /// <returns>true if can be written to, otherwise false</returns>
    private async Task<Result<bool>> CanWriteToPath(string serverPath)
    {
        Func<string, Result<bool>> error = (errorMessage) =>
        {
            logger?.WLog(errorMessage);
            return Result<bool>.Fail(errorMessage);
        };
        
        try
        {
            string url = serverUrl;
            if (url.EndsWith("/") == false)
                url += "/";
            url += "remote/file-server/can-write-to";
            
            string json = JsonSerializer.Serialize(new
            {
                Path = serverPath
            });
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-executor", executorUid.ToString());
            if(string.IsNullOrWhiteSpace(_accessToken) == false)
                request.Headers.Add("x-token", _accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");;
            var response = await _client.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode == false)
                return error((int)response.StatusCode == 503
                    ? "Failed to check can write to on server"
                    : $"Failed to check can write to with status code: {response.StatusCode}");

            if (body.ToLowerInvariant() == "true")
                return true;
            return Result<bool>.Fail("Not allowed to write to path: " + serverPath);
        }
        catch (Exception ex)
        {
            return error($"An error occurred while attempting to checking path access: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Uploads a file to the specified URL, along with its hash for integrity verification.
    /// </summary>
    /// <param name="filePath">The path of the file to be uploaded.</param>
    /// <param name="serverPath">The path on the server where the file will be saved.</param>
    /// <returns>A tuple indicating upload success (bool) and an error message (string), if any.</returns>
    public async Task<(bool Success, string Error)> UploadFile(string filePath, string serverPath)
    {
        Func<string, (bool Success, string Error)> error = (errorMessage) =>
        {
            logger?.WLog(errorMessage);
            return (false, errorMessage);
        };
        
        try
        {
            if (File.Exists(filePath) == false)
                return error($"File does not exist: '{filePath}");

            if ((await CanWriteToPath(serverPath)).Failed(out var errorString))
            {
                logger.ILog(errorString);
                return (false, errorString);
            }
            
            if(serverPath.Length > 2 && serverPath[1] != ':')
                serverPath = serverPath.Replace("\\", "/");

            string url = serverUrl;
            if (url.EndsWith("/") == false)
                url += "/";
            url += "remote/file-server";
            url += "?path=" + HttpUtility.UrlEncode(serverPath);
            logger.ILog("Uploading file to URL: " + url);

            await using var fileStream = FileOpenHelper.OpenRead_NoLocks(filePath);
            var content = new StreamContent(fileStream);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-executor", executorUid.ToString());
            if(string.IsNullOrWhiteSpace(_accessToken) == false)
                request.Headers.Add("x-token", _accessToken);
            request.Content = content;
            var response = await _client.SendAsync(request);
            
            string body = await response.Content.ReadAsStringAsync();
            if(string.IsNullOrWhiteSpace(body) == false)
                logger.ILog("Response from server:\n" + body);
            
            if (response.IsSuccessStatusCode == false)
                return error($"Upload failed with status code: {response.StatusCode}");
            
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return error($"An error occurred during upload: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Calculates the SHA256 hash asynchronously for the file located at the specified file path.
    /// </summary>
    /// <param name="filePath">The path of the file for which the hash needs to be calculated.</param>
    /// <returns>The computed SHA256 hash of the file as a byte array.</returns>
    private static async Task<byte[]> CalculateFileHash(string filePath)
    {
        await using var stream = FileOpenHelper.OpenRead_NoLocks(filePath);
        return await SHA256.Create().ComputeHashAsync(stream);
    }

    /// <summary>
    /// Deletes a remote path
    /// </summary>
    /// <param name="path">the path to delete</param>
    /// <param name="ifEmpty">if the path is a directory, only delete if its empty, for files this is ignored</param>
    /// <param name="includePatterns">
    /// the patterns to include for files to determine if the directory is empty
    /// eg [mkv, avi, mpg, etc] will treat the directory as empty if none of those files are found
    /// </param>
    /// <returns>true if successful, otherwise false</returns>
    public async Task<(bool Success, string Error)> DeleteRemote(string path, bool ifEmpty, string[] includePatterns)
    {
        Func<string, (bool Success, string Error)> error = (errorMessage) =>
        {
            logger?.WLog(errorMessage);
            return (false, errorMessage);
        };
        
        try
        {
            string url = serverUrl;
            if (url.EndsWith("/") == false)
                url += "/";
            url += "remote/file-server/delete";
            
            string json = JsonSerializer.Serialize(new
            {
                Path = path,
                IfEmpty = ifEmpty,
                IncludePatterns = includePatterns
            });
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-executor", executorUid.ToString());
            if(string.IsNullOrWhiteSpace(_accessToken) == false)
                request.Headers.Add("x-token", _accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");;
            var response = await _client.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();
            
            if(string.IsNullOrWhiteSpace(body) == false)
                logger?.ILog(body);

            if (response.IsSuccessStatusCode == false)
                return error((int)response.StatusCode == 503
                    ? "Failed to delete on server"
                    : $"Failed to delete with status code: {response.StatusCode}");

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return error($"An error occurred while attempting to delete: {ex.Message}");
        }
    }
}
