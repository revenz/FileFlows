using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New Audio Flow wizard
/// </summary>
public partial class NewAudioFlowWizard
{
    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguages = [], AudioMode1Languages = [];
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] public FrontendService feService { get; set; }

    /// <summary>
    /// Flow properties
    /// </summary>
    private string Codec = "aac";
    private int Bitrate = 192;
    private bool Normalize;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> AudioCodecs = [], AudioBitrates = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }
    /// <summary>
    /// If the user is adding a file drop flow
    /// </summary>
    private bool FileDropFlow;

    // if the initialization has been done
    private bool initDone;
    private bool IsWindows;
    
    /// <summary>
    /// Gets or sets bound codec
    /// </summary>
    private object BoundCodec
    {
        get => Codec;
        set
        {
            if (value is string codec)
                Codec = codec;
        }
    }
    /// <summary>
    /// Gets or sets bound bitrate
    /// </summary>
    private object BoundBitrate
    {
        get => Bitrate;
        set
        {
            if (value is int bitrate)
                Bitrate = bitrate;
        }
    }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Options is NewAudioFlowWizardOptions options)
            FileDropFlow = options.FileDropFlow;
        
        var profile = feService.Profile.Profile; 
        
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;

        AudioCodecs =
        [
            new() { Label = "AAC", Value = "aac" },
            new() { Label = "MP3", Value = "MP3" },
            new () { Label = "OGG (Vorbis)", Value = "ogg"},
            new () { Label = "OGG (Opus)", Value = "libopus"},
            new() { Label = "WAV", Value = "wav" }
        ];

        AudioBitrates = Enumerable.Range(0, 9).Select(x => new ListOption()
        {
            Label = (x * 32 + 64) + " KBps",
            Value = (x * 32 + 64)
        }).ToList();

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
            builder.Add(new FlowPart()
            {
                FlowElementUid = FlowElementUids.AudioFile,
                Outputs = 1
            });

            FlowAudio(builder);
            Output.FlowAddOutput(builder, ["*.mp3", "*.ogg", "*.flac", "*.m4a", "*.wav", "*.ac3", "*.wma"]);
            
            var flow = builder.Flow;
            flow.Description = Description;
            flow.Icon = "fas fa-headphones";
            
            if (FileDropFlow)
            {
                flow.Type = FlowType.FileDrop;
                flow.FileDropOptions ??= new();
                flow.FileDropOptions.Extensions = Extensions_Audio;
            }
            
            var saveResult = await HttpHelper.Put<Flow>("/api/flow?uniqueName=true", flow);
            if (saveResult.Success == false)
            {
                Wizard.HideBlocker();
                feService.Notifications.ShowError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
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
            feService.Notifications.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
            return false;
        }

        return Output.Validate();
    }
    
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
    /// Adds the audio flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowAudio(FlowBuilder builder)
    {
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = FlowElementUids.Audio_ConvertAudio,
            Outputs = 2,
            Type = FlowElementType.Process,
            Model = ExpandoHelper.ToExpandoObject(new
            {
                Codec,
                SampleRate = 0,
                Channels = 0,
                Bitrate,
                Normalize
            })
        });
    }
}

/// <summary>
/// The New Audio Flow Wizard Options
/// </summary>
public class NewAudioFlowWizardOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets if the user is adding a file drop flow
    /// </summary>
    public bool FileDropFlow { get; set; }
}
