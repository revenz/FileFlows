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

    /// <summary>
    /// Gets or sets the selected status
    /// </summary>
    [Parameter]
    public FileStatus Selected { get; set; } = FileStatus.Unprocessed;


    private LibraryFilePageTabItem Unprocessed,
        Processing,
        Processed,
        ProcessingFailed,
        OutOfSchedule,
        Disabled,
        OnHold;

    protected override void OnInitialized()
    {
        Unprocessed = new(FileStatus.Unprocessed, "fas fa-hourglass", feService.Files.UnprocessedTotal);
        Processing = new (FileStatus.Processing, "fas fa-file-medical-alt",feService.Files.Processing.Count);
        OnHold = new(FileStatus.OnHold, "fas fa-hand-paper", feService.Files.OnHoldTotal);
        Disabled = new(FileStatus.Disabled, "fas fa-toggle-off", feService.Files.DisabledTotal);
        OutOfSchedule = new(FileStatus.OutOfSchedule, "fas fa-clock", feService.Files.OutOfScheduleTotal);
        ProcessingFailed = new(FileStatus.ProcessingFailed, "far far fa-times-circle", feService.Files.FailedFilesTotal);
        Processed = new(FileStatus.Processed, "far fa-check-circle", feService.Files.ProcessedTotal);
        
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
        OnStatusChanged?.Invoke(status);
    }

    /// <summary>
    /// Called when the unprocessing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnUnprocessedUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (Unprocessed.Count == data.Total) return;
        Unprocessed.Count = data.Total;
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
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnOnHoldUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (OnHold.Count == data.Total) return;
        OnHold.Count = data.Total;
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnOutOfScheduleUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (OutOfSchedule.Count == data.Total) return;
        OutOfSchedule.Count = data.Total;
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the processing data is updated
    /// </summary>
    /// <param name="data">the data</param>
    private void OnDisabledUpdated(FileHandler.ListAndCount<LibraryFileMinimal> data)
    {
        if (Disabled.Count == data.Total) return;
        Disabled.Count = data.Total;
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