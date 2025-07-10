using FileFlows.Plugin;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Library editor
/// </summary>
public partial class LibraryEditor : ModalEditor
{

    /// <summary>
    /// Gets or sets the model being edited
    /// </summary>
    public Library Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/libraries";

    /// <summary>
    /// Translations
    /// </summary>
    private string lblManualLibrary, lblScheduleDescription, lblDetectionDescription;
    
    private List<ListOption> FlowOptions = [], PriorityOptions = [], ProcessingOrderOptions = [],
        MatchOptions = [], MatchDateOptions = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();;
        Title = Translater.Instant("Pages.Library.Title");
        lblManualLibrary = Translater.Instant("Labels.ManualLibrary");
        lblScheduleDescription = Translater.Instant("Pages.Library.Fields.ScheduleDescription");
        lblDetectionDescription = Translater.Instant("Pages.Library.Fields.DetectionDescription");
        
        FlowOptions = feService.Flow.Flows
            .OrderBy(x => x.Name.ToLowerInvariant())
            .Select(x => new ListOption
            {
                Value = new ObjectReference { Name = x.Name, Uid = x.Uid, 
                    Type = typeof(Flow).FullName! }, Label = x.Name
            }).ToList();
        
        PriorityOptions = [
            new () { Value = ProcessingPriority.Lowest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Lowest)}" },
            new () { Value = ProcessingPriority.Low, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Low)}" },
            new () { Value = ProcessingPriority.Normal, Label =$"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Normal)}" },
            new () { Value = ProcessingPriority.High, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.High)}" },
            new () { Value = ProcessingPriority.Highest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Highest)}" }
        ];
        
        ProcessingOrderOptions = [
            new () { Value = ProcessingOrder.AsFound, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.AsFound)}" },
            new () { Value = ProcessingOrder.Alphabetical, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Alphabetical)}" },
            new () { Value = ProcessingOrder.SmallestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.SmallestFirst)}" },
            new () { Value = ProcessingOrder.LargestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.LargestFirst)}" },
            new () { Value = ProcessingOrder.NewestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.NewestFirst)}" },
            new () { Value = ProcessingOrder.OldestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.OldestFirst)}" },
            new () { Value = ProcessingOrder.Random, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Random)}" },
        ];
        
        MatchOptions = [
            new () { Value = MatchRange.Any, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Any)}" },
            new () { Value = MatchRange.GreaterThan, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.GreaterThan)}" },
            new () { Value = MatchRange.LessThan, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.LessThan)}" },
            new () { Value = MatchRange.Between, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Between)}" },
            new () { Value = MatchRange.NotBetween, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.NotBetween)}" }
        ];

        MatchDateOptions = MatchOptions.Concat([
            new () { Value = MatchRange.After, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.After)}" },
            new () { Value = MatchRange.Before, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Before)}" }
        ]).ToList();
        
        StateHasChanged();
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if (Options is ModalEditorOptions meOptions && meOptions.Model is Library library)
        {
            Model = library;
            StateHasChanged();
            return;
        }
        
        var uid = GetModelUid();

        var result = await HttpHelper.Get<Library>("/api/library/" + uid);
        if (result.Success == false || result.Data == null)
        {
            Container.HideBlocker();
            Close();
        }

        Model = result.Data;
        StateHasChanged();
    }
    
    /// <summary>
    /// Saves the library
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            // Model.Libraries = BoundLibraries.Cast<ObjectReference>().ToList();
            // Model.PermissionsFiles = PermissionsFiles == 0 ? null : PermissionsFiles;
            // Model.PermissionsFolders = PermissionsFolders == 0 ? null : PermissionsFolders;
            var saveResult = await HttpHelper.Post<Library>($"/api/library", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for library to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
    
    /// <summary>
    /// Gets or sets the bound Flow
    /// </summary>
    private object BoundFlow
    {
        get => Model.Flow;
        set
        {
            if (value is ObjectReference objectReference)
                Model.Flow = objectReference;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound priority
    /// </summary>
    private object BoundPriority
    {
        get => Model.Priority;
        set
        {
            if (value is ProcessingPriority priority)
                Model.Priority = priority;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound processing order
    /// </summary>
    private object BoundProcessingOrder
    {
        get => Model.ProcessingOrder;
        set
        {
            if (value is ProcessingOrder order)
                Model.ProcessingOrder = order;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound DetectFileCreation
    /// </summary>
    private object BoundDetectFileCreation
    {
        get => Model.DetectFileCreation;
        set
        {
            if (value is MatchRange match)
                Model.DetectFileCreation = match;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound DetectFileLastWritten
    /// </summary>
    private object BoundDetectFileLastWritten
    {
        get => Model.DetectFileLastWritten;
        set
        {
            if (value is MatchRange match)
                Model.DetectFileLastWritten = match;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound DetectFileSize
    /// </summary>
    private object BoundDetectFileSize
    {
        get => Model.DetectFileSize;
        set
        {
            if (value is MatchRange match)
                Model.DetectFileSize = match;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound DetectFileCreationDate
    /// </summary>
    private DateTime BoundDetectFileCreationDate
    {
        get => Model.DetectFileCreationDate ?? DateTime.Today;
        set => Model.DetectFileCreationDate = value.Year > 1970 ? value : null;
    }
    
    /// <summary>
    /// Gets or sets the bound DetectFileLastWrittenDate
    /// </summary>
    private DateTime BoundDetectFileLastWrittenDate
    {
        get => Model.DetectFileLastWrittenDate ?? DateTime.Today;
        set => Model.DetectFileLastWrittenDate = value.Year > 1970 ? value : null;
    }
}