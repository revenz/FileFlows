using FileFlows.Client.Components;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// Wizard Output
/// </summary>
public partial class WizardOutput : ComponentBase
{
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }
    /// <summary>
    /// Gets or sets if this is a file drop flow
    /// </summary>
    [Parameter] public bool FileDropFlow { get; set; }
    
    private string OutputPath;
    private bool DeleteOld, IsWindows, IsFileDrop;
    private int OutputMode = 0;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get();
        IsWindows = profile.ServerOS == OperatingSystemType.Windows;
        IsFileDrop = profile.LicensedFor(LicenseFlags.FileDrop);
        if (IsFileDrop && FileDropFlow)
            OutputMode = 2;
    }

    /// <summary>
    /// Required validator
    /// </summary>
    private readonly List<Validator> RequiredValidator = [new Required()];
    
    /// <summary>
    /// Validates the output before saving
    /// </summary>
    /// <returns>true if the output is valid, otherwise false</returns>
    public bool Validate()
    {
        if (OutputMode == 1 && string.IsNullOrWhiteSpace(OutputPath))
        {
            Toast.ShowError("Dialogs.NewFlowCommon.Messages.OutputPathRequired");
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Adds the output flow elements
    /// </summary>
    /// <param name="builder">the flow builder</param>
    /// <param name="keepExtensions">the extensions to keep if deleting old</param>
    /// <param name="row">Optional row to start at</param>
    /// <param name="col">Optional col to start at</param>
    /// <returns>the primary flow output flow part</returns>
    public FlowPart  FlowAddOutput(FlowBuilder builder, string[] keepExtensions
    , int row = 0, int col = 0)
    {
        int preOutputColumn = builder.CurrentColumn;
        FlowPart fpOutput;
        if (OutputMode == 0)
        {
            // replace original
            fpOutput = builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.ReplaceOriginal,
                Outputs = 1,
                Type = FlowElementType.Process
            }, row: row, column: col > 0 ? (preOutputColumn + col) : 0);
        }
        else if (OutputMode == 2)
        {
            // file drop
            fpOutput = builder.AddAndConnect(new FlowPart()
            {
                FlowElementUid = FlowElementUids.MoveToUserFolder,
                Outputs = 1,
                Type = FlowElementType.Process
            }, row: row, column: col > 0 ? (preOutputColumn + col) : 0);
        }
        else
        {
            // save to
            fpOutput = builder.AddAndConnect(new FlowPart()
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
            }, row: row, column: col > 0 ? (preOutputColumn + col) : 0);

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
                        IncludePatterns = keepExtensions?.Select(x => x.StartsWith('*') ? x : "*." + x)?.ToArray()
                    })
                }, row: row> 0 ? row + 1 : 0, column: col > 0 ? (preOutputColumn + col - 1) : 0);
            }
        }

        return fpOutput;
    }
}