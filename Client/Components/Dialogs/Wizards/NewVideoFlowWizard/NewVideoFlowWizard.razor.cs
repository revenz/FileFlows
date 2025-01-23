using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs.Wizards;

/// <summary>
/// New videoFlow wizard
/// </summary>
public partial class NewVideoFlowWizard : IModal
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
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }
    
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
    private string lblTitle, lblUseOriginal, lblPageType, lblPageTypeDescription, 
        lblPageVideo, lblPageVideoDescription, lblGeneral, lblGeneralDescription,
        lblPageAudio, lblPageAudioDescription, lblPageAudio1, lblPageAudio1Description, lblPageSubtitle, lblPageSubtitleDescription,
        lblQuality, lblQualityDescription, lblBitrate, lblBitrateDescription, 
        lblTypeFilm, lblTypeFilmDescription, lblTypeTV, lblTypeTVDescription, lblTypeVideo, lblTypeVideoDescription,
        lblDontConvertAudio, lblDontConvertAudioDescription, lblConvertAudio, lblConvertAudioDescription;

    /// <summary>
    /// Gets the selected encoding type
    /// </summary>
    private int SelectedVideoEncodingType = 0;
    private int Quality = 22, Bitrate = 5000;
    private bool CropBlackBars;
    private List<string> AudioLanguages = [];
    
    /// <summary>
    /// The new libraries name
    /// </summary>
    private string VideoFlowName { get; set; } = string.Empty;
    /// <summary>
    /// The new libraries path
    /// </summary>
    private string VideoFlowPath {get;set;} = string.Empty;

    /// <summary>
    /// Flow properties
    /// </summary>
    private string VideoCodec = "h265", VideoContainer = "MKV", AudioCodec = "aac", DefaultLanguage = "eng";
    private int VideoEncoderType, SelectedType;
    private bool ConvertAudio = false;
    /// <summary>
    /// The new libraries extensions
    /// </summary>
    private string[] Extensions = [];

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> VideoCodecs = [], VideoContainers = [], AudioCodecs = [], LanguageOptions = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    private bool IsWindows;
    // if the initalization has been done
    private bool initDone;
    
    /// <summary>
    /// Gets or sets bound video codec
    /// </summary>
    private object BoundVideoCodec
    {
        get => VideoCodec;
        set
        {
            if (value is string codec)
                VideoCodec = codec;
        }
    }
    
    /// <summary>
    /// Gets or sets bound video container
    /// </summary>
    private object BoundVideoContainer
    {
        get => VideoContainer;
        set
        {
            if (value is string container)
                VideoContainer = container;
        }
    }
    
    /// <summary>
    /// Gets or sets bound default language
    /// </summary>
    private object BoundDefaultLanguage
    {
        get => DefaultLanguage;
        set
        {
            if (value is string v)
                DefaultLanguage = v;
        }
    }
    
    /// <summary>
    /// Gets or sets bound audio codec
    /// </summary>
    private object BoundAudioCodec
    {
        get => AudioCodec;
        set
        {
            if (value is string codec)
                AudioCodec = codec;
        }
    }
    
    /// <summary>
    /// Gets or sets bound video encoder type
    /// </summary>
    private object BoundVideoEncoderType
    {
        get => VideoEncoderType;
        set
        {
            if (value is int type)
                VideoEncoderType = type;
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get(); 
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;
        
        lblTitle = Translater.Instant("Dialogs.NewVideoFlowWizard.Title");
        lblUseOriginal = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.UseOriginal");
        
        lblPageType = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Type");
        lblPageTypeDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.TypeDescription"); 
        lblGeneral = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.General");
        lblGeneralDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.GeneralDescription");
        lblPageVideo = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Video");
        lblPageVideoDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.VideoDescription");
        lblPageAudio = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Audio");
        lblPageAudioDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.AudioDescription");
        lblPageAudio1 = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Audio1");
        lblPageAudio1Description = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Audio1Description");
        lblPageSubtitle = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Subtitle");
        lblPageSubtitleDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.SubtitleDescription");
        
        lblTypeFilm = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.Film");
        lblTypeFilmDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.FilmDescription");
        lblTypeTV = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.TV");
        lblTypeTVDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.TVDescription");
        lblTypeVideo = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.Video");
        lblTypeVideoDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Types.VideoDescription");
        
        lblQuality = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Quality");
        lblQualityDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.QualityDescription");
        lblBitrate = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.Bitrate");
        lblBitrateDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.BitrateDescription");
        
        lblDontConvertAudio= Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.DontConvertAudio");
        lblDontConvertAudioDescription=Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.DontConvertAudioDescription");
        lblConvertAudio= Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ConvertAudio");
        lblConvertAudioDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ConvertAudioDescription");
        
        LanguageOptions = LanguageHelper.Languages.DistinctBy(x => x.Iso2).Select(x =>
        {
            var name = profile.UseFrench ? x.French :
                profile.UseGerman ? x.German :
                x.English;
            name = name?.EmptyAsNull() ?? x.English;
            return new ListOption()
            {
                Label = name, Value = x.Iso2
            };
        }).OrderBy(x => x.Label.ToLowerInvariant()).ToList();
        VideoCodecs =
        [
            new () { Label = lblUseOriginal, Value = ""},
            new() { Label = "H264", Value = "h264" },
            new() { Label = "HEVC", Value = "h265" },
            new() { Label = "HEVC (10-Bit)", Value = "h265 10BIT" },
            new() { Label = "AV1", Value = "av1" },
            new() { Label = "AV1 (10-Bit)", Value = "av1 10BIT" },
            new() { Label = "VP9", Value = "vp9" }
        ];
        VideoContainers =
        [
            new() { Label = "MKV", Value = "MKV" },
            new() { Label = "MP4", Value = "MP4" }
        ];
        AudioCodecs =
        [
            new () { Label = lblUseOriginal, Value = ""},
            new() { Label = "AAC", Value = "aac" },
            new() { Label = "AC3", Value = "ac3" },
            new() { Label = "DTS", Value = "dts" },
            new() { Label = "FLAC", Value = "flac" },
            new() { Label = "OPUS", Value = "opus" }
        ];

        initDone = true;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(VideoFlowName))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
            return;
        }
        if (string.IsNullOrWhiteSpace(VideoFlowPath))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.PathRequired");
            return;
        }
        
        Wizard.ShowBlocker("Labels.Saving");

        try
        {
            var videoFlow = new Flow();
            
            var saveResult = await HttpHelper.Post<Flow>("/api/videoFlow", videoFlow);
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
/// The New VideoFlow Wizard Options
/// </summary>
public class NewVideoFlowWizardOptions : IModalOptions
{
}
