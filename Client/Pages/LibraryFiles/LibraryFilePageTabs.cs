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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        RefreshStatus(feService.Files.LibraryFileCounts);
        SelectedItem = Items.First();
        feService.Files.LibraryFileCountsUpdated += FilesOnLibraryFileCountsUpdated;
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
        
       var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
       foreach (var s in order)
       {
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
           string icon = status.Status switch
           {
               FileStatus.Unprocessed => "far fa-hourglass",
               FileStatus.Disabled => "fas fa-toggle-off",
               FileStatus.Processed => "far fa-check-circle",
               FileStatus.Processing => "fas fa-file-medical-alt",
               FileStatus.FlowNotFound => "fas fa-exclamation",
               FileStatus.ProcessingFailed => "far fa-times-circle",
               FileStatus.OutOfSchedule => "far fa-calendar-times",
               FileStatus.Duplicate => "far fa-copy",
               FileStatus.MappingIssue => "fas fa-map-marked-alt",
               FileStatus.MissingLibrary => "fas fa-trash",
               FileStatus.OnHold => "fas fa-hand-paper",
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
        feService.Files.LibraryFileCountsUpdated -= FilesOnLibraryFileCountsUpdated;
    }
}