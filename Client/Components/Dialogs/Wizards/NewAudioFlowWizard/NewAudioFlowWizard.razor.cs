using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs.Wizards;

/// <summary>
/// New Audio Flow wizard
/// </summary>
public partial class NewAudioFlowWizard : IModal
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
    private string Codec = "aac", OutputPath;
    private int Bitrate = 192;
    private bool ReplaceOriginal = true, DeleteOld, Normalize;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> AudioCodecs = [], AudioBitrates = [];
    
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
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get(); 
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;

        AudioCodecs =
        [
            new() { Label = "AAC", Value = "aac" },
            new() { Label = "MP3", Value = "mp3" },
            new() { Label = "OGG", Value = "ogg" },
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
            builder.MaxRows = 7;
            builder.Add(new FlowPart()
            {
                FlowElementUid = FlowElementUids.AudioFile,
                Outputs = 1
            });

            
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
}

/// <summary>
/// The New Audio Flow Wizard Options
/// </summary>
public class NewAudioFlowWizardOptions : IModalOptions
{
}
