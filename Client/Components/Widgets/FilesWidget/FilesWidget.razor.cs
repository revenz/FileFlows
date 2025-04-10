using System.IO;
using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Services.Frontend.Handlers;
using FileFlows.Shared.Formatters;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Files Widget
/// </summary>
public partial class FilesWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] Editor Editor { get; set; }

    private FileStatus _FileMode = 0;
    
    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode
    {
        get => (int)_FileMode;
        set
        {
            _FileMode = (FileStatus)value;
            if(initialized)
                _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
        }
    }

    /// <summary>
    /// the selected file status
    /// </summary>
    private FileStatus SelectedStatus => _FileMode;
    

    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private const string LocalStorageKey = "FilesWidget";

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblUpcoming, lblProcessing, lblFinished, lblFailed, lblNoUpcomingFiles, lblNoFailedFiles, lblNoRecentlyFinishedFiles;

    private OptionButtons OptionButtons;
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    /// <summary>
    /// If this component needs rendering
    /// </summary>
    private bool _needsRendering = false;

    private List<LibraryFileMinimal> UpcomingFiles = [], RecentlyFinished = [], FailedFiles = [];
    private int TotalUpcoming, TotalFinished, TotalFailed, TotalProcessing;
    private bool initialized = false;
    
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.Files.Title");
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = 0});
        lblProcessing = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = 0});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = 0});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = 0 });
        lblNoUpcomingFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoUpcomingFiles");
        lblNoRecentlyFinishedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoRecentlyFinishedFiles");
        lblNoFailedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoFailedFiles");
        _FileMode = (FileStatus)Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 4);
        
        InitializeData();
        
        feService.Files.UnprocessedUpdated += UnprocessedUpdated;
        feService.Files.SuccessfulUpdated += OnRecentlyFinishedUpdated;
        feService.Files.FailedFilesUpdated += OnFailedFilesUpdated;
        OnProcessingUpdated(feService.Files.Processing);
        feService.Files.ProcessingUpdated += OnProcessingUpdated;
    }


    /// <summary>
    /// Raised when the executors are updated
    /// </summary>
    /// <param name="info">the executors</param>
    private void OnProcessingUpdated(List<ProcessingLibraryFile> info)
    {
        TotalProcessing = info.Count;
        lblProcessing = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = TotalProcessing});
        OptionButtons?.TriggerStateHasChanged();
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when the failed files list has been updated
    /// </summary>
    /// <param name="lat">the updated list</param>
    private void OnFailedFilesUpdated(FileHandler.ListAndCount<LibraryFileMinimal> lat)
    {
        FailedFiles = lat.Data.Count > 50 ? lat.Data.Take(50).ToList() : lat.Data;
        TotalFailed = feService.Files.FailedFilesTotal;
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = TotalFailed });
        StateHasChanged();
        OptionButtons?.TriggerStateHasChanged();
    }

    /// <summary>
    /// Called when the recently finished files list has been updated
    /// </summary>
    /// <param name="lat">the updated list</param>
    private void OnRecentlyFinishedUpdated(FileHandler.ListAndCount<LibraryFileMinimal> lat)
    {
        RecentlyFinished = lat.Data.Count > 50 ? lat.Data.Take(50).ToList() : lat.Data;
        TotalFinished = feService.Files.ProcessedTotal;
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = TotalFinished});
        StateHasChanged();
        OptionButtons?.TriggerStateHasChanged();
    }

    /// <summary>
    /// Upcoming files have changed
    /// </summary>
    /// <param name="files">the updated files</param>
    private void UnprocessedUpdated(List<LibraryFileMinimal> files)
    {
        UpcomingFiles = files.Count > 50 ? files.Take(50).ToList() : files;
        TotalUpcoming = UpcomingFiles.Count;
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = files.Count});
        StateHasChanged();
        OptionButtons?.TriggerStateHasChanged();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    private void InitializeData()
    {
        UpcomingFiles = feService.Files.Unprocessed.Count > 50 ? feService.Files.Unprocessed.Take(50).ToList() : feService.Files.Unprocessed;
        RecentlyFinished = feService.Files.Processed.Count> 50 ? feService.Files.Processed.Take(50).ToList() : feService.Files.Processed;
        FailedFiles = feService.Files.FailedFiles.Count > 50
            ? feService.Files.FailedFiles.Take(50).ToList()
            : feService.Files.FailedFiles;
        
        TotalUpcoming = UpcomingFiles.Count;
        TotalFailed = feService.Files.FailedFilesTotal;
        TotalFinished = feService.Files.ProcessedTotal;
        TotalProcessing = feService.Files.Processing.Count;
        
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = TotalUpcoming});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = TotalFinished});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = TotalFailed });

        if (initialized == false)
        {
            switch (SelectedStatus)
            {
                case FileStatus.Processing when TotalProcessing == 0:
                case FileStatus.Unprocessed when TotalUpcoming == 0:
                {
                    if (TotalProcessing > 0)
                        FileMode = (int)FileStatus.Processing;
                    else if (TotalUpcoming > 0)
                        FileMode = (int)FileStatus.Unprocessed;
                    else if (TotalFailed > 0 && TotalFinished > 0)
                    {
                        var failed = FailedFiles.Max(x => x.Date);
                        var success = RecentlyFinished.Max(x => x.Date);
                        FileMode = (int)(failed > success ? FileStatus.ProcessingFailed : FileStatus.Processed);
                    }
                    else if (TotalFinished > 0)
                        FileMode = (int)FileStatus.Processed;
                    else if (TotalFailed > 0)
                        FileMode = (int)FileStatus.ProcessingFailed;

                    break;
                }
                case FileStatus.ProcessingFailed when TotalFailed == 0:
                case FileStatus.Processed when TotalFinished == 0:
                {
                    if (TotalProcessing > 0)
                        FileMode = (int)FileStatus.Processing;
                    if (TotalUpcoming > 0)
                        FileMode = (int)FileStatus.Unprocessed;   
                    else if(TotalFinished > 0)
                        FileMode = (int)FileStatus.Processed;
                    else if(TotalFailed > 0)
                        FileMode = (int)FileStatus.ProcessingFailed;
                    break;
                }
            }

            initialized = true;
        }
        StateHasChanged();
        OptionButtons?.TriggerStateHasChanged();
    }

    /// <summary>
    /// Waits for a render to occur
    /// </summary>
    async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }
    
    /// <summary>
    /// Opens the file for viewing
    /// </summary>
    /// <param name="file">the file</param>
    private void OpenFile(LibraryFileMinimal file)
        => _ = Helpers.LibraryFileEditor.Open(Blocker, Editor, file.Uid, Profile, feService);
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Files.UnprocessedUpdated -= UnprocessedUpdated;
        feService.Files.SuccessfulUpdated -= OnRecentlyFinishedUpdated;
        feService.Files.FailedFilesUpdated -= OnFailedFilesUpdated;
        feService.Files.ProcessingUpdated -= OnProcessingUpdated;
    }

    /// <summary>
    /// Gets the thumbnail url
    /// </summary>
    /// <param name="file">the file</param>
    /// <returns>the thumbnail url</returns>
    private string GetThumbUrl(LibraryFileMinimal file)
        => IconHelper.GetThumbnail(file.Uid, file.DisplayName, file.Extension == null);
    
    /// <summary>
    /// Gets the extension image
    /// </summary>
    /// <param name="file">the file</param>
    /// <returns>the extension image</returns>
    private string GetExtensionImage(LibraryFileMinimal file)
        => IconHelper.GetExtensionImage(file.DisplayName);
    
    
    /// <summary>
    /// Humanizes a date, eg 11 hours ago
    /// </summary>
    /// <param name="dateUtc">the date</param>
    /// <returns>the humanized date</returns>
    protected string DateString(DateTime? dateUtc)
    {
        if (dateUtc == null) return string.Empty;
        if (dateUtc.Value.Year < 2020) return string.Empty; // fixes 0000-01-01 issue
        // var localDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour,
        //     date.Value.Minute, date.Value.Second);

        return FormatHelper.HumanizeDate(dateUtc.Value);
    }
}