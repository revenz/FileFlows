using FileFlows.Client.Services.Frontend.Handlers;

namespace FileFlows.Client.Pages;

/// <summary>
/// File updates of library files
/// </summary>
public partial class LibraryFiles
{
    void InitializeFileUpdates()
    {
        
        feService.Files.UnprocessedUpdated += OnUnprocessedUpdated;
        feService.Files.FailedFilesUpdated += OnFailedFilesUpdated;
        feService.Files.SuccessfulUpdated += OnSuccessfulUpdated;
    }

    void DisposeFileUpdates()
    {
        
    }
    
    private void OnSuccessfulUpdated(FileHandler.ListAndCount<LibraryFileMinimal> args)
    {
        
        if (SelectedStatus != FileStatus.Processed || PageIndex != 0)
            return;
        Data = args.Data;
        TotalItems = args.Total;
        Table?.TriggerStateHasChanged();
        StateHasChanged();
    }

    private void OnFailedFilesUpdated(FileHandler.ListAndCount<LibraryFileMinimal> args)
    {
        if (SelectedStatus != FileStatus.ProcessingFailed || PageIndex != 0)
            return;
        Data = args.Data;
        TotalItems = args.Total;
        Table?.TriggerStateHasChanged();
        StateHasChanged();
    }

    /// <summary>
    /// Called when the files are updated
    /// </summary>
    /// <param name="files">the files</param>
    /// <param name="total">the total unprocessed</param>
    private void OnUnprocessedUpdated(List<LibraryFileMinimal> files, int total)
    {
        if (SelectedStatus != FileStatus.Unprocessed)
            return;

        Data = files;
        Table?.TriggerStateHasChanged();
        StateHasChanged();
    }
}