using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New library wizard
/// </summary>
public partial class NewLibraryWizard : IModal
{
    private Editor _Editor;

    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor
    {
        get => _Editor;
        set
        {
            if (_Editor != value && value != null)
            {
                _Editor = value;
                StateHasChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblLibraryType, lblLibraryTypeDescription, lblGeneral, lblGeneralDescription,
        lblFileTypes, lblFileTypesDescription, lblFileExtensions, lblFileExtensionsDescription;

    /// <summary>
    /// Gets the selected library type
    /// </summary>
    private int SelectedLibraryType = 0;
    /// <summary>
    /// Gets the selected file type
    /// </summary>
    private int SelectedFileType = 0;

    /// <summary>
    /// The int value for custom file types
    /// </summary>
    private int CustomFileTypes;
    
    /// <summary>
    /// The new libraries name
    /// </summary>
    private string LibraryName { get; set; } = string.Empty;
    /// <summary>
    /// The new libraries path
    /// </summary>
    private string LibraryPath {get;set;} = string.Empty;
    /// <summary>
    /// Gets or sets the flow to run against
    /// </summary>
    private Guid FlowUid { get; set; }
    /// <summary>
    /// Gets or sets the flow to run against folder
    /// </summary>
    private Guid FlowUidFolder { get; set; }
    /// <summary>
    /// The new libraries extensions
    /// </summary>
    private string[] Extensions = [];

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> FlowOptions = [];
    /// <summary>
    /// The input options folders
    /// </summary>
    private List<ListOption> FlowOptionsFolders = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    List<RadioListOption> fileTypeOptions = [], libraryTypeOptions = [];
    private bool IsWindows;
    // if the initalization has been done
    private bool initDone;

    /// <summary>
    /// The flows
    /// </summary>
    private Dictionary<Guid, string> Flows = [], FlowsFolders = [];
    
    /// <summary>
    /// Gets or sets flow uid
    /// </summary>
    private object BoundFlowUid
    {
        get => FlowUid;
        set
        {
            if (value is Guid uid)
                FlowUid = uid;
        }
    }

    /// <summary>
    /// Gets or sets flow uid Folder
    /// </summary>
    private object BoundFlowUidFolder
    {
        get => FlowUidFolder;
        set
        {
            if (value is Guid uid)
                FlowUidFolder = uid;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Flows = feService.Flow.Flows.Where(x => x.Type == FlowType.Standard &&  x.FolderFlow == false).ToDictionary(x => x.Uid, x => x.Name);
        FlowOptions = Flows
            .OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption()
            {
                Value = x.Key,
                Label = x.Value
            }).ToList();
        
        FlowsFolders = feService.Flow.Flows.Where(x => x.Type == FlowType.Standard && x.FolderFlow).ToDictionary(x => x.Uid, x => x.Name);
        FlowOptionsFolders = FlowsFolders
            .OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption()
            {
                Value = x.Key,
                Label = x.Value
            }).ToList();

        if (FlowOptions.Count == 0)
        {
            Toast.ShowWarning(Translater.Instant("Pages.Libraries.ErrorMessages.NoFlows"));
            Close();
            return;
        }
        
        IsWindows = feService.Profile.Profile.ServerOS == OperatingSystemType.Windows;
        
        lblLibraryType = Translater.Instant("Dialogs.NewLibraryWizard.Labels.LibraryType");
        lblLibraryTypeDescription = Translater.Instant("Dialogs.NewLibraryWizard.Labels.LibraryTypeDescription");
        lblGeneral = Translater.Instant("Dialogs.NewLibraryWizard.Labels.General");
        lblGeneralDescription = Translater.Instant("Dialogs.NewLibraryWizard.Labels.GeneralDescription");
        lblFileTypes = Translater.Instant("Dialogs.NewLibraryWizard.Labels.FileTypes");
        lblFileTypesDescription = Translater.Instant("Dialogs.NewLibraryWizard.Labels.FileTypesDescription");
        lblFileExtensions = Translater.Instant("Dialogs.NewLibraryWizard.Labels.FileExtensions");
        lblFileExtensionsDescription = Translater.Instant("Dialogs.NewLibraryWizard.Labels.FileExtensionsDescription");

        int indexValue = 0;
        foreach (var (name, icon) in new[]
                 {
                     ("Files", "fas fa-file"),
                     ("Folders", "fas fa-folder"),
                     ("Downloads", "fas fa-cloud-download-alt")
                 })
        {
            libraryTypeOptions.Add(new RadioListOption()
            {
                Icon = icon,
                Value = indexValue++,
                Title = Translater.Instant($"Dialogs.NewLibraryWizard.Labels.LibraryTypes.{name}.Title"),
                Description = Translater.Instant($"Dialogs.NewLibraryWizard.Labels.LibraryTypes.{name}.Description"),
            });
        }
        indexValue = 0;
        foreach (var (name, icon) in new[]
                 {
                     ("Video", "fas fa-video"),
                     ("Audio", "fas fa-headphones"),
                     ("Image", "fas fa-image"),
                     ("Comic", "fas fa-journal-whills"),
                     ("Audiobook", "fas fa-microphone-alt"),
                     ("eBook", "fas fa-book"),
                     ("Custom", "fas fa-file")
                 })
        {
            fileTypeOptions.Add(new RadioListOption()
            {
                Icon = icon,
                Value = indexValue++,
                Title = Translater.Instant($"Dialogs.NewLibraryWizard.FileTypes.{name}"),
                Description = Translater.Instant($"Dialogs.NewLibraryWizard.FileTypes.{name}Description"),
            });
        }

        CustomFileTypes = indexValue - 1;
        initDone = true;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    private async Task Save()
    {
        await Editor.Validate();
        
        if (string.IsNullOrWhiteSpace(LibraryName))
        {
            Toast.ShowError("Dialogs.NewLibraryWizard.Messages.NameRequired");
            return;
        }
        if (string.IsNullOrWhiteSpace(LibraryPath))
        {
            Toast.ShowError("Dialogs.NewLibraryWizard.Messages.PathRequired");
            return;
        }

        var flowUid = SelectedLibraryType == 1 ? FlowUidFolder : FlowUid;
        var flowsDictionary = SelectedLibraryType == 1 ? FlowsFolders : Flows;

        if (flowUid == Guid.Empty || !flowsDictionary.TryGetValue(flowUid, out var flowName))
        {
            Toast.ShowError("Dialogs.NewLibraryWizard.Messages.FlowRequired");
            return;
        }


        Wizard.ShowBlocker("Labels.Saving");

        try
        {
            var library = new Library()
            {
                Name = LibraryName.Trim(),
                Path = LibraryPath.Trim(),
                Flow = new()
                {
                    Uid = flowUid,
                    Name = flowName,
                    Type = typeof(Flow).FullName
                },
                Enabled = true,
                Schedule = new string('1', 672),
                ScanInterval = 3 * 60 * 60,
                FileSizeDetectionInterval = 5
            };
            if (SelectedLibraryType == 1)
                library.Folders = true;
            else
            {
                if (SelectedLibraryType == 2)
                    library.DownloadsDirectory = true;

                switch (SelectedFileType)
                {
                    case 0: // video
                        library.Extensions =
                        [
                            "ts", "mp4", "mkv", "avi", "mpe", "mpeg", "mov", "mpv", "flv", "wmv", "webm", "avchd",
                            "h264", "h265"
                        ];
                        break;
                    case 1: // audio
                        library.Extensions = ["mp3", "wav", "ogg", "aac", "wma", "flac", "alac", "m4a", "m4p"];
                        break;
                    case 2: // images
                        library.Extensions = ["jpg", "jpeg", "jpe", "tiff", "gif", "png", "webp", "tga", "pbm", "bmp"];
                        break;
                    case 3: // comics
                        library.Extensions = ["cbz", "cbr", "cb7", "pdf", "bz2", "gz"];
                        break;
                    case 4: // audiobook
                        library.Extensions = ["m4b", "mp3", "flac", "wma", "m4a", "aac", "wav"];
                        break;
                    case 5: // ebook
                        library.Extensions = ["epub", "mobi", "pdf", "azw"];
                        break;
                    case 6: // custom
                        library.Extensions = Extensions?.ToList() ?? [];
                        break;
                }
            }
            
            var saveResult = await HttpHelper.Post<Library>("/api/library", library);
            if (saveResult.Success == false)
            {
                Wizard.HideBlocker();
                Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return;
            }
            
            TaskCompletionSource.TrySetResult(saveResult.Data); 
        }
        catch(Exception)
        {
            Wizard.HideBlocker();
        }
    }
    
    /// <summary>
    /// Required validator
    /// </summary>
    private readonly List<Validator> RequiredValidator = new()
    {
        new Required()
    };
}

/// <summary>
/// The New Library Wizard Options
/// </summary>
public class NewLibraryWizardOptions : IModalOptions
{
}

/// <summary>
/// Radio list option
/// </summary>
public class RadioListOption
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; init; }
    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; init; }
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    public string Icon { get; init; }
    
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public int Value { get; init; }
}