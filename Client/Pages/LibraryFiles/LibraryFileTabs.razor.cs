using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Services.Frontend.Handlers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class LibraryFileTabs : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the status chagned event
    /// </summary>
    [Parameter]
    public Action<FileStatus> OnStatusChanged { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]
    private FrontendService feService { get; set; }

    private FileStatus Selected = FileStatus.Unprocessed;


    private LibraryFilePageTabItem Unprocessed,
        Processing,
        Processed,
        ProcessingFailed,
        OutOfSchedule,
        Disabled,
        OnHold;

    protected override void OnInitialized()
    {
        Unprocessed = new(FileStatus.Unprocessed, "fas fa-hourglass", feService.Files.FileQueue.Count);
        Processing = new (FileStatus.Processing, "fas fa-file-medical-alt",feService.Files.Processing.Count);
        OnHold = new(FileStatus.OnHold, "fas fa-hand-paper", feService.Files.OnHold.Count);
        Disabled = new(FileStatus.Disabled, "fas fa-toggle-off", feService.Files.Disabled.Count);
        OutOfSchedule = new(FileStatus.OutOfSchedule, "fas fa-clock", feService.Files.OutOfSchedule.Count);
        ProcessingFailed = new(FileStatus.ProcessingFailed, "far far fa-times-circle", feService.Files.FailedFilesTotal);
        Processed = new(FileStatus.Processed, "far fa-check-circle", feService.Files.SuccessfulTotal);
        
        feService.Files.UnprocessedUpdated += OnUnprocessedUpdated;
        feService.Files.SuccessfulUpdated += OnProcessedUpdated;
        feService.Files.FailedFilesUpdated += OnFailedFilesUpdated;
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
        feService.Files.OutOfScheduleUpdated += OnOutOfScheduleUpdated;
        feService.Files.OnHoldUpdated += OnOnHoldUpdated;
        feService.Files.DisabledUpdated += OnDisabledUpdated;
    }

    private void Select(FileStatus status)
    {
        Selected = status;
    }

    /// <summary>
    /// Called when the unprocessing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnUnprocessedUpdated(List<LibraryFileMinimal> data)
    {
        if (Unprocessed.Count == data.Count) return;
        Unprocessed.Count = data.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> data)
    {
        if (Processing.Count == data.Count) return;
        Processing.Count = data.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnFailedFilesUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (ProcessingFailed.Count == data.Total) return;
        ProcessingFailed.Count = data.Total;
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnProcessedUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (Processed.Count == data.Total) return;
        Processed.Count = data.Total;
        StateHasChanged();
    }
    /// <summary>
    /// 
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void FilesOnUnprocessedUpdated(List<LibraryFileMinimal> data)
    {
        if (Unprocessed.Count == data.Count) return;
        Unprocessed.Count = data.Count;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnOnHoldUpdated(List<LibraryFileMinimal> data)
    {
        if (OnHold.Count == data.Count) return;
        OnHold.Count = data.Count;
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnOutOfScheduleUpdated(List<LibraryFileMinimal> data)
    {
        if (OutOfSchedule.Count == data.Count) return;
        OutOfSchedule.Count = data.Count;
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnDisabledUpdated(List<LibraryFileMinimal> data)
    {
        if (Disabled.Count == data.Count) return;
        Disabled.Count = data.Count;
        StateHasChanged();
    }


    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.UnprocessedUpdated -= OnUnprocessedUpdated;
        feService.Files.SuccessfulUpdated -= OnProcessedUpdated;
        feService.Files.FailedFilesUpdated -= OnFailedFilesUpdated;
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
        feService.Files.OutOfScheduleUpdated -= OnOutOfScheduleUpdated;
        feService.Files.OnHoldUpdated -= OnOnHoldUpdated;
        feService.Files.DisabledUpdated -= OnDisabledUpdated;
    }

    /// <summary>
    /// The items in the page tabs
    /// </summary>
    public class LibraryFilePageTabItem
    {
        /// <summary>
        /// Gets or sets the icon
        /// </summary>
        public string Icon { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the count 
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Gets or sets the status
        /// </summary>
        public FileStatus Status { get; init; }

        public LibraryFilePageTabItem(FileStatus status, string icon, int count)
        {
            Status = status;
            Name = Translater.Instant("Enums.FileStatus." + status);
            Icon = icon;
            Count = count;
        }
    }
}