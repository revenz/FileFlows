using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend.Handlers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Library page tabs
/// </summary>
public class LibraryFilePageTabs : FlowPageTabs<LibraryStatus>, IDisposable
{
    /// <summary>
    /// Gets or sets the status chagned event
    /// </summary>
    [Parameter]
    public Action<FileStatus> OnStatusChanged { get; set; }

    private LibraryFilePageTabItem Unprocessed, Processing, OnHold, Disabled, OutOfSchedule, Failed, Successful;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Unprocessed = new LibraryFilePageTabItem()
        {
            Status = FileStatus.Unprocessed,
            Icon = "fas fa-hourglass",
            Count = feService.Files.FileQueue.Count,
            Name = Translater.Instant("Enums.FileStatus.Unprocessed")
        };
        Processing = new LibraryFilePageTabItem()
        {
            Status = FileStatus.Processing,
            Icon = "fas fa-file-medical-alt",
            Count = feService.Runner.Runners.Count,
            Name = Translater.Instant("Enums.FileStatus.Processing")
        };
        OnHold = new LibraryFilePageTabItem()
        {
            Status = FileStatus.OnHold,
            Icon = "fas fa-hand-paper",
            Count = feService.Files.OnHold.Count,
            Name = Translater.Instant("Enums.FileStatus.OnHold")
        };
        Disabled = new LibraryFilePageTabItem()
        {
            Status = FileStatus.Disabled,
            Icon = "fas fa-toggle-off",
            Count = feService.Files.Disabled.Count,
            Name = Translater.Instant("Enums.FileStatus.Disabled")
        };
        OutOfSchedule = new LibraryFilePageTabItem()
        {
            Status = FileStatus.OutOfSchedule,
            Icon = "fas fa-clock",
            Count = feService.Files.OutOfSchedule.Count,
            Name = Translater.Instant("Enums.FileStatus.OutOfSchedule")
        };
        Failed = new LibraryFilePageTabItem()
        {
            Status = FileStatus.ProcessingFailed,
            Icon = "far far fa-times-circle",
            Count = feService.Files.FailedFilesTotal,
            Name = Translater.Instant("Enums.FileStatus.ProcessingFailed")
        };
        Successful = new LibraryFilePageTabItem()
        {
            Status = FileStatus.Processed,
            Icon = "far fa-check-circle",
            Count = feService.Files.SuccessfulTotal,
            Name = Translater.Instant("Enums.FileStatus.Processed")
        };
        
        feService.Files.FileQueueUpdated += FilesOnFileQueueUpdated;
        feService.Files.LibraryFileCountsUpdated += FilesOnLibraryFileCountsUpdated;
        feService.Files.OnHoldUpdated += FilesOnOnHoldUpdated;
        feService.Files.OutOfScheduleUpdated += FilesOutOfScheduleUpdated;
        feService.Files.DisabledUpdated += FilesDisabledUpdated;
        feService.Files.SuccessfulUpdated += FilesOnSuccessfulUpdated;
        feService.Files.FailedFilesUpdated += FilesOnFailedFilesUpdated;
        feService.Runner.RunnerInfoUpdated += RunnerOnRunnerInfoUpdated;
        
