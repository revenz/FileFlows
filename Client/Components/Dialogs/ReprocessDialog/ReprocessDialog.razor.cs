using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog used to reprocess files
/// </summary>
public partial class ReprocessDialog : IModal
{
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    
    /// <summary>
    /// Gets or sets if this is in the process options mode
    /// </summary>
    public bool ProcessOptionsMode { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the modal instance
    /// </summary>
    private Modal Modal { get; set; }
    
    /// <summary>
    /// Gets or sets the flow to run against
    /// </summary>
    private Guid FlowUid { get; set; }
    /// <summary>
    /// Gets or sets the flows in the system
    /// </summary>
    private List<KeyValuePair<Guid, string>> Flows { get; set; }
    /// <summary>
    /// Gets or sets the files to reprocess
    /// </summary>
    private List<LibraryFile> Files { get; set; }
    /// <summary>
    /// Gets or sets the Node to run against
    /// </summary>
    private Guid NodeUid { get; set; }
    /// <summary>
    /// Gets or sets the nodes in the system
    /// </summary>
    private List<KeyValuePair<Guid, string>> Nodes { get; set; }
    /// <summary>
    /// The strings for translations
    /// </summary>
    private string lblTitle, lblDescription,
        lblReprocessTitle, lblReprocessDescription,
        lblProcessOptionsTitle, lblProcessOptionsDescription, 
        lblFlow, lblNode, lblPosition, lblCustomVariablesMode, lblTopOfQueue, 
        lblBottomOfQueue, lblAnyNode, lblSameFlow, lblSave, lblReprocess, lblCancel, lblMerge, lblOriginal, lblReplace;
    /// <summary>
    /// The API URL for library files
    /// </summary>
    public string ApiUrl => "/api/library-file";

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> FlowOptions = new (), NodeOptions = new (), PositionOptions = new (), CustomVariableModeOptions = new ();
    
    /// <summary>
    /// Gets or sets if the files should be reprocessed at the bottom of the queue
    /// </summary>
    private bool BottomOfQueue { get; set; }
    
    /// <summary>
    /// Gets or sets the custom variables mode
    /// </summary>
    private ReprocessModel.CustomVariablesMode VariablesMode { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Options is ReprocessOptions rOptions == false)
        {
            Close();
            return;
        }
        
        lblReprocessTitle = Translater.Instant("Dialogs.ReprocessDialog.Title");
        lblReprocessDescription = Translater.Instant("Dialogs.ReprocessDialog.Description");
        lblProcessOptionsTitle = Translater.Instant("Dialogs.ReprocessDialog.ProcessOptions.Title");
        lblProcessOptionsDescription = Translater.Instant("Dialogs.ReprocessDialog.ProcessOptions.Description");
        lblFlow = Translater.Instant("Dialogs.ReprocessDialog.Fields.Flow");
        lblNode = Translater.Instant("Dialogs.ReprocessDialog.Fields.Node");
        lblAnyNode = Translater.Instant("Dialogs.ReprocessDialog.Fields.AnyNode");
        lblSameFlow = Translater.Instant("Dialogs.ReprocessDialog.Fields.SameFlow");
        lblPosition = Translater.Instant("Dialogs.ReprocessDialog.Fields.Position");
        lblBottomOfQueue = Translater.Instant("Dialogs.ReprocessDialog.Fields.BottomOfQueue");
        lblCustomVariablesMode = Translater.Instant("Dialogs.ReprocessDialog.Fields.CustomVariablesMode");
        lblMerge = Translater.Instant("Dialogs.ReprocessDialog.Fields.Merge");
        lblOriginal = Translater.Instant("Dialogs.ReprocessDialog.Fields.Original");
        lblReplace = Translater.Instant("Dialogs.ReprocessDialog.Fields.Replace");
        lblTopOfQueue = Translater.Instant("Dialogs.ReprocessDialog.Fields.TopOfQueue");
        lblReprocess = Translater.Instant("Labels.Reprocess");
        lblSave = Translater.Instant("Labels.Save");
        lblCancel = Translater.Instant("Labels.Cancel");
        PositionOptions =
        [
            new() { Label = lblTopOfQueue, Value = false },
            new() { Label = lblBottomOfQueue, Value = true },
        ];
        CustomVariableModeOptions =
        [
            new () { Label = lblOriginal, Value = ReprocessModel.CustomVariablesMode.Original },
            new () { Label = lblMerge, Value = ReprocessModel.CustomVariablesMode.Merge },
            new () { Label = lblReplace, Value = ReprocessModel.CustomVariablesMode.Replace },
        ];
        
        FlowOptions = feService.Flow.Flows
            .Where(x => x.Type == FlowType.Standard)
            .OrderBy(x => x.Name.ToLowerInvariant())
            .Select(x => new ListOption { Label = x.Name, Value = x.Uid }).ToList();
        FlowOptions.Insert(0, new() { Label = lblSameFlow, Value = Guid.Empty });
        FlowUid = Guid.Empty;
        VariablesMode = ReprocessModel.CustomVariablesMode.Original;
        BottomOfQueue = false;
        
        NodeOptions = feService.Node.NodeList.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption { Label = x.Value == "FileFlowsServer" ? "Internal Processing Node" : x.Value, Value = x.Key }).ToList();

