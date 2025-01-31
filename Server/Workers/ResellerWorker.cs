using FileFlows.ServerShared.Workers;
using FileFlows.Services;
using FileFlows.Services.ResellerServices;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker responsible for cleaning up old reseller files
/// </summary>
public class ResellerWorker : Worker
{
    /// <summary>
    /// Constructs a new instance of the worker
    /// </summary>
    public ResellerWorker() : base(ScheduleType.Hourly, 3, false)
    {
        var service = ServiceLoader.Load<FlowRunnerService>();
        service.OnFileFinished += ServiceOnOnFileFinished;
    }

    /// <summary>
    /// Called when a file finishes processing
    /// </summary>
    /// <param name="file">the file that finished processing</param>
    private void ServiceOnOnFileFinished(LibraryFile file)
    {
        if (file.Additional?.ResellerUserUid == null || file.Additional?.ResellerFlowUid == null)
            return; // nothing to do 

        if (file.Status != FileStatus.ProcessingFailed)
            return; // it didnt fail, nothing to do

        _ = Task.Run(async () =>
        {
            var resellerFlow = await ServiceLoader.Load<ResellerFlowService>().GetByUid(file.Additional.ResellerFlowUid.Value);
            if (resellerFlow == null)
                return;

            if (resellerFlow.Tokens < 1)
                return; // no tokens
            
            // give the user the tokens back
            var userService = ServiceLoader.Load<ResellerUserService>();
            await userService.GiveTokens(file.Additional.ResellerUserUid.Value, resellerFlow.Tokens);
        });
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        var path = Path.Combine(DirectoryHelper.ManualLibrary, "reseller-users");
        if (Directory.Exists(path) == false)
            return; // nothing to do

        foreach (var file in new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories))
        {
            if (file.CreationTime > DateTime.Now.AddDays(-7))
                continue;
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                // Ignore
            }

            if (DirectoryIsEmpty(file.Directory.FullName))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }
    }

    /// <summary>
    /// Checks if a directory is empty.
    /// </summary>
    /// <param name="path">The path to the directory to check.</param>
    /// <returns>True if the directory exists and is empty (no files or subdirectories), false if the directory does not exist, is null, empty, or contains files or subdirectories.</returns>
    private bool DirectoryIsEmpty(string path)
    {
        try
        {
            // Check if the path is null or empty
            if (string.IsNullOrEmpty(path))
                return false; // Invalid path, return false

            // Check if the directory exists
            if (Directory.Exists(path) == false)
                return false; // Directory doesn't exist, return false

            // Get the files and subdirectories in the directory
            var files = Directory.GetFiles(path);
            var subdirectories = Directory.GetDirectories(path);

            // Return true if both files and subdirectories are empty
            return files.Length == 0 && subdirectories.Length == 0;
        }
        catch (Exception)
        {
            return false; // assume its not empty
        }
    }

}