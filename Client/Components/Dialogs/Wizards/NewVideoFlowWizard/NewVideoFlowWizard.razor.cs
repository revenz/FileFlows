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
    private string lblUseOriginal, lblPageType, lblPageTypeDescription, 
        lblPageVideo, lblPageVideoDescription, lblGeneral, lblGeneralDescription,
        lblPageAudio, lblPageAudioDescription, lblPageAudio1, lblPageAudio1Description, lblPageAudio2, lblPageAudio2Description, 
        lblPageSubtitle, lblPageSubtitleDescription, lblPageOutput, lblPageOutputDescription,
        lblQuality, lblQualityDescription, lblBitrate, lblBitrateDescription, 
        lblTypeFilm, lblTypeFilmDescription, lblTypeTV, lblTypeTVDescription, lblTypeVideo, lblTypeVideoDescription,
        lblCopyAllAudio, lblCopyAllAudioDescription,
        lblCopyOnlyLanguages, lblCopyOnlyLanguagesDescription, lblConvertAudio, lblConvertAudioDescription,
        lblKeepAllSubtitles, lblKeepAllSubtitlesDescription, lblKeepOnlySpecifiedSubtitles, lblKeepOnlySpecifiedSubtitlesDescription,
        lblReplaceOriginal, lblReplaceOriginalDescription, lblSaveToFolder, lblSaveToFolderDescription;

    /// <summary>
    /// Gets the selected encoding type
    /// </summary>
    private int SelectedVideoEncodingType;
    private int Quality, Bitrate = 5000;
    private bool CropBlackBars;
    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguages = [], AudioMode2Languages = [];
    
    /// <summary>
    /// The new libraries name
    /// </summary>
    private string VideoFlowName { get; set; } = string.Empty;

    /// <summary>
    /// Flow properties
    /// </summary>
    private string VideoCodec = "h265", VideoContainer = "MKV", Audio1Codec = "aac", Audio2Codec = "aac", DefaultLanguage = "eng", 
        OutputPath;
    private int SelectedType, Audio1Channels, Audio2Channels, AudioMode;
    private bool TwoAudioVersions, ReplaceOriginal = true, DeleteOld, SubtitleKeepOnly;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> VideoCodecs = [],
        VideoContainers = [],
        AudioCodecs = [],
        AudioChannels = [],
        LanguageOptions = [],
        QualityOptions = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    private bool IsWindows;
    // if the initialization has been done
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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get(); 
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;

        if (profile.UseGerman)
            Audio1Languages = ["eng", "deu", "orig"];
        else if (profile.UseFrench)
            Audio1Languages = ["eng", "fre", "orig"];
        else
            Audio1Languages = ["eng", "orig"];
        
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
        lblCopyAllAudio = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.CopyAllAudio");
        lblCopyAllAudioDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.CopyAllAudioDescription");
        lblCopyOnlyLanguages = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.CopyOnlyLanguages");
        lblCopyOnlyLanguagesDescription = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.CopyOnlyLanguagesDescription");
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
            // don't include 10-bit here, don't need to confuse users
            // new() { Label = "HEVC (10-Bit)", Value = "h265 10BIT" },
            new() { Label = "AV1", Value = "av1" },
            // new() { Label = "AV1 (10-Bit)", Value = "av1 10BIT" },
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
            // new() { Label = "DTS", Value = "dts" },
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
        QualityOptions =
        [
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Ok"), Value = -2},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Good"), Value = -1},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Recommended"), Value = 0},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.High"), Value = 1},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.VeryHigh"), Value = 2},
        ];

        initDone = true;
        StateHasChanged();
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    private async Task Save()
    {
        if (await ValidateFlow() == false)
            return;

        Wizard.ShowBlocker("Labels.Saving");

        try
        {
            var builder = new FlowBuilder(VideoFlowName);
            builder.MaxRows = 7;
            builder.Add(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.VideoFile,
                Outputs = 1
            });
            FlowAddMetaLookup(builder);
            
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderStart,
                Outputs = 1,
                Type = FlowElementType.BuildStart
            }, allOutputsIncludingFailure: true);
            
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = VideoContainer == "MP4" ? builder.ElementUids.FFmpegBuilderRemuxToMp4 : builder.ElementUids.FFmpegBuilderRemuxToMkv,
                Outputs = 1,
                Type = FlowElementType.BuildPart
            });

            bool videoEncode = string.IsNullOrWhiteSpace(VideoCodec) == false;

            if (videoEncode)
                FlowAddVideo(builder);
            FlowAddAudio(builder);
            FlowAddSubtitles(builder);
            
            var executor = builder.AddAndConnect(new FlowPart()
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


            var preOutputColumn = builder.CurrentColumn;
            FlowPart fpOutput = FlowAddOutput(builder, videoEncode);

            if (videoEncode)
            {
                // is using encoding, so catch an error turn off hardware and try again

                string codecLabel = VideoCodec switch
                {
                    "h265" => "HEVC",
                    _ => VideoCodec.ToUpper()
                };
                builder.CurrentColumn = preOutputColumn + 4;
                builder.CurrentRow = 0;
                var secondEncode = builder.Add(
                    SelectedVideoEncodingType == 1
                        ? new FlowPart()
                        {
                            // bitrate encode
                            FlowElementUid = builder.ElementUids.FFmpegBuilderVideoBitrateEncode,
                            Outputs = 1,
                            Name = codecLabel + " (CPU)",
                            Type = FlowElementType.BuildPart,
                            Model = ExpandoHelper.ToExpandoObject(new
                            {
                                Codec = VideoCodec,
                                Encoder= "CPU",
                                Bitrate
                            })
                        }
                        : new FlowPart()
                        {
                            FlowElementUid = builder.ElementUids.FFmpegBuilderVideoEncode,
                            Outputs = 1,
                            Name = codecLabel + " (Bitrate) (CPU)",
                            Type = FlowElementType.BuildPart,
                            Model = ExpandoHelper.ToExpandoObject(new
                            {
                                Codec = VideoCodec,
                                Encoder= "CPU",
                                Quality = GetQuality(VideoCodec, Quality),
                                Speed = "medium"
                            })
                        }, row: 1);
                builder.Connect(executor, secondEncode, -1);
                
                var secondExecutor = builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = builder.ElementUids.FFmpegBuilderExecutor,
                    Outputs = 2,
                    Type = FlowElementType.BuildEnd,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        HardwareDecoding = false,
                        Strictness = "experimental"
                    })
                }, row: 2);
                
                builder.Connect(secondExecutor, fpOutput, 1);
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
    /// Adds the output flow elements
    /// </summary>
    /// <param name="builder">the flow builder</param>
    /// <param name="videoEncode">if video encoding will occur</param>
    /// <returns>the primary flow output flow part</returns>
    private FlowPart FlowAddOutput(FlowBuilder builder, bool videoEncode)
    {
        int preOutputColumn = builder.CurrentColumn;
        FlowPart fpOutput;
        if (ReplaceOriginal)
        {
            fpOutput = builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.ReplaceOriginal,
                Outputs = 1,
                Type = FlowElementType.Process
            }, row: videoEncode ? 6 : 0, column: videoEncode ? (preOutputColumn + 4) : 0);
        }
        else
        {
            fpOutput = builder.AddAndConnect(new FlowPart()
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
            }, row: videoEncode ? 5 : 0, column: videoEncode ? (preOutputColumn + 4) : 0);

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
                }, row: videoEncode ? 6 : 0, column: videoEncode ? (preOutputColumn + 3) : 0);
            }
        }

        return fpOutput;
    }

    /// <summary>
    /// Validates the flow before saving
    /// </summary>
    /// <returns>true if flow is valid, otherwise false</returns>
    private async Task<bool> ValidateFlow()
    {
        await Editor.Validate();
        if (string.IsNullOrWhiteSpace(VideoFlowName))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
            return false;
        }

        if (ReplaceOriginal == false && string.IsNullOrWhiteSpace(OutputPath))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.OutputPathRequired");
            return false;
        }

        if (AudioMode == 1)
        {
            if (AudioMode2Languages.Count == 0)
            {
                Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.AudioMode2LanguagesRequired");
                return false;
            }
        }
        else if (AudioMode == 2)
        {
            if (Audio1Languages.Count == 0)
            {
                Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio1LanguageRequired");
                return false;
            }

            if (TwoAudioVersions)
            {
                if (Audio2Languages.Count == 0)
                {
                    Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio2LanguageRequired");
                    return false;
                }

                if (string.Equals(Audio1Codec, Audio2Codec, StringComparison.InvariantCultureIgnoreCase) &&
                    Audio1Channels == Audio2Channels)
                {
                    Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.Audio2MustBeDifferent");
                    return false;
                }
            }
        }

        if (SubtitleKeepOnly)
        {
            if (SubtitleLanguages.Count == 0)
            {
                Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.SubtitleLanguageRequired");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Adds the meta lookup flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddMetaLookup(FlowBuilder builder)
    {
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
    }

    /// <summary>
    /// Adds the video flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddVideo(FlowBuilder builder)
    {
        string codecLabel = VideoCodec switch
        {
            "h265" => "HEVC",
            _ => VideoCodec.ToUpper()
        };
        if (SelectedVideoEncodingType == 1)
        {
            // bitrate encode
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderVideoBitrateEncode,
                Outputs = 1,
                Name = codecLabel,
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Codec = VideoCodec,
                    Encoder = "",
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
                Name = codecLabel + " (Bitrate)",
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Codec = VideoCodec,
                    Encoder = "",
                    Quality = GetQuality(VideoCodec, Quality),
                    Speed = "medium"
                })
            });
        }

        if (CropBlackBars)
        {
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

    /// <summary>
    /// Adds the subtitle flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddSubtitles(FlowBuilder builder)
    {
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
                    Languages = SubtitleLanguages,
                    NotMatching = true
                })
            }, allOutputs: true);
        }
    }

    /// <summary>
    /// Adds the audio flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddAudio(FlowBuilder builder)
    {
        if (AudioMode > 1)
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
        }

        if (AudioMode == 1)
        {
            // copy audio only matching languages
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderLanguageRemover,
                Outputs = 2,
                Type = FlowElementType.BuildPart,
                Name = lblCopyOnlyLanguages,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    StreamType = "Audio",
                    Languages = AudioMode2Languages,
                    NotMatching = true
                })
            }, allOutputs: true);
        }
        else if (AudioMode == 2)
        {
            // convert audio
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = builder.ElementUids.FFmpegBuilderAudioLanguageConverter,
                Outputs = 2,
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Languages = Audio1Languages,
                    Codec = Audio1Codec,
                    Channels = Audio1Channels,
                    Bitrate = GetBitratePerChannel(Audio1Codec)
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
                        Channels = Audio2Channels,
                        Bitrate = GetBitratePerChannel(Audio2Codec)
                    })
                }, allOutputs: true);
            }
        }
    }

    /// <summary>
    /// Gets the quality a specified codec
    /// </summary>
    /// <param name="codec">the codec</param>
    /// <param name="quality">the quality index</param>
    /// <returns>the quality int for the codec</returns>
    private int GetQuality(string codec, int quality)
        => quality switch
        {
            -2 => 28,
            -1 => 25,
            1 => 20,
            2 => 18,
            _ => 22
        };

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
    private readonly List<Validator> RequiredValidator = [new Required()];
    
    
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