        NodeOptions.Insert(0, new() { Label = lblAnyNode, Value = Guid.Empty });
        NodeUid = Guid.Empty;
        ProcessOptionsMode = rOptions.ProcessOptionsMode;
        lblDescription = rOptions.ProcessOptionsMode ? lblProcessOptionsDescription : lblReprocessDescription;
        lblTitle = rOptions.ProcessOptionsMode ? lblProcessOptionsTitle : lblReprocessTitle;
        CustomVariables = ObjectHelper.GetCommonCustomVariables(rOptions.Files.Select(file => file.CustomVariables ?? []).ToList())
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).ToList();

        Files = rOptions.Files;
    }
    
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
    /// Language is chosen
    /// </summary>
    private async void Save()
    {
        var dict = new Dictionary<string, object>();
        foreach (var kv in CustomVariables)
        {
            if (dict.Keys.Any(x => x.Equals(kv.Key, StringComparison.InvariantCultureIgnoreCase)))
                continue;
            dict[kv.Key] = ObjectHelper.StringToObject(kv.Value);
        }

        Modal?.Blocker?.Show();
        try
        {
            var result = await HttpHelper.Post(ApiUrl + (ProcessOptionsMode ? "/set-process-options" : "/reprocess"), new ReprocessModel
            {
                Uids = Files.Select(x => x.Uid).ToList(),
                Node = NodeUid == Guid.Empty ? null : new () { Uid = NodeUid },
                Flow = FlowUid == Guid.Empty ? null : new () { Uid = FlowUid },
                BottomOfQueue = BottomOfQueue,
                Mode = VariablesMode,
                CustomVariables = dict
            });
            if (result.Success == false)
            {
                feService.Notifications.ShowError(result.Body);
                return;
            }
        }
        finally
        {
            Modal?.Blocker?.Hide();
        }

        TaskCompletionSource.TrySetResult(true);
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets or sets flow uid
    /// </summary>
    private object BoundFlowUid
    {
        get => FlowUid;
        set
        {
            if (value is Guid uid)
                FlowUid = uid;
        }
    }
    
    /// <summary>
    /// Gets or sets node uid
    /// </summary>
    private object BoundNodeUid
    {
        get => NodeUid;
        set
        {
            if (value is Guid uid)
                NodeUid = uid;
        }
    }

    /// <summary>
    /// Gets or sets the custom variables
    /// </summary>
    private List<KeyValuePair<string, string>> CustomVariables { get; set; } = new();
    
    /// <summary>
    /// Gets or sets if the files should be reprocessed at the bottom of the queue
    /// </summary>
    private object BoundPosition
    {
        get => BottomOfQueue;
        set
        {
            if (value is bool b)
                BottomOfQueue = b;
        }
    }
    /// <summary>
    /// Gets or sets bound variables mode
    /// </summary>
    private object BoundVariablesMode
    {
        get => VariablesMode;
        set
        {
            if (value is ReprocessModel.CustomVariablesMode mode)
                VariablesMode = mode;
        }
    }
}
/// <summary>
/// Options for the reprocess
/// </summary>
public class ReprocessOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the files to reprocessed
    /// </summary>
    public List<LibraryFile> Files { get; set;}

    /// <summary>
    /// Gets or sets if this is in the process options mode for unprocessed files
    /// </summary>
    public bool ProcessOptionsMode { get; set; }
}