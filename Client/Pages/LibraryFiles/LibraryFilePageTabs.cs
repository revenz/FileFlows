using System.Runtime.CompilerServices;
using FileFlows.Client.Components.Common;
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

    private LibraryFilePageTabItem Unprocessed, Processing, OnHold;

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
        
        RefreshStatus(feService.Files.LibraryFileCounts);
        SelectedItem = Unprocessed;
        feService.Files.FileQueueUpdated += FilesOnFileQueueUpdated;
        feService.Files.LibraryFileCountsUpdated += FilesOnLibraryFileCountsUpdated;
        feService.Files.OnHoldUpdated += FilesOnOnHoldUpdated;
        feService.Runner.RunnerInfoUpdated += RunnerOnRunnerInfoUpdated;
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

    private void FilesOnOnHoldUpdated(List<LibraryFileMinimal> onHoldItems)
    {
        if (OnHold.Count == onHoldItems.Count) return;
        OnHold.Count = onHoldItems.Count;
        if (OnHold.Count == 0)
            Items.Remove(OnHold);
        else if (Items.Contains(OnHold) == false)
            Items.Add(OnHold);
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
            if (s is FileStatus.Unprocessed or FileStatus.Processing or FileStatus.OnHold)
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
            if (status.Status is FileStatus.Unprocessed or FileStatus.Processing or FileStatus.OnHold)
                continue; // we track these separately

            string icon = status.Status switch
            {
                FileStatus.Disabled => "fas fa-toggle-off",
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
        feService.Files.LibraryFileCountsUpdated -= FilesOnLibraryFileCountsUpdated;;
        feService.Files.OnHoldUpdated -= FilesOnOnHoldUpdated;
    }
}