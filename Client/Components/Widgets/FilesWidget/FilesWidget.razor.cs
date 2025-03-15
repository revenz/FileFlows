using System.IO;
using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
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

    private int _FileMode = 0;
    private const int MODE_UPCOMING = 1;
    private const int MODE_FINISHED = 0;
    private const int MODE_FAILED = 2;

    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode
    {
        get => _FileMode;
        set
        {
            _FileMode = value;
            if(initialized)
                _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
        }
    }
    

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
    private string lblTitle, lblUpcoming, lblFinished, lblFailed, lblNoUpcomingFiles, lblNoFailedFiles, lblNoRecentlyFinishedFiles;

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


    private List<LibraryFileMinimal> UpcomingFiles, RecentlyFinished, FailedFiles;
    private int TotalUpcoming, TotalFinished, TotalFailed;
    private bool initialized = false;
    
    protected override async Task OnInitializedAsync()
    {
        Profile = feService.Profile.Profile;
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.Files.Title");
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = 0});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = 0});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = 0 });
        lblNoUpcomingFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoUpcomingFiles");
        lblNoRecentlyFinishedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoRecentlyFinishedFiles");
        lblNoFailedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoFailedFiles");
        _FileMode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 2);
        //await RefreshData();
        //feService.FileStatusUpdated += OnFileStatusUpdated;
        InitializeData();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    private void InitializeData()
    {
        UpcomingFiles = feService.Files.UpcomingFiles;
        RecentlyFinished = feService.Files.RecentlyFinished;
        FailedFiles = feService.Files.FailedFiles;
        TotalUpcoming = UpcomingFiles.Count;
        TotalFailed = FailedFiles.Count;
        TotalFinished = RecentlyFinished.Count;
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = TotalUpcoming});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = TotalFinished});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = TotalFailed });

        if (initialized == false)
        {
            switch (FileMode)
            {
                case MODE_UPCOMING when TotalUpcoming == 0:
                {
                    if (TotalFailed > 0 && TotalFinished > 0)
                    {
                        var failed = FailedFiles.Max(x => x.Date);
                        var success = RecentlyFinished.Max(x => x.Date);
                        FileMode = failed > success ? MODE_FAILED : MODE_FINISHED;
                    }
                    else if (TotalFinished > 0)
                        FileMode = MODE_FINISHED;
                    else if (TotalFailed > 0)
                        FileMode = MODE_FAILED;

                    break;
                }
                case MODE_FAILED when TotalFailed == 0:
                {
                    if (TotalUpcoming > 0)
                        FileMode = MODE_UPCOMING;   
                    else if(TotalFinished > 0)
                        FileMode = MODE_FINISHED;
                    break;
                }
                case MODE_FINISHED when TotalFinished == 0:
                {
                    if (TotalUpcoming > 0)
                        FileMode = MODE_UPCOMING;   
                    else if(TotalFailed > 0)
                        FileMode = MODE_FAILED;
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
    /// Loads the data from the server
    /// </summary>
    /// <param name="url">the URL to call</param>
    /// <typeparam name="T">the type of data</typeparam>
    /// <returns>the returned ata</returns>
    private async Task<T> LoadData<T>(string url)
    {
        var result = await HttpHelper.Get<T>(url);
        if(result.Success == false || result.Data == null)
            return default;
        return result.Data;
    }
    
    public record DashboardFile(Guid Uid, string Name, string DisplayName,
        string RelativePath,
        DateTime ProcessingEnded,
        string LibraryName,
        bool IsDirectory,
        string? When,
        long OriginalSize,
        long FinalSize,
        string Message,
        FileStatus Status,
        string[] Traits
    );
    
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
//        ClientService.FileStatusUpdated -= OnFileStatusUpdated;
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
}