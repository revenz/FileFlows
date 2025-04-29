using FileFlows.Plugin;
using FileFlows.RemoteServices;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Flow element that downloads the file from the remote server
/// </summary>
public class FileDownloader : Node
{
    /// <summary>
    /// The run instance running this
    /// </summary>
    private readonly RunInstance runInstance;
    
    /// <summary>
    /// The cancelation token source
    /// </summary>
    CancellationTokenSource cancellation = new CancellationTokenSource();
    
    /// <summary>
    /// Creates a new instance of the file downloader
    /// </summary>
    /// <param name="runInstance">the run instance running this</param>
    public FileDownloader(RunInstance runInstance)
    {
        this.runInstance = runInstance;
    }
    
    /// <summary>
    /// Executes the flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output to call</returns>
    public override int Execute(NodeParameters args)
    {
        string dest = Path.Combine(args.TempPath, new FileInfo(args.LibraryFileName).Name);
        var downloader = new ServerShared.FileServices.FileDownloader(args.Logger, RemoteService.ServiceBaseUrl, 
            runInstance.Properties.Uid, RemoteService.AccessToken, args.Node.Uid);
        downloader.OnProgress += (percent, eta, speed) =>
        {
            args.PartPercentageUpdate(percent);
            
            args.RecordAdditionalInfo("Progress", percent + "%", 1, new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("Speed", speed, 1, new TimeSpan(0, 1, 0));
            args.RecordAdditionalInfo("ETA", eta == null ? null : Plugin.Helpers.TimeHelper.ToHumanReadableString(eta.Value),1, new TimeSpan(0, 1, 0));
        };
        var result = downloader.DownloadFile(args.LibraryFileName, dest, cancellationToken: cancellation.Token).Result;
        if (result.IsFailed)
        {
            args.Logger?.ELog("Failed to remotely download file: " + result.Error);
            return -1;
        }

        if (args.Variables.TryGetValue("file.Orig.Size", out object? value) == false || value == null || value is long tSize == false ||
            tSize <= 0)
        {
            var size = new FileInfo(dest).Length;
            args.Variables["file.Orig.Size"] = size;
            args.Logger?.ILog("Set file.Orig.Size to: " + size);
        }

        args.SetWorkingFile(dest);
        return 1;
    }
    
    /// <summary>
    /// Cancels the conversion
    /// </summary>
    /// <returns>the task to await</returns>
    public override Task Cancel()
    {
        cancellation.Cancel();
        return base.Cancel();
    }
}