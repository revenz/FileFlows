using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using FileFlows.Plugin.Types;
using Microsoft.AspNetCore.Components;
using Polly;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New Comic Flow wizard
/// </summary>
public partial class NewComicFlowWizard 
{
    private List<string> Image1Languages = [], Image2Languages = [], SubtitleLanguages = [], ImageMode1Languages = [];
    
    /// <summary>
    /// Flow properties
    /// </summary>
    private string Format = "";
    private int Quality = 70;
    private bool Cbz = true, EnsureTopDirectory, DeleteNonPageImages;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> ImageFormats = [];
    
    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    // if the initialization has been done
    private bool initDone;
    /// <summary>
    /// If the user is adding a file drop flow
    /// </summary>
    private bool FileDropFlow;
    
    /// <summary>
    /// Gets or sets bound Format
    /// </summary>
    private object BoundFormat
    {
        get => Format;
        set
        {
            if (value is string codec)
                Format = codec;
        }
    }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Options is NewComicFlowWizardOptions options)
            FileDropFlow = options.FileDropFlow;
        ImageFormats =
        [
            new () { Value = "", Label = Translater.Instant("Dialogs.NewComicFlowWizard.Labels.SameAsSource") },
            new () { Value = "WebP", Label = "WebP" },
            new () { Value = "JPEG", Label = "JPEG" },
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
                FlowElementUid = FlowElementUids.ImageFile,
                Outputs = 1
            });

            FlowFormat(builder);
            Output.FlowAddOutput(builder,
                new[] { "*.jpeg", "*.jpe", "*.jpg", "*.png", "*.bmp", "*.gif", "*.heic", "*.tiff", "*.psd" }
            );
            
            var flow = builder.Flow;
            flow.Description = Description;
            flow.Icon = "fas fa-journal-whills";
            if (FileDropFlow)
                flow.Type = FlowType.FileDrop;
            
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
            Toast.ShowError("Dialogs.NewVideoFlowWizard.Messages.NameRequired");
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
    /// Adds the image flow parts to the flow
    /// </summary>
    /// <param name="builder">the flow builder</param>
    private void FlowFormat(FlowBuilder builder)
    {
        builder.AddAndConnect(new FlowPart()
        {
            FlowElementUid = FlowElementUids.ComicConverter,
            Outputs = 2,
            Type = FlowElementType.Process,
            Model = ExpandoHelper.ToExpandoObject(new
            {
                EnsureTopDirectory,
                DeleteNonPageImages,
                Format = Cbz ? "CBZ" : "PDF",
                Quality = Cbz && string.IsNullOrWhiteSpace(Format) == false ? Quality : 100,
                Codec = Cbz ? Format ?? string.Empty : string.Empty
            })
        });
    }
}

/// <summary>
/// The New Image Flow Wizard Options
/// </summary>
public class NewComicFlowWizardOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets if the user is adding a file drop flow
    /// </summary>
    public bool FileDropFlow { get; set; }
}
