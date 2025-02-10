using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using FileFlows.Plugin.Types;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New Image Flow wizard
/// </summary>
public partial class NewImageFlowWizard 
{
    
    internal const string IMAGE_FORMAT_BMP = "Bmp";
    internal const string IMAGE_FORMAT_GIF = "Gif";
    internal const string IMAGE_FORMAT_JPEG = "Jpeg";
    internal const string IMAGE_FORMAT_PBM = "Pbm";
    internal const string IMAGE_FORMAT_PNG = "Png";
    internal const string IMAGE_FORMAT_TIFF = "Tiff";
    internal const string IMAGE_FORMAT_TGA = "Tga";
    internal const string IMAGE_FORMAT_WEBP = "WebP";
    internal const string IMAGE_FORMAT_HEIC = "Heic";

    private List<string> Image1Languages = [], Image2Languages = [], SubtitleLanguages = [], ImageMode1Languages = [];

    /// <summary>
    /// Flow properties
    /// </summary>
    private string Format = IMAGE_FORMAT_JPEG;
    private int Quality = 70, ResizeMode = 1;
    private bool Resize;
    public NumberPercent Width = new() { Value = 1920 }, Height = new() { Value = 1080 };

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
    /// If the user is adding a reseller flow
    /// </summary>
    private bool ResellerFlow;
    
    /// <summary>
    /// Gets or sets bound Format
    /// </summary>
    private object BoundFormat
    {
        get => Format;
        set
        {
            if (value is string v)
                Format = v;
        }
    }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Options is NewImageFlowWizardOptions options)
            ResellerFlow = options.ResellerFlow;
        
        ImageFormats =
        [
            new () { Value = "###GROUP###", Label = Translater.Instant("Dialogs.NewImageFlowWizard.Labels.LosslessFormats") },
            new () { Value = IMAGE_FORMAT_PNG, Label = "PNG" },
            new () { Value = IMAGE_FORMAT_BMP, Label = "Bitmap" },
            new () { Value = IMAGE_FORMAT_TIFF, Label = "TIFF" },
            new () { Value = IMAGE_FORMAT_TGA, Label = "TGA" },
            new () { Value = IMAGE_FORMAT_WEBP, Label = "WebP" },
            new () { Value = "###GROUP###", Label = Translater.Instant("Dialogs.NewImageFlowWizard.Labels.LossyFormats") },
            new () { Value = IMAGE_FORMAT_JPEG, Label = "JPEG" },
            new () { Value = IMAGE_FORMAT_GIF, Label = "GIF" },
            new () { Value = IMAGE_FORMAT_HEIC, Label = "HEIC" },
            new () { Value = IMAGE_FORMAT_PBM, Label = "PBM" },
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

            FlowImage(builder);
            Output.FlowAddOutput(builder,
                ["*.jpeg", "*.jpe", "*.jpg", "*.png", "*.bmp", "*.gif", "*.heic", "*.tiff", "*.psd"]
            );

            var flow = builder.Flow;
            flow.Description = Description;
            flow.Icon = "fas fa-image";
            if (ResellerFlow)
                flow.Type = FlowType.Reseller;
            
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
    private void FlowImage(FlowBuilder builder)
    {
        if (Resize)
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.ImageResizer,
                Outputs = 1,
                Type = FlowElementType.Process,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Format,
                    Quality = Format is IMAGE_FORMAT_WEBP or IMAGE_FORMAT_JPEG ? Quality : 100,
                    Mode = ResizeMode,
                    Width,
                    Height
                })
            });
        }
        else
        {
            builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.ImageConvert,
                Outputs = 2,
                Type = FlowElementType.Process,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Format,
                    Quality = Format is IMAGE_FORMAT_WEBP or IMAGE_FORMAT_JPEG ? Quality : 100
                })
            });
        }
    }
}

/// <summary>
/// The New Image Flow Wizard Options
/// </summary>
public class NewImageFlowWizardOptions : IModalOptions
{
    
    /// <summary>
    /// Gets or sets if the user is adding a reseller flow
    /// </summary>
    public bool ResellerFlow { get; set; }
}
