using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New Audio to Video Flow wizard
/// </summary>
public partial class NewAudioToVideoWizard 
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguages = [], AudioMode1Languages = [];
    
    /// <summary>
    /// Flow properties
    /// </summary>
    private string Codec = "h265", Container = "MKV", Resolution = "640x480";
    private int Visualization = 1;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> VideoCodecs = [], VideoResolutions = [], VideoContainers = [], Visualizations = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    // if the initialization has been done
    private bool initDone;
    
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
    /// Gets or sets bound container
    /// </summary>
    private object BoundContainer
    {
        get => Container;
        set
        {
            if (value is string v)
                Container = v;
        }
    }
    /// <summary>
    /// Gets or sets bound Resolution
    /// </summary>
    private object BoundResolution
    {
        get => Resolution;
        set
        {
            if (value is string v)
                Resolution = v;
        }
    }
    /// <summary>
    /// Gets or sets bound Visualization
    /// </summary>
    private object BoundVisualization
    {
        get => Visualization;
        set
        {
            if (value is int v)
                Visualization = v;
        }
    }
    /// <summary>
    /// If the user is adding a file drop flow
    /// </summary>
    private bool FileDropFlow;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Options is NewImageFlowWizardOptions options)
            FileDropFlow = options.FileDropFlow;
        VideoCodecs =
        [
            new() { Label = "H264", Value = "h264" },
            new() { Label = "HEVC", Value = "h265" },
        ];
        
        VideoContainers =
        [
            new() { Label = "MKV", Value = "mkv" },
            new() { Label = "MP4", Value = "mp4" }
        ];

        Visualizations =
        [
            new () { Label = "Waves", Value = 1} ,
            new () { Label = "Audio Vector Scope", Value = 2} ,
            new () { Label = "Spectrum", Value = 3} ,
        ];
        VideoResolutions =
        [
            new () { Label = "480P", Value = "640x480"} ,
            new () { Label = "720P", Value = "1280x720"} ,
            new () { Label = "1080P", Value = "1920x1080"} ,
            new () { Label = "4K", Value = "3840x2160"} ,
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
            builder.Add(new FlowPart()
            {
                FlowElementUid = FlowElementUids.InputFile,
                Outputs = 1
            });

            FlowVideo(builder);
            Output.FlowAddOutput(builder,
                ["*.mp3", "*.ogg", "*.flac", "*.m4a", "*.wav", "*.ac3", "*.wma"]);
            
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
    private void FlowVideo(FlowBuilder builder)
    {
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = FlowElementUids.AudioToVideo,
            Outputs = 2,
            Type = FlowElementType.Process,
            Model = ExpandoHelper.ToExpandoObject(new
            {
                Visualization,
                Container,
                Resolution,
                Codec
            })
        });
    }
}

/// <summary>
/// The New Audio to Video Flow Wizard Options
/// </summary>
public class NewAudioToVideoWizardOptions : IModalOptions
{
    
    /// <summary>
    /// Gets or sets if the user is adding a file drop flow
    /// </summary>
    public bool FileDropFlow { get; set; }
}
