using System.Web;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using Humanizer;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Task editor
/// </summary>
public partial class TaskEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets if the Task 
    /// </summary>
    public FileFlowsTask Model { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/system/tasks";
    
    
    private enum TimeSchedule
    {
        Hourly,
        Every3Hours,
        Every6Hours,
        Every12Hours,
        Daily,
        Custom
    }

    private static readonly string SCHEDULE_HOURLY = string.Concat(Enumerable.Repeat("1000", 24 * 7));
    private static readonly string SCHEDULE_3_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 11), 8 * 7));
    private static readonly string SCHEDULE_6_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 23), 4 * 7));
    private static readonly string SCHEDULE_12_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 47), 2 * 7));
    private static readonly string SCHEDULE_DAILY = string.Concat(Enumerable.Repeat("1" + new string('0', 95), 7));

    private Dictionary<Guid, string> Scripts = new ();
    
    private List<ListOption> ScriptOptions, TypeOptions, TimeScheduleOptions;
    private TimeSchedule Schedule;
    private string CustomSchedule = new string('1', 672);


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.Task.Title");
        
        Scripts = feService.Script.Scripts.Where(x =>
                x.Type == ScriptType.System)
            .ToDictionary(x => x.Uid, x => x.Name);
        
        ScriptOptions = Scripts.Select(x => new ListOption()
        {
            Label = x.Value,
            Value = x.Key
        }).ToList();

        TypeOptions = Enum.GetValues<TaskType>().Select(x => new ListOption()
            { Label = x.Humanize(LetterCasing.Title).Replace("File Flows", "FileFlows"), Value = x }).ToList();

        TimeScheduleOptions = Enum.GetValues<TimeSchedule>().Select(x => new ListOption()
            { Label = x.Humanize(LetterCasing.Title), Value = x }).ToList();
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is FileFlowsTask model)
        {
            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<FileFlowsTask>("/api/task/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Close();
                return;
            }

            Model = result.Data;
        }

        Schedule = TimeSchedule.Hourly;
        CustomSchedule = Model.Schedule;
        if (Model.Type == TaskType.Schedule)
        {
            if (Model.Schedule == SCHEDULE_DAILY)
                Schedule = TimeSchedule.Daily;
            else if (Model.Schedule == SCHEDULE_12_HOURLY)
                Schedule = TimeSchedule.Every12Hours;
            else if (Model.Schedule == SCHEDULE_6_HOURLY)
                Schedule = TimeSchedule.Every6Hours;
            else if (Model.Schedule == SCHEDULE_3_HOURLY)
                Schedule = TimeSchedule.Every3Hours;
            else if (Model.Schedule == SCHEDULE_HOURLY)
                Schedule = TimeSchedule.Hourly;
            else if (Model.Schedule != new string('0', 672))
                Schedule = TimeSchedule.Custom;
        }
        
        StateHasChanged(); // needed to update the title
    }

    
    /// <summary>
    /// Saves the Task
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            FileFlowsTask task = new()
            {
                Name = Model.Name,
                Script = Model.Script,
                Uid = Model.Uid,
                Type = Model.Type,
                Enabled = Model.Enabled
            };
            if (task.Type == TaskType.Schedule)
            {
                switch (Schedule)
                {
                    case TimeSchedule.Daily: task.Schedule = SCHEDULE_DAILY;
                        break;
                    case TimeSchedule.Hourly: task.Schedule = SCHEDULE_HOURLY;
                        break;
                    case TimeSchedule.Every3Hours: task.Schedule = SCHEDULE_3_HOURLY;
                        break;
                    case TimeSchedule.Every6Hours: task.Schedule = SCHEDULE_6_HOURLY;
                        break;
                    case TimeSchedule.Every12Hours: task.Schedule = SCHEDULE_12_HOURLY;
                        break;
                    default:
                        task.Schedule = CustomSchedule;
                        break;
                }
            }
            
            var saveResult = await HttpHelper.Post<FileFlowsTask>($"/api/task", task);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
    
    
    /// <summary>
    /// Gets or sets the bound Script
    /// </summary>
    private object BoundScript
    {
        get => Model.Script;
        set
        {
            if (value is Guid v)
                Model.Script = v;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound type
    /// </summary>
    private object BoundType
    {
        get => Model.Type;
        set
        {
            if (value is TaskType v)
                Model.Type = v;
        }
    }
    
    /// <summary>
    /// Gets or sets the bound schedule
    /// </summary>
    private object BoundSchedule
    {
        get => Schedule;
        set
        {
            if (value is TimeSchedule v)
                Schedule = v;
        }
    }
}