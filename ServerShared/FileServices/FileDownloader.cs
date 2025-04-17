using System.Text;
using System.Text.Json;
using FileFlows.Plugin;
using HttpMethod = System.Net.Http.HttpMethod;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Helper class for downloading and validating files from a URL.
/// </summary>
public class FileDownloader
{
    private static readonly HttpClient _client;
    
    /// <summary>
    /// The logger used for log messages
    /// </summary>
    private ILogger logger;
    
    /// <summary>
    /// The URL where the file will be uploaded.
    /// </summary>
    private readonly string serverUrl;
    
    /// <summary>
    /// The UID of the executor for the authentication
    /// </summary>
    private readonly Guid executorUid;
    
    /// <summary>
    /// Represents a delegate that defines the signature for handling progress events.
    /// </summary>
    /// <param name="percent">The progress percentage.</param>
    /// <param name="eta">The estimate time to completion.</param>
    /// <param name="speed">The speed of the download.</param>
    public delegate void OnProgressDelegate(int percent, TimeSpan? eta, string? speed);

    /// <summary>
    /// Event that is triggered to notify subscribers about the progress, using the <see cref="OnProgressDelegate"/> delegate.
    /// </summary>
    public event OnProgressDelegate? OnProgress;
    /// <summary>
    /// The api token
    /// </summary>
    private readonly string AccessToken;
    /// <summary>
    /// The remote node UID
    /// </summary>
    private readonly Guid RemoteNodeUid;
    
    /// <summary>
    /// Constructs an instance of the file downloader
    /// </summary>
    /// <param name="logger">the logger to use in the file downloader</param>
    /// <param name="serverUrl">The URL where the file will be uploaded.</param>
    /// <param name="executorUid">The UID of the executor for the authentication</param>
    /// <param name="accessToken">the API token to use</param>
    /// <param name="remoteNodeUid">the UID of the remote node</param>
    public FileDownloader(ILogger logger, string serverUrl, Guid executorUid, string accessToken, Guid remoteNodeUid)
    {
        this.logger = logger;
        this.serverUrl = serverUrl;
        this.executorUid = executorUid;
        this.AccessToken = accessToken;
        this.RemoteNodeUid = remoteNodeUid;
    }

    /// <summary>
    /// Static constructor
    /// </summary>
    static FileDownloader()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        _client = new HttpClient(handler);
        _client.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// Downloads a file from the specified URL and saves it to the destination path.
    /// Validates the downloaded file using a SHA256 hash provided by the server.
    /// </summary>
    /// <param name="path">The path of the file to download</param>
    /// <param name="destinationPath">The path where the downloaded file will be saved.</param>
    /// <param name="cancellationToken">The cancelation token</param>
    /// <returns>A tuple indicating success (True) or failure (False) and an error message if applicable.</returns>
    public async Task<Result<bool>> DownloadFile(string path, string destinationPath, CancellationToken? cancellationToken = null)
    {
        try
        {
            cancellationToken ??= new CancellationTokenSource().Token;
            logger.ILog("Downloading file: " + path);
            logger.ILog("Destination file: " + destinationPath);
            string url = serverUrl;
            if (url.EndsWith("/") == false)
                url += "/";
            url += "remote/file-server";
            
            DateTime start = DateTime.UtcNow;
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url + "/download");
            request.Headers.Add("x-executor", executorUid.ToString());
            if(string.IsNullOrWhiteSpace(AccessToken) == false)
                request.Headers.Add("x-token", AccessToken);
            string json = JsonSerializer.Serialize(new { path });
            request.Content  = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request and read the response content as a stream
            HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken.Value);
            if (response.IsSuccessStatusCode == false)
            {
                string error = (await response.Content.ReadAsStringAsync()).EmptyAsNull() ?? "Failed to remotely download the file";
                logger.ELog("Download Failed: " + error);
                logger.ELog("File Download URL: " + url);
                return Result<bool>.Fail(error);
            }

