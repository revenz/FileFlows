using System.Reflection.Emit;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New videoFlow wizard
/// </summary>
public partial class NewVideoFlowWizard 
{
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblUseOriginal, lblCopyOnlyLanguages, lblOnlyOnePerLanguage;

    /// <summary>
    /// Gets the selected encoding type
    /// </summary>
    private int SelectedVideoEncodingType;
    private int Quality = 6, Bitrate = 5000;
    private bool CropBlackBars, AttemptHardwareEncode = true;
    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguages = [], AudioMode1Languages = [];
    /// <summary>
    /// If the user is adding a file drop flow
    /// </summary>
    private bool FileDropFlow;
    

    /// <summary>
    /// Flow properties
    /// </summary>
    private string VideoCodec = "h265", VideoContainer = "MKV", Audio1Codec = "aac", Audio2Codec = "aac", DefaultLanguage = "eng";
    private int _SelectedType, Audio1Channels, Audio2Channels, AudioMode;
    private bool TwoAudioVersions, FallBackAudio, SubtitleKeepOnly;

    /// <summary>
    /// Gets or sets the selected type
    /// </summary>
    private int SelectedType
    {
        get => _SelectedType;
        set
        {
            _SelectedType = value;
            if (value == 1)
                Quality = 8;
            if (value == 2)
                Quality = 6;
        }
    }

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

        if (Options is NewVideoFlowWizardOptions options)
            FileDropFlow = options.FileDropFlow;

        if (profile.UseGerman)
        {
            Audio1Languages = ["eng", "deu", "orig"];
            SubtitleLanguages = ["eng", "deu", "orig"];
            AudioMode1Languages = ["eng", "deu", "orig"];
        }
        else if (profile.UseFrench)
        {
            Audio1Languages = ["eng", "fre", "orig"];
            SubtitleLanguages = ["eng", "fre", "orig"];
            AudioMode1Languages = ["eng", "fre", "orig"];
        }
        else
        {
            Audio1Languages = ["eng", "orig"];
            SubtitleLanguages = ["eng", "orig"];
            AudioMode1Languages = ["eng", "orig"];
        }

        lblUseOriginal = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.UseOriginal");
        lblOnlyOnePerLanguage = Translater.Instant("Dialogs.NewVideoFlowWizard.Labels.OnlyOnePerLanguage");
        
