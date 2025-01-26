using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using FileFlows.Shared.Widgets;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs.Wizards;

/// <summary>
/// New Audio to Video Flow wizard
/// </summary>
public partial class NewAudioToVideoWizard : IModal
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

    private List<string> Audio1Languages = [], Audio2Languages = [], SubtitleLanguages = [], AudioMode1Languages = [];
    
    /// <summary>
    /// The new flow name
    /// </summary>
    private string FlowName { get; set; } = string.Empty;

    /// <summary>
    /// Flow properties
    /// </summary>
    private string Codec = "h265", Container = "MKV", Resolution = "640x480", OutputPath;
    private int Visualization = 1;
    private bool ReplaceOriginal = true, DeleteOld;

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
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get(); 
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;

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
            FlowAddOutput(builder);
            
            var saveResult = await HttpHelper.Put<Flow>("/api/flow?uniqueName=true", builder.Flow);
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
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
            return false;
        }

        if (ReplaceOriginal == false && string.IsNullOrWhiteSpace(OutputPath))
        {
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.OutputPathRequired");
            return false;
        }
        return true;
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
    
    /// <summary>
    /// Adds the output flow elements
    /// </summary>
    /// <param name="builder">the flow builder</param>
    /// <returns>the primary flow output flow part</returns>
    private void FlowAddOutput(FlowBuilder builder)
    {
        if (ReplaceOriginal)
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.ReplaceOriginal,
                Outputs = 1,
                Type = FlowElementType.Process
            });
        }
        else
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.MoveFile,
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
                    FlowElementUid = FlowElementUids.DeleteSourceDirectory,
                    Outputs = 2,
                    Type = FlowElementType.Process,
                    Model = ExpandoHelper.ToExpandoObject(new
                    {
                        IfEmpty = true,
                        IncludePatterns = new[] { "*.mp3", "*.ogg", "*.flac", "*.m4a", "*.wav", "*.ac3", "*.wma" }
                    })
                });
            }
        }
    }
}

/// <summary>
/// The New Audio to Video Flow Wizard Options
/// </summary>
public class NewAudioToVideoWizardOptions : IModalOptions
{
}
