namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Provides helper methods for donwload operations.
/// </summary>
public class DownloadHelper
{
    private static readonly HttpClient _httpClient;

    /// <summary>
    /// Static constructor to initialize the HTTP client with a handler that ignores SSL certificate errors.
    /// </summary>
    static DownloadHelper()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
    }

    /// <summary>
    /// Downloads a file from the specified URL and saves it to the specified destination.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="destination">The full file path where the downloaded file will be saved.</param>
    /// <param name="maxFileSize">The maximum allowed file size in bytes. Defaults to 10MB.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> object indicating success or failure.
    /// If successful, the <see cref="Result{T}.Data"/> property will be <c>true</c>.
    /// If an error occurs, the <see cref="Result{T}.Error"/> property will contain the error message.
    /// </returns>
    public static Result<bool> Download(string url, string destination, long maxFileSize = 10 * 1024 * 1024)
    {
        try
        {
            using var response = _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;

            if (!response.IsSuccessStatusCode)
                return Result<bool>.Fail($"Failed to download: {response.StatusCode}");

            var contentLength = response.Content.Headers.ContentLength ?? -1;
            if (contentLength > maxFileSize)
                return Result<bool>.Fail("File exceeds maximum allowed size.");

            using var stream = response.Content.ReadAsStreamAsync().Result;
            using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalRead += bytesRead;
                if (totalRead > maxFileSize)
                    return Result<bool>.Fail("File size exceeded limit during download.");

                fileStream.Write(buffer, 0, bytesRead);
            }

            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }
}