        lblCopyOnlyLanguages = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.CopyOnlyLanguages");
        
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
        LanguageOptions.Insert(0, new ListOption() { 
            Label = Translater.Instant("Labels.OriginalLanguage"), 
            Value = "OriginalLanguage" 
        });
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
            //new () { Label = lblUseOriginal, Value = ""},
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
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Ok"), Value = 2},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Good"), Value = 4},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.Recommended"), Value = 6},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.High"), Value = 8},
            new () { Label = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.QualityLevel.VeryHigh"), Value = 10},
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
            var builder = new FlowBuilder(FlowName);
            builder.MaxRows = 7;
            builder.Add(new FlowPart()
            {
                FlowElementUid = FlowElementUids.VideoFile,
                Outputs = 1
            });
            FlowAddMetaLookup(builder);
            
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuilderStart,
                Outputs = 1,
                Type = FlowElementType.BuildStart
            }, allOutputsIncludingFailure: true);

            FlowAddRemux(builder);

            bool videoEncode = string.IsNullOrWhiteSpace(VideoCodec) == false;

            if (videoEncode)
                FlowAddVideo(builder);
            
            FlowAddSubtitles(builder);
            
            // audio must go last since it can require 2 flow parts being connected up
            var partsToConnect = FlowAddAudio(builder);

            var executor = new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuilderExecutor,
                Outputs = 2,
                Type = FlowElementType.BuildEnd,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    HardwareDecoding = "auto",
                    Strictness = "experimental"
                })
            };
            int rowBase = 0;
            if(partsToConnect.Length < 2)
                builder.AddAndConnect(executor, allOutputs: true);
            else
            {
                builder.Add(executor);
                foreach (var partToConnect in partsToConnect)
                    builder.Connect(partToConnect, executor, 1);
                builder.CurrentRow += 1;
                rowBase = 1;
            }

            var preOutputColumn = builder.CurrentColumn;
            FlowPart fpOutput = Output.FlowAddOutput(builder,
                ["mkv", "mp4", "avi", "divx", "mov", "mpeg", "mpe"],
                videoEncode && AttemptHardwareEncode ? 5 + rowBase : 0, videoEncode && AttemptHardwareEncode ? 4 : 0);

            if (videoEncode && AttemptHardwareEncode)
            {
                // is using encoding, so catch an error turn off hardware and try again
                builder.CurrentColumn = preOutputColumn + 4;
                builder.CurrentRow = 0;
                var secondEncode = builder.Add(
                    SelectedVideoEncodingType == 1
                        ? new FlowPart()
                        {
                            // bitrate encode
                            FlowElementUid = FlowElementUids.FFmpegBuilderVideoBitrateEncode,
                            Outputs = 1,
                            Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.CpuFailOverEncode"),
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
                            FlowElementUid = FlowElementUids.FFmpegBuilderVideoEncodeSimple,
                            Outputs = 1,
                            Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.CpuFailOverEncode"),
                            Type = FlowElementType.BuildPart,
                            Model = ExpandoHelper.ToExpandoObject(new
                            {
                                Codec = VideoCodec,
                                Encoder= "CPU",
                                Quality,
                                Speed = 3
                            })
                        }, row: 1);
                builder.Connect(executor, secondEncode, -1);
                
                var secondExecutor = builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.FFmpegBuilderExecutor,
                    Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.CpuFailOverExecutor"),
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

            var flow = builder.Flow;
            flow.Description = Description;
            flow.Icon = SelectedType switch
            {
                1 => "fas fa-film",
                2 => "fas fa-tv",
                _ => "fas fa-video"
            };
            if (FileDropFlow)
            {
                flow.Type = FlowType.FileDrop;
                flow.FileDropOptions ??= new();
                flow.FileDropOptions.PreviewMode = FileDropPreviewMode.Thumbnails;
                flow.FileDropOptions.Extensions = Extensions_Video;

            }
            
            var saveResult = await HttpHelper.Put<Flow>("/api/flow?uniqueName=true", flow);
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
    /// Validates the flow before saving
    /// </summary>
    /// <returns>true if flow is valid, otherwise false</returns>
    private async Task<bool> ValidateFlow()
    {
        await Editor.Validate();
        if (string.IsNullOrWhiteSpace(FlowName))
        {
            Toast.ShowError("Dialogs.NewFlowCommon.Messages.NameRequired");
            return false;
        }
        
        if(Output.Validate() == false)
            return false;

        if (AudioMode == 1)
        {
            if (AudioMode1Languages.Count == 0)
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
    /// Adds the remux flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddRemux(FlowBuilder builder)
    {
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = VideoContainer == "MP4" ? FlowElementUids.FFmpegBuilderRemuxToMp4 : FlowElementUids.FFmpegBuilderRemuxToMkv,
            Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.RemuxToContainer", new { container = VideoContainer}),
            Outputs = 1,
            Type = FlowElementType.BuildPart
        });
        if (VideoContainer != "MP4") 
            return;
        
        // need to remove subtitles that arent supported in MP4
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = FlowElementUids.FFmpegBuilderSubtitleFormatRemover,
            Outputs = 2,
            Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.RemoveUnsupportedSubtitles"),
            Type = FlowElementType.BuildPart,
            Model = ExpandoHelper.ToExpandoObject(new
            {
                SubtitlesToRemove = new List<string>
                {
                    "xsub",           // DivX subtitles (XSUB)
                    "dvbsub",         // DVB subtitles (codec dvb_subtitle)
                    "dvdsub",         // DVD subtitles (codec dvd_subtitle)
                    "dvb_teletext",   // DVB/Teletext Format
                    "hdmv_pgs_subtitle", // Presentation Graphic Stream (PGS)
                    "ttml",           // TTML subtitle
                    "OTHER"           // Unknown/Other
                }
            })
        });
            
        // need to remove attachments
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = FlowElementUids.FFmpegBuilderRemoveAttachments,
            Outputs = 1,
            Type = FlowElementType.BuildPart
        }, allOutputs: true);
    }
    
    /// <summary>
    /// Adds the meta lookup flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAddMetaLookup(FlowBuilder builder)
    {
        switch (SelectedType)
        {
            case 1: // film
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.MovieLookup,
                    Outputs = 2,
                    Type = FlowElementType.Logic,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        UseFolderName = true
                    })
                });
                break;
            case 2: // tv
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.TVShowLookup,
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
                FlowElementUid = FlowElementUids.FFmpegBuilderVideoBitrateEncode,
                Outputs = 1,
                Name = codecLabel + " (Bitrate)",
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Codec = VideoCodec,
                    Encoder = AttemptHardwareEncode ? "" : "CPU",
                    Bitrate
                })
            });
        }
        else
        {
            // quality encode
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuilderVideoEncodeSimple,
                Outputs = 1,
                Name = codecLabel,
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Codec = VideoCodec,
                    Encoder = AttemptHardwareEncode ? "" : "CPU",
                    Quality = Quality,
                    Speed = 3
                })
            });
        }

        if (CropBlackBars)
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuildeCropBlackBars,
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
                FlowElementUid = FlowElementUids.FFmpegBuilderLanguageRemover,
                Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.RemoveUnwantedSubtitles"),
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
    /// <returns>the flow parts to connect the next flow part to</returns>
    private FlowPart[] FlowAddAudio(FlowBuilder builder)
    {
        if (AudioMode > 1)
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuilderAudioSetLanguage,
                Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.DefaultLanguage", new {lang = DefaultLanguage}),
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
                FlowElementUid = FlowElementUids.FFmpegBuilderLanguageRemover,
                Outputs = 2,
                Type = FlowElementType.BuildPart,
                Name = lblCopyOnlyLanguages,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    StreamType = "Audio",
                    Languages = AudioMode1Languages,
                    NotMatching = true
                })
            }, allOutputs: true);
        }
        else if (AudioMode == 2)
        {
            // convert audio
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.FFmpegBuilderAudioLanguageConverter,
                Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.PrimaryAudio"),
                Outputs = 2,
                Type = FlowElementType.BuildPart,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Languages = Audio1Languages,
                    RemoveOthers = true,
                    Codec = Audio1Codec,
                    Channels = Audio1Channels,
                    Bitrate = GetBitratePerChannel(Audio1Codec)
                })
            }, allOutputs: true);

            if (TwoAudioVersions)
            {
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.FFmpegBuilderAudioLanguageConverter,
                    Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Parts.SecondaryAudio"),
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
            else if (FallBackAudio)
            {
                int currentColumn = builder.CurrentColumn;
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.FFmpegBuilderAudioLanguageConverter,
                    Name = Translater.Instant("Dialogs.NewVideoFlowWizard.Fields.FallBackAudio"),
                    Outputs = 2,
                    Type = FlowElementType.BuildPart,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        Languages = Audio1Languages,
                        Codec = Audio1Codec,
                        Channels = 0,
                        Bitrate = GetBitratePerChannel(Audio1Codec)
                    })
                }, column:currentColumn + 1, output: 2);
                var parts = builder.Flow.Parts[^2..].ToArray();
                builder.AddAndConnect(new FlowPart()
                {
                    FlowElementUid = FlowElementUids.FailFlow,
                    Outputs = 0,
                    Type = FlowElementType.Logic,
                    CustomColor = "var(--error)",
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        Reason = "Failed to find any wanted audio languages."
                    })
                }, column: currentColumn + 2, output: 2);
                
                builder.CurrentColumn = currentColumn;
                return parts;
            }
        }
        return [builder.Flow.Parts.Last()];
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
        bool valid = string.IsNullOrWhiteSpace(FlowName) == false;
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
    /// <summary>
    /// Gets or sets if the user is adding a file drop flow
    /// </summary>
    public bool FileDropFlow { get; set; }
}
