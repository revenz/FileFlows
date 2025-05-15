using FileFlows.Plugin;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Node editor
/// </summary>
public partial class NodeEditor : ModalEditor
{

    /// <summary>
    /// Gets or sets the model being edited
    /// </summary>
    public ProcessingNode Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/nodes";
    
    /// <summary>
    /// Gets if this is an external node
    /// </summary>
    public bool IsExternalNode { get; set; }
    
    /// <summary>
    /// Gets or sets if this is a Windows node
    /// </summary>
    public bool WindowsNode { get; set; }

    /// <summary>
    /// Translations
    /// </summary>
    private string lblScheduleDescription, lblMappingsDescription, lblProcessing, lblVariables;

    private int PermissionsFiles, PermissionsFolders;
    
    private List<ListOption> ScriptOptions = [], AllLibrariesOptions = [], ProcessingOrderOptions = [],LibraryOptions = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();;
        Title = Translater.Instant("Pages.ProcessingNode.Title");
        lblScheduleDescription = Translater.Instant("Pages.ProcessingNode.Fields.ScheduleDescription");
        lblMappingsDescription = Translater.Instant("Pages.ProcessingNode.Fields.MappingsDescription");
        lblProcessing = Translater.Instant("Pages.ProcessingNode.Fields.ProcessingDescription");
        lblVariables = Translater.Instant("Pages.ProcessingNode.Fields.VariablesDescription");
        ScriptOptions = feService.Script.Scripts.Where(x => x.Type == ScriptType.System)
            .OrderBy(x => x.Name.ToLowerInvariant())
            .Select(x => new ListOption
            {
                Value = x.Uid, Label = x.Name
            }).ToList();
        ScriptOptions.Insert(0, new ListOption() { Label = "Labels.None", Value = Guid.Empty});

        AllLibrariesOptions =
        [
            new() { Label = Translater.Instant("Enums.ProcessingLibraries.All"), Value = ProcessingLibraries.All },
            new() { Label = Translater.Instant("Enums.ProcessingLibraries.Only"), Value = ProcessingLibraries.Only },
            new()
            {
                Label = Translater.Instant("Enums.ProcessingLibraries.AllExcept"), Value = ProcessingLibraries.AllExcept
            }
        ];
        
        LibraryOptions = feService.Library.Libraries
            .OrderBy(x => x.Name.ToLowerInvariant()
            ).Select(x => new ListOption
            {
                Label = x.Name,
                Value = new ObjectReference
                {
                    Uid = x.Uid,
                    Name = x.Name,
                    Type = typeof(Library)?.FullName ?? string.Empty
                }
            }).OrderBy(x => x.Label).ToList();
        
        ProcessingOrderOptions = [
            new () { Value = (ProcessingOrder)1000, Label = $"Labels.Default" },
            new () { Value = ProcessingOrder.AsFound, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.AsFound)}" },
            new () { Value = ProcessingOrder.Alphabetical, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Alphabetical)}" },
            new () { Value = ProcessingOrder.SmallestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.SmallestFirst)}" },
            new () { Value = ProcessingOrder.LargestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.LargestFirst)}" },
            new () { Value = ProcessingOrder.NewestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.NewestFirst)}" },
            new () { Value = ProcessingOrder.OldestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.OldestFirst)}" },
            new () { Value = ProcessingOrder.Random, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Random)}" },
        ];
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        var uid = GetModelUid();

        var result = await HttpHelper.Get<ProcessingNode>("/api/node/" + uid);
        if (result.Success == false || result.Data == null)
        {
            Container.HideBlocker();
            Close();
        }

        InitializeModel(result.Data);
    }


    /// <summary>
    /// Opens the node editor to edit a specific node
    /// </summary>
    /// <param name="model">the node to edit</param>
    /// <returns>true if the node was saved, otherwise false</returns>
    public void InitializeModel(ProcessingNode model)
    {
        IsExternalNode = model.Uid != CommonVariables.InternalNodeUid;
        WindowsNode = model.OperatingSystem == OperatingSystemType.Windows;
        if(IsExternalNode == false)
            Title = Translater.Instant("Labels.InternalProcessingNode");
        Model = model;
        BoundLibraries = model.Libraries?.Cast<object>()?.ToList() ?? [];
        PermissionsFolders = model.PermissionsFolders ?? 0;
        PermissionsFiles = model.PermissionsFiles ?? 0;
    }
    
    /// <summary>
    /// Saves the node
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            Model.Libraries = BoundLibraries.Cast<ObjectReference>().ToList();
            Model.PermissionsFiles = PermissionsFiles == 0 ? null : PermissionsFiles;
            Model.PermissionsFolders = PermissionsFolders == 0 ? null : PermissionsFolders;
            var saveResult = await HttpHelper.Post<Node>($"/api/node", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for node to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
    /// <summary>
    /// Gets or sets the bound BoundPreExecuteScript
    /// </summary>
    private object BoundPreExecuteScript
    {
        get => Model.PreExecuteScript;
        set
        {
            if (value is Guid v)
                Model.PreExecuteScript = v;
        }
    }
    /// <summary>
    /// Gets or sets the bound BoundAllLibraries
    /// </summary>
    private object BoundAllLibraries
    {
        get => Model.AllLibraries;
        set
        {
            if (value is ProcessingLibraries v)
                Model.AllLibraries = v;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound BoundAllLibraries
    /// </summary>
    private object BoundProcessingOrder
    {
        get => Model.ProcessingOrder;
        set
        {
            if (value is ProcessingOrder v)
                Model.ProcessingOrder = v;
            else
                Model.ProcessingOrder = null;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound BoundLibraries
    /// </summary>
    private List<object> BoundLibraries { get; set; }
}