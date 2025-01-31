using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Partial class for handling the file upload dialog logic.
/// </summary>
public partial class FileUploadDialog : IModal
{
    private string lblTitle, lblCancel, lblEta, lblSpeed, lblUploaded, lblComplete;
    private long uploadedBytes;
    private long totalBytes;
    private bool isUploading;
    private CancellationTokenSource cts;
    private IBrowserFile FileUploading;
    private DateTime startTime;
    private double uploadSpeed;
    private TimeSpan eta;
    private string formattedSpeed;
    private string formattedEta;
    private string formattedUploaded;
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblCancel = Translater.Instant("Labels.Cancel");
        lblTitle = Translater.Instant("Labels.Upload");
        lblEta = Translater.Instant("Dialogs.FileUpload.ETA");
        lblSpeed = Translater.Instant("Dialogs.FileUpload.Speed");
        lblUploaded = Translater.Instant("Dialogs.FileUpload.UploadedAmount");
        lblComplete = Translater.Instant("Dialogs.FileUpload.Complete");
        if (Options is FileUploadOptions fileUploadOptions == false || fileUploadOptions.File == null)
        {
            Cancel();
            return;
        }

        FileUploading = fileUploadOptions.File;
        Logger.Instance.ILog("File: " + fileUploadOptions.File.Name);
        
        // Begin upload if file is valid
        cts = new CancellationTokenSource();
        await UploadFile(FileUploading, cts.Token);
    }

    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }
    
    /// <summary>
    /// Uploads the file with progress and cancellation support.
    /// </summary>
    private async Task UploadFile(IBrowserFile file, CancellationToken cancellationToken)
    {
        string url = "/api/library-file/upload";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        startTime = DateTime.Now;
        isUploading = true;
        string uploadedFile = string.Empty;

        const int chunkSize = 4 * 1024 * 1024; // 4 MB chunk size
        totalBytes = file.Size;
        int totalChunks = (int)Math.Ceiling((double)totalBytes / chunkSize);
        string fileName = file.Name;

        // Open file stream for reading without using Seek
        using var stream = file.OpenReadStream(maxAllowedSize: long.MaxValue);

        for (int chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
        {
            // Calculate the number of bytes to read in this chunk
            int bytesToRead = (int)Math.Min(chunkSize, totalBytes - (chunkNumber * chunkSize));
            var buffer = new byte[bytesToRead];
        
            // Read the current chunk into the buffer
            int bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken);

            // Prepare multipart form data with current chunk and metadata
            using var form = new MultipartFormDataContent
            {
                { new ByteArrayContent(buffer, 0, bytesRead), "file", fileName },
                { new StringContent(chunkNumber.ToString()), "chunkNumber" },
                { new StringContent(totalChunks.ToString()), "totalChunks" },
                { new StringContent(fileName), "fileName" }
            };

            // Send the chunk to the server
            var response = await HttpHelper.Client.PostAsync(url, form, cancellationToken);
        
            if (!response.IsSuccessStatusCode)
            {
                TaskCompletionSource.TrySetResult("Upload Failed");
                isUploading = false;
                return;
            }


            // Update upload metrics and UI after each chunk
            uploadedBytes += bytesRead;
            UpdateUploadMetrics();
            await InvokeAsync(StateHasChanged);

            // Check if upload was canceled
            if (cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource.TrySetCanceled();
                isUploading = false;
                return;
            }
            uploadedFile = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        // Finalize upload completion
        TaskCompletionSource.TrySetResult(uploadedFile);
        isUploading = false;
    }

    private void UpdateUploadMetrics()
    {
        var elapsedTime = DateTime.Now - startTime;
        uploadSpeed = uploadedBytes / elapsedTime.TotalSeconds;
        eta = TimeSpan.FromSeconds((totalBytes - uploadedBytes) / uploadSpeed);

        formattedSpeed = FormatSpeed(uploadSpeed);
        formattedEta = eta.ToString(@"hh\:mm\:ss");
        formattedUploaded = $"{FormatBytes(uploadedBytes)} / {FormatBytes(totalBytes)}";
    }

    private string FormatSpeed(double speed)
    {
        if (speed > 1_000_000)
            return $"{(speed / 1_000_000):F2} MB/s";
        if (speed > 1000)
            return $"{(speed / 1000):F2} KB/s";
        return $"{speed:F2} Bytes/s";
    }

    private string FormatBytes(double bytes)
    {
        if (bytes >= 1_000_000_000) // GB
            return $"{bytes / 1_000_000_000:F2} GB";
        if (bytes >= 1_000_000) // MB
            return $"{bytes / 1_000_000:F2} MB";
        if (bytes >= 1000) // KB
            return $"{bytes / 1000:F2} KB";
        return $"{bytes:F2} Bytes";
    }
}

/// <summary>
/// The File Upload Options
/// </summary>
public class FileUploadOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the file being uploaded
    /// </summary>
    public IBrowserFile File { get; set; }
}