        RefreshStatus(feService.Files.LibraryFileCounts);
        SelectedItem = Unprocessed;
    }

    private void FilesOnFailedFilesUpdated(FileHandler.ListAndCount obj)
    {
        if (Failed.Count == obj.Total) return;
        Failed.Count = obj.Total;
        StateHasChanged();
    }

    private void FilesOnSuccessfulUpdated(FileHandler.ListAndCount obj)
    {
        if (Successful.Count == obj.Total) return;
        Successful.Count = obj.Total;
        StateHasChanged();
    }

    private void FilesOnFileQueueUpdated(List<LibraryFileMinimal> obj)
    {
        if (Unprocessed.Count == obj.Count) return;
        Unprocessed.Count = obj.Count;
        StateHasChanged();
    }

    private void RunnerOnRunnerInfoUpdated(List<FlowExecutorInfoMinified> obj)
    {
        if (Processing.Count == obj.Count) return;
        Processing.Count = obj.Count;
        StateHasChanged();
    }

    private void FilesOnOnHoldUpdated(List<LibraryFileMinimal> items)
    {
        if (OnHold.Count == items.Count) return;
        OnHold.Count = items.Count;
        if (OnHold.Count == 0)
            Items.Remove(OnHold);
        else if (Items.Contains(OnHold) == false)
            Items.Add(OnHold);
        StateHasChanged();
    }
    private void FilesOutOfScheduleUpdated(List<LibraryFileMinimal> items)
    {
        if (OutOfSchedule.Count == items.Count) return;
        OutOfSchedule.Count = items.Count;
        if (OutOfSchedule.Count == 0)
            Items.Remove(OutOfSchedule);
        else if (Items.Contains(OutOfSchedule) == false)
            Items.Add(OutOfSchedule);
        StateHasChanged();
    }
    private void FilesDisabledUpdated(List<LibraryFileMinimal> items)
    {
        if (Disabled.Count == items.Count) return;
        Disabled.Count = items.Count;
        if (Disabled.Count == 0)
            Items.Remove(Disabled);
        else if (Items.Contains(Disabled) == false)
            Items.Add(Disabled);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void AfterItemSelected(FlowPageTabItem item)
    {
        OnStatusChanged?.Invoke(((LibraryFilePageTabItem)item).Status);
    }

    private void FilesOnLibraryFileCountsUpdated(List<LibraryStatus> obj)
    {
        RefreshStatus(obj);
    }

    private void RefreshStatus(List<LibraryStatus> data)
    {
        Items ??= new();

        var order = new List<FileStatus>
        {
            FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed,
            FileStatus.FlowNotFound, FileStatus.ProcessingFailed
        };
        foreach (var s in order)
        {
            if ((int)s < 1 || s is FileStatus.Processing or FileStatus.ProcessingFailed or FileStatus.Processed)
                continue; // we track these separately 

            if (data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
                data.Add(new LibraryStatus { Status = s });
        }

        foreach (var s in data)
            s.Name = Translater.Instant("Enums.FileStatus." + s.Status);

        foreach (var status in data.OrderBy(x =>
                 {
                     int index = order.IndexOf(x.Status);
                     return index >= 0 ? index : 100;
                 }))
        {
            if ((int)status.Status < 1 || status.Status is FileStatus.Processing or FileStatus.ProcessingFailed or FileStatus.Processed)
                continue; // we track these separately

            string icon = status.Status switch
            {
                FileStatus.Processed => "far fa-check-circle",
                FileStatus.FlowNotFound => "fas fa-exclamation",
                FileStatus.ProcessingFailed => "far fa-times-circle",
                FileStatus.Duplicate => "far fa-copy",
                FileStatus.MappingIssue => "fas fa-map-marked-alt",
                FileStatus.MissingLibrary => "fas fa-trash",
                FileStatus.ReprocessByFlow => "fas fa-redo",
                _ => ""
            };
            if (status.Status != FileStatus.Unprocessed &&
                status.Status != FileStatus.Processing &&
                status.Status != FileStatus.Processed &&
                status.Status != FileStatus.ProcessingFailed &&
                status.Count == 0)
                continue;

            var sbItem = Items.FirstOrDefault(x => ((LibraryFilePageTabItem)x).Status == status.Status);
            if (sbItem != null)
                sbItem.Count = status.Count;
            else
            {
                Items.Add(new LibraryFilePageTabItem()
                {
                    Count = status.Count,
                    Icon = icon,
                    Name = status.Name,
                    Status = status.Status
                });
            }
        }

        if (Items.Contains(Unprocessed) == false)
            Items.Insert(0, Unprocessed);
        if (Items.Contains(Processing) == false)
            Items.Insert(1, Processing);
        if (Items.Contains(Successful) == false)
            Items.Insert(2, Successful);
        if (Items.Contains(Failed) == false)
            Items.Insert(3, Failed);
        
        if (Disabled.Count > 0 && Items.Contains(Disabled) == false)
            Items.Add(Disabled);
        if (OutOfSchedule.Count > 0 && Items.Contains(OutOfSchedule) == false)
            Items.Add(OutOfSchedule);
        if (OnHold.Count > 0 && Items.Contains(OnHold) == false)
            Items.Add(OnHold);

        StateHasChanged();
    }

    /// <summary>
    /// The items in the page tabs
    /// </summary>
    public class LibraryFilePageTabItem : FlowPageTabItem
    {
        /// <summary>
        /// Gets or sets the status
        /// </summary>
        public FileStatus Status { get; init; }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.FileQueueUpdated -= FilesOnFileQueueUpdated;
        feService.Files.LibraryFileCountsUpdated -= FilesOnLibraryFileCountsUpdated;
        feService.Files.OnHoldUpdated -= FilesOnOnHoldUpdated;
        feService.Files.OutOfScheduleUpdated -= FilesOutOfScheduleUpdated;
        feService.Files.DisabledUpdated -= FilesDisabledUpdated;
        feService.Runner.RunnerInfoUpdated -= RunnerOnRunnerInfoUpdated;
    }
}