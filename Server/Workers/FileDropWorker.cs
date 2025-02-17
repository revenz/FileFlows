using FileFlows.ServerShared.Workers;
using FileFlows.Services;
using FileFlows.Services.FileDropServices;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker responsible for cleaning up old file drop files
/// </summary>
public class FileDropWorker : Worker
{
    /// <summary>
    /// Constructs a new instance of the worker
    /// </summary>
    public FileDropWorker() : base(ScheduleType.Hourly, 1, false)
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
        if (file.Status != FileStatus.ProcessingFailed)
            return; // it didnt fail, nothing to do
        
        if (file.Additional?.FileDropUserUid == null || file.Additional?.TokenCost == null ||
            file.Additional.TokenCost < 1)
            return; // nothing to do 

        _ = Task.Run(async () =>
        {
            // give the user the tokens back
            var userService = ServiceLoader.Load<FileDropUserService>();
            await userService.GiveTokens(file.Additional.FileDropUserUid.Value, file.Additional.TokenCost);
        });
    }

    /// <inheritdoc />
    protected override void Execute()
    {
        CleanUpFileDropFiles();
        GiveTokens().Wait();
    }

    /// <summary>
    /// Gives any tokens to users
    /// </summary>
    async Task GiveTokens()
    {
        if (LicenseService.IsLicensed(LicenseFlags.FileDrop) == false)
            return;

        var settings = ServiceLoader.Load<FileDropSettingsService>().Get();
        if (settings.AutoTokens == false || settings.AutoTokensAmount <= 0 || settings.AutoTokensPeriodMinutes < 60)
            return;

        var service = ServiceLoader.Load<FileDropUserService>();
        var fdUsers = await service.GetAll();
        foreach (var fdUser in fdUsers)
        {
            // Check if they received tokens in this interval or not
            if (fdUser.LastAutoTokensUtc >= DateTime.UtcNow.AddMinutes(-settings.AutoTokensPeriodMinutes))
                continue;

            if (fdUser.Tokens < settings.AutoTokensMaximum)
            {
                // Give them the tokens, make sure their tokens aren't greater than the max
                int newTotal = Math.Min(settings.AutoTokensMaximum, fdUser.Tokens + settings.AutoTokensAmount);
                await service.SetTokens(fdUser.Uid, newTotal, autoTokens: true);
            }
            else
            {
                // Update the LastAutoTokensUtc when no tokens were given
                fdUser.LastAutoTokensUtc = DateTime.UtcNow;
                await service.Update(fdUser);
            }
        }

    }
    
    /// <summary>
    /// Cleans up any file drop files
    /// </summary>
    void CleanUpFileDropFiles()
    {
        var path = Path.Combine(DirectoryHelper.ManualLibrary, "file-drop-users");
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