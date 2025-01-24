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
        lblPageAudio, lblPageAudioDescription, lblPageAudio1, lblPageAudio1Description, lblPageAudio2, lblPageAudio2Description, 
        lblPageSubtitle, lblPageSubtitleDescription, lblPageOutput, lblPageOutputDescription,
        lblQuality, lblQualityDescription, lblBitrate, lblBitrateDescription, 
        lblTypeFilm, lblTypeFilmDescription, lblTypeTV, lblTypeTVDescription, lblTypeVideo, lblTypeVideoDescription,
        lblDontConvertAudio, lblDontConvertAudioDescription, lblConvertAudio, lblConvertAudioDescription,
        lblKeepAllSubtitles, lblKeepAllSubtitlesDescription, lblKeepOnlySpecifiedSubtitles, lblKeepOnlySpecifiedSubtitlesDescription,
        lblReplaceOriginal, lblReplaceOriginalDescription, lblSaveToFolder, lblSaveToFolderDescription;

    /// <summary>
    /// Gets the selected encoding type
    /// </summary>
    private int SelectedVideoEncodingType = 0;
    private int Quality = 22, Bitrate = 5000;
    private bool CropBlackBars;
    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguges = [];
    
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
    private string VideoCodec = "h265", VideoContainer = "MKV", Audio1Codec = "aac", Audio2Codec = "aac", DefaultLanguage = "eng", 
        OutputPath;
    private int VideoEncoderType, SelectedType, Audio1Channels, Audio2Channels;
    private bool ConvertAudio = false, TwoAudioVersions = false, ReplaceOriginal = true, DeleteOld = false, SubtitleKeepOnly = false;
    /// <summary>
    /// The new libraries extensions
    /// </summary>
    private string[] Extensions = [];

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> VideoCodecs = [], VideoContainers = [], AudioCodecs = [], AudioChannels = [], LanguageOptions = [];
    
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
    /// Gets or sets bound audio 1 codec
    /// </summary>
    private object BoundAudio1Codec
    {
        get => Audio1Codec;
        set
        {
            if (value is string codec)
                Audio1Codec = codec;
        }
    }
    /// <summary>
    /// Gets or sets bound audio 2 codec
    /// </summary>
    private object BoundAudio2Codec
    {
        get => Audio2Codec;
        set
        {
            if (value is string codec)
                Audio2Codec = codec;
        }
    }
    /// <summary>
    /// Gets or sets bound audio 1 channels
    /// </summary>
    private object BoundAudio1Channels
    {
        get => Audio1Channels;
        set
        {
            if (value is int channels)
                Audio1Channels = channels;
        }
    }
    /// <summary>
    /// Gets or sets bound audio 2 channels
    /// </summary>
    private object BoundAudio2Channels
    {
        get => Audio2Channels;
        set
        {
            if (value is int channels)
                Audio2Channels = channels;
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
        lblPageAudio2 = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Audio2");
        lblPageAudio2Description = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Audio2Description");
        lblPageSubtitle = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Subtitle");
        lblPageSubtitleDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.SubtitleDescription");
        lblPageOutput = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.Output");
        lblPageOutputDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Pages.OutputDescription");
        
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
        
        lblDontConvertAudio = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.DontConvertAudio");
        lblDontConvertAudioDescription=Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.DontConvertAudioDescription");
        lblConvertAudio= Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ConvertAudio");
        lblConvertAudioDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ConvertAudioDescription");
       
        lblKeepAllSubtitles = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.KeepAllSubtitles");
        lblKeepAllSubtitlesDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.KeepAllSubtitles-Help");
        lblKeepOnlySpecifiedSubtitles = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.KeepOnlySpecifiedSubtitles");
        lblKeepOnlySpecifiedSubtitlesDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.KeepOnlySpecifiedSubtitles-Help");
            
        lblReplaceOriginal = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ReplaceOriginal");
        lblReplaceOriginalDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.ReplaceOriginal-Help");
        lblSaveToFolder = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.SaveToFolder");
        lblSaveToFolderDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.SaveToFolder-Help");
        
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
        AudioChannels =
        [
            new () { Label = lblUseOriginal, Value = 0 },
            new() { Label = "Stereo", Value = 2 },
            new() { Label = "5.1", Value = 6 },
            new() { Label = "7.1", Value = 8 }
        ];

        initDone = true;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    private async Task Save()
    {
        await Editor.Validate();
        if (string.IsNullOrWhiteSpace(VideoFlowName))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
            return;
        }

        if (ReplaceOriginal == false && string.IsNullOrWhiteSpace(OutputPath))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.OutputPathRequired");
            return;
        }

        if (ConvertAudio)
        {
            if (Audio1Languages.Count == 0)
            {
                Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio1LanguageRequired");
                return;
            }

            if (TwoAudioVersions)
            {
                if (Audio2Languages.Count == 0)
                {
                    Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio2LanguageRequired");
                    return;
                }

                if (string.Equals(Audio1Codec, Audio2Codec, StringComparison.InvariantCultureIgnoreCase) &&
                    Audio1Channels == Audio2Channels)
                {
                    Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio2MustBeDifferent");
                    return;
                }
            }
        }

        if (SubtitleKeepOnly)
        {
            if (SubtitleLanguges.Count == 0)
            {
                Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.SubtitleLanguageRequired");
                return;
            }
        }

        Wizard.ShowBlocker("Labels.Saving");

        try
        {
            var builder = new FlowBuilder(VideoFlowName);
            builder.Add(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.VideoFile,
                Outputs = 1
            });
            switch (SelectedType)
            {
                case 0: // film
                    builder.AddAndConnect(new FlowPart()
                    {
                        FlowElementUid = builder.ElementUids.MovieLookup,
                        Outputs = 2,
                        Type = FlowElementType.Logic,
                        Model = ExpandoHelper.ToExpandoObject(new 
                        {
                            UseFolderName = true
                        })
                    });
                    break;
                case 1: // tv
                    builder.AddAndConnect(new FlowPart()
                    {
                        FlowElementUid = builder.ElementUids.TVShowLookup,
                        Outputs = 2,
                        Type = FlowElementType.Logic,
                        Model = ExpandoHelper.ToExpandoObject(new 
                        {
                            UseFolderName = true
                        })
                    });
                    break;
            }
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderStart,
                Outputs = 1,
                Type = FlowElementType.BuildStart
            }, allOutputs: true);
            
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = VideoContainer == "MP4" ? builder.ElementUids.FFmpegBuilderRemuxToMp4 : builder.ElementUids.FFmpegBuilderRemuxToMkv,
                Outputs = 1,
                Type = FlowElementType.BuildPart
            });

            if (string.IsNullOrWhiteSpace(VideoCodec) == false)
            {
                if (CropBlackBars)
                {
                    if (SelectedVideoEncodingType == 1)
                    {
                        // bitrate encode
                        builder.AddAndConnect(new FlowPart()
                        {
                            FlowElementUid = builder.ElementUids.FFmpegBuilderVideoBitrateEncode,
                            Outputs = 1,
                            Type = FlowElementType.BuildPart,
                            Model = ExpandoHelper.ToExpandoObject(new
                            {
                                Codec = VideoCodec,
                                Bitrate
                            })
                        });
                    }
                    else
                    {
                        // quality encode
                        builder.AddAndConnect(new FlowPart()
                        {
                            FlowElementUid = builder.ElementUids.FFmpegBuilderVideoEncode,
                            Outputs = 1,
                            Type = FlowElementType.BuildPart,
                            Model = ExpandoHelper.ToExpandoObject(new
                            {
                                Codec = VideoCodec,
                                Quality,
                                Speed = "medium"
                            })
                        });
                    }
                    
                    builder.AddAndConnect(new FlowPart()
                    {
                        FlowElementUid = builder.ElementUids.FFmpegBuildeCropBlackBars,
                        Outputs = 2,
                        Type = FlowElementType.BuildPart,
                        Model = ExpandoHelper.ToExpandoObject(new
                        {
                            CroppingThreshold = 10
                        })
                    });
                }
            }

            if (ConvertAudio)
            {
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.FFmpegBuilderAudioSetLanguage,
                    Outputs = 2,
                    Type = FlowElementType.BuildPart,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        StreamType = "Both",
                        Language = DefaultLanguage
                    })
                }, allOutputs: true);
                
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.FFmpegBuilderAudioLanguageConverter,
                    Outputs = 2,
                    Type = FlowElementType.BuildPart,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        Languages = Audio1Languages,
                        Codec = Audio1Codec,
                        Channels = Audio1Channels
                    })
                }, allOutputs: true);

                if (TwoAudioVersions)
                {
                    builder.AddAndConnect(new FlowPart()
                    {
                        FlowElementUid = builder.ElementUids.FFmpegBuilderAudioLanguageConverter,
                        Outputs = 2,
                        Type = FlowElementType.BuildPart,
                        Model = ExpandoHelper.ToExpandoObject(new
                        {
                            Languages = Audio2Languages,
                            Codec = Audio2Codec,
                            Channels = Audio2Channels
                        })
                    }, allOutputs: true);
                }
            }

            if (SubtitleKeepOnly)
            {
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.FFmpegBuilderLanguageRemover,
                    Outputs = 2,
                    Type = FlowElementType.BuildPart,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        StreamType = "Subtitle",
                        Languages = SubtitleLanguges,
                        NotMatching = true
                    })
                }, allOutputs: true);
            }
            
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderExecutor,
                Outputs = 2,
                Type = FlowElementType.BuildEnd,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    HardwareDecoding = "auto",
                    Strictness = "experimental"
                })
            }, allOutputs: true);
            

            if (ReplaceOriginal)
            {
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.ReplaceOriginal,
                    Outputs = 1,
                    Type = FlowElementType.Process
                });
            }
            else
            {
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.MoveFile,
                    Outputs = 2,
                    Type = FlowElementType.Process,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        DestinationPath = OutputPath,
                        DeleteOriginal = DeleteOld,
                        MoveFolder = true
                    })
                });

                if (DeleteOld)
                {
                    builder.AddAndConnect(new FlowPart()
                    {
                        FlowElementUid = builder.ElementUids.DeleteSourceDirectory,
                        Outputs = 2,
                        Type = FlowElementType.Process,
                        Model = ExpandoHelper.ToExpandoObject(new
                        {
                            IfEmpty = true,
                            IncludePatterns = new[] { "^((?!sample).)*\\.(mkv|mp4|avi|divx|mov|mp(e)?g)$ " }
                        })
                    });
                }
            }
            
            var saveResult = await HttpHelper.Put<Flow>("/api/flow", builder.Flow);
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
    /// Gets the bitrate per channel for a specified codec
    /// </summary>
    /// <param name="codec">the codec</param>
    /// <returns>the bitrate to use per channel</returns>
    private int GetBitratePerChannel(string codec)
        => codec switch
        {
            "aac" => 64,
            "ac3" => 96,
            "dts" => 192,
            "flac" => 448,
            "opus" => 64,
            _ => 192
        };
    
    /// <summary>
    /// Required validator
    /// </summary>
    private readonly List<Validator> RequiredValidator = new()
    {
        new Required()
    };
    
    
    /// <summary>
    /// Validates the general page
    /// </summary>
    /// <returns>true if successful, otherwise false</returns>
    private async Task<bool> OnGeneralPageAdvanced()
    {
        bool valid = string.IsNullOrWhiteSpace(VideoFlowName) == false;
        if(valid)
            return true;
        await Editor.Validate();
        return false;
    }
    
    /// <summary>
    /// Validates the audio 1 page
    /// </summary>
    /// <returns>true if successful, otherwise false</returns>
    private async Task<bool> OnAudio1PageAdvanced()
    {
        bool valid = Audio1Languages.Count > 0;
        if(valid)
            return true;
        await Editor.Validate();
        return false;
    }
    
    /// <summary>
    /// Validates the audio 2 page
    /// </summary>
    /// <returns>true if successful, otherwise false</returns>
    private async Task<bool> OnAudio2PageAdvanced()
    {
        bool valid = Audio2Languages.Count > 0;
        await Editor.Validate();
        if (valid == false)
            return false;

        if (string.Equals(Audio1Codec, Audio2Codec, StringComparison.InvariantCultureIgnoreCase) &&
            Audio1Channels == Audio2Channels)
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio2MustBeDifferent");
            return false;
        }

        return true;
    }
}

/// <summary>
/// The New VideoFlow Wizard Options
/// </summary>
public class NewVideoFlowWizardOptions : IModalOptions
{
}