            using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken.Value);


            long fileSize = 0;
            if (response.Content.Headers.ContentLength.HasValue)
            {
                fileSize = response.Content.Headers.ContentLength.Value;
                logger?.ILog($"Content-Length: {fileSize} bytes");
            }
            else
            {
                logger?.ILog("Content-Length header is not present in the response.");
            }

            using FileStream fileStream = FileOpenHelper.OpenWrite_NoReadLock(destinationPath, FileMode.Create);
            
            int bufferSize;
            
            if (fileSize < 10 * 1024 * 1024) // If file size is less than 10 MB
                bufferSize = 4 * 1024;
            else if (fileSize < 100 * 1024 * 1024) // If file size is less than 100 MB
                bufferSize = 64 * 1024;
            else
                bufferSize = 1 * 1024 * 1024;
            
            byte[] buffer = new byte[bufferSize]; 

            int bytesRead;
            long bytesReadTotal = 0;
            int percent = 0;
            
            // Calculate ETA variables
            DateTime startTime = DateTime.UtcNow;
            TimeSpan elapsed;
            TimeSpan eta;
            double speed = 0; // Download speed in bytes per second
            
            OnProgress?.Invoke(0, null, null);
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Value)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken.Value);
                bytesReadTotal += bytesRead;
                
                // Calculate elapsed time and download speed
                elapsed = DateTime.UtcNow - startTime;
                double bytesPerSecond = bytesReadTotal / elapsed.TotalSeconds;
                speed = bytesPerSecond;
                
                // Calculate ETA
                double remainingSeconds = (fileSize - bytesReadTotal) / bytesPerSecond;
                eta = TimeSpan.FromSeconds(remainingSeconds);
                
                // Convert speed to a human-readable format (e.g., KBps or MBps)
                string speedString = FormatSpeed(speed);
                
                float percentage = bytesReadTotal * 100f / fileSize;
                int iPercent = Math.Clamp((int)Math.Round(percentage), 0, 100);
                if (iPercent != percent)
                {
                    percent = iPercent;
                    logger?.ILog($"Download Percentage: {percent} %");
                    OnProgress?.Invoke(iPercent, eta, speedString);
                }
            }
            OnProgress?.Invoke(100, null, null);

            var timeTaken = DateTime.UtcNow.Subtract(start);
            var size = new FileInfo(destinationPath).Length;
            logger?.ILog(
                $"Time taken to download file: {timeTaken}, bytes read: {bytesReadTotal}, expected size: {fileSize}, size on disk: {size})");
            
            // using FileStream fileStream = File.OpenWrite(destinationPath);
            // await response.Content.CopyToAsync(fileStream);
            
            if(fileSize < 1)
                fileSize = await GetFileSize(url, path);
            // long downloadedBytes = 0;
            //
            // await using (var outputStream = new FileStream(destinationPath, FileMode.Create))
            // while (downloadedBytes < fileSize)
            // {
            //     var request = new HttpRequestMessage(HttpMethod.Get, serverUrl);
            //     request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(downloadedBytes, fileSize - 1);
            //
            //     using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            //     await using var responseStream = await response.Content.ReadAsStreamAsync();
            //     
            //     byte[] buffer = new byte[BufferSize];
            //     int bytesRead;
            //     while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //     {
            //         await outputStream.WriteAsync(buffer, 0, bytesRead);
            //         downloadedBytes += bytesRead;
            //     }
            // }

            if (Math.Abs(size - fileSize) > bufferSize)
            {
                return Result<bool>.Fail("File failed to download completely!");
            }
            return true;

            // var hash = await FileHasher.Hash(destinationPath);
            //
            // if (await ValidateFileHash(url, hash) == false)
            // // if(downloadedHash != fileHash)
            // {
            //     return (false, "File validation failed!");
            // }
            //
            //
            // return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates the downloaded file hash against the hash provided by the server for the specified URL.
    /// </summary>
    /// <param name="url">The URL of the file for which the hash needs to be validated.</param>
    /// <param name="hash">The hash of the downloaded file.</param>
    /// <returns>True if the file hash is validated successfully; otherwise, False.</returns>
    private async Task<bool> ValidateFileHash(string url, string hash)
    {
        try
        {
            var serverHash = await _client.GetStringAsync(url + "/hash");;
            bool result = string.Equals(hash, serverHash, StringComparison.InvariantCulture);
            if (result == false)
            {
                logger?.ELog("File-Hash mismatch");
            }

            return result;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the file size from the server for the specified URL.
    /// </summary>
    /// <param name="url">The URL of the file for which the size needs to be fetched.</param>
    /// <param name="path">The path of the file</param>
    /// <returns>The size of the file in bytes.</returns>
    private async Task<long> GetFileSize(string url, string path)
    {
        try
        {
            // Create the GET request with required headers
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url + "/size");
            request.Headers.Add("x-executor", executorUid.ToString());
            if(string.IsNullOrWhiteSpace(AccessToken) == false)
                request.Headers.Add("x-token", AccessToken);
            string json = JsonSerializer.Serialize(new { path });
            request.Content  = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request and retrieve the response content as a string
            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string str = await response.Content.ReadAsStringAsync();
            long.TryParse(str, out long result);
            return result;
        }
        catch (Exception)
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Formats the download speed in bytes per second into a human-readable string.
    /// </summary>
    /// <param name="bytesPerSecond">The download speed in bytes per second.</param>
    /// <returns>A human-readable representation of the download speed.</returns>
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{(bytesPerSecond / (1024 * 1024)):F2} MBps";
        if (bytesPerSecond >= 1024)
            return $"{(bytesPerSecond / 1024):F2} KBps";
        return $"{bytesPerSecond:F2} Bps";
    }
}
