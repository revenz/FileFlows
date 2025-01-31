using System.Web;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Client.Components.ScriptEditor;
using FileFlows.Plugin;
using Humanizer;

namespace FileFlows.Client.Pages;

public partial class Tasks : ListPage<Guid, FileFlowsTask>
{
    public override string ApiUrl => "/api/task";

    private string lblLastRun, lblNever, lblTrigger;
    private string lblRunAt, lblSuccess, lblReturnCode, lblEditScript;

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
    
    private string lblFileAdded, lblFileProcessed, lblFileProcessing, lblFileProcessFailed, lblFileProcessSuccess, 
        lblFileFlowsServerUpdating,  lblFileFlowsServerUpdateAvailable, lblCustomSchedule;

    /// <summary>
    /// The script importer
    /// </summary>
    private Components.Dialogs.ImportScript ScriptImporter;

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.Tasks); 
    
    protected override void OnInitialized()
    {
        lblNever = Translater.Instant("Labels.Never");
        lblLastRun = Translater.Instant("Labels.LastRun");
        lblTrigger = Translater.Instant("Labels.Trigger");
        lblRunAt = Translater.Instant("Labels.RunAt");
        lblSuccess = Translater.Instant("Labels.Success");
        lblReturnCode = Translater.Instant("Labels.ReturnCode");
        lblEditScript = Translater.Instant("Pages.Tasks.Buttons.EditScript");

        lblCustomSchedule = Translater.Instant("Pages.Tasks.Fields.CustomSchedule");
        lblFileAdded = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileAdded)}");
        lblFileProcessed = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessed)}");
        lblFileProcessing = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessing)}");
        lblFileProcessFailed = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessFailed)}");
        lblFileProcessSuccess = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessSuccess)}");
        lblFileFlowsServerUpdating = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileFlowsServerUpdating)}");
        lblFileFlowsServerUpdateAvailable = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileFlowsServerUpdateAvailable)}");
        base.OnInitialized();
    }

    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;


    /// <inheritdoc />
    public async override Task PostLoad()
    {
        try
        {
            var result = await HttpHelper.Get<Dictionary<Guid, string>>("/api/script/basic-list?type=system");
            if (result.Success)
                Scripts = result.Data.Where(x => x.Value != CommonVariables.FILE_DISPLAY_NAME).ToDictionary();
        }
        catch (Exception)
        {
            Toast.ShowError("Failed loading scripts");
        }
    }

    /// <inheritdoc />
    public override Task<List<FileFlowsTask>> PostLoadGotData(List<FileFlowsTask> data)
    {
        if(initialSortOrder == null)
        {
            data = data.OrderByDescending(x => x.Enabled).ThenBy(x => x.Name)
                .ToList();
            initialSortOrder = data.Select(x => x.Uid).ToList();
        }
        else
        {
            data = data.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenBy(x => x.Name)
                .ToList();
        }
        return Task.FromResult(data);
    }
    
    /// <summary>
    /// Gets the text for the schedule
    /// </summary>
    /// <param name="task">the task</param>
    /// <returns>the schedule text</returns>
    private string GetSchedule(FileFlowsTask task)
    {
        if (task.Type != TaskType.Schedule)
            return string.Empty;
        if (task.Schedule == SCHEDULE_HOURLY) return "Hourly";
        if (task.Schedule == SCHEDULE_3_HOURLY) return "Every 3 Hours";
        if (task.Schedule == SCHEDULE_6_HOURLY) return "Every 6 Hours";
        if (task.Schedule == SCHEDULE_12_HOURLY) return "Every 12 Hours";
        if (task.Schedule == SCHEDULE_DAILY) return "Daily";
        return "Custom Schedule";
    }
    
    /// <summary>
    /// Adds a new task
    /// </summary>
    private async Task Add()
    {
        await Edit(new FileFlowsTask()
        {
            Schedule = SCHEDULE_DAILY
        });
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(FileFlowsTask item)
    {
        List<IFlowField> fields = new ();

        // var scriptResponse = await HttpHelper.Get<Dictionary<string, string>>("/api/script/basic-list?type=system");
        // if (scriptResponse.Success == false)
        // {
        //     Toast.ShowError(scriptResponse.Body);
        //     return false;
        // }


        if (Scripts.Any() != true)
        {
            Toast.ShowError("Pages.Tasks.Messages.NoScripts");
            return false;
        }

        var scriptOptions = Scripts.Select(x => new ListOption()
        {
            Label = x.Value,
            Value = x.Key
        }).ToList();

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<Validator> {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Script),
            Parameters = new ()
            {
                { nameof(InputSelect.Options), scriptOptions }
            }
        });
        var efTaskType = new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Type),
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<TaskType>().Select(x => new ListOption()
                        { Label = x.Humanize(LetterCasing.Title).Replace("File Flows", "FileFlows"), Value = x }).ToList()
                }
            }
        };
        fields.Add(efTaskType);

        TimeSchedule timeSchedule = TimeSchedule.Hourly;
        if (item.Type == TaskType.Schedule)
        {
            if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_12_HOURLY)
                timeSchedule = TimeSchedule.Every12Hours;
            else if (item.Schedule == SCHEDULE_6_HOURLY)
                timeSchedule = TimeSchedule.Every6Hours;
            else if (item.Schedule == SCHEDULE_3_HOURLY)
                timeSchedule = TimeSchedule.Every3Hours;
            else if (item.Schedule == SCHEDULE_HOURLY)
                timeSchedule = TimeSchedule.Hourly;
            else if(item.Schedule != new string('0', 672))
                timeSchedule = TimeSchedule.Custom;
        }

        var efSchedule = new ElementField
        {
            InputType = FormInputType.Select,
            Name = "TimeSchedule",
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<TimeSchedule>().Select(x => new ListOption()
                        { Label = x.Humanize(LetterCasing.Title), Value = x }).ToList()
                }
            },
            Conditions = new List<Condition>
            {
                new(efTaskType, item.Type, value: TaskType.Schedule)
            }
        };
        fields.Add(efSchedule);

        string customSchedule = SCHEDULE_HOURLY;
        if (item.Type == TaskType.Schedule)
        {
            if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_3_HOURLY)
                timeSchedule = TimeSchedule.Every3Hours;
            else if (item.Schedule == SCHEDULE_6_HOURLY)
                timeSchedule = TimeSchedule.Every6Hours;
            else if (item.Schedule == SCHEDULE_12_HOURLY)
                timeSchedule = TimeSchedule.Every12Hours;
            else if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_HOURLY)
                timeSchedule = TimeSchedule.Hourly;
            else
                timeSchedule = TimeSchedule.Custom;
            customSchedule = item.Schedule?.EmptyAsNull() ?? SCHEDULE_HOURLY;
        }
        
        fields.Add(new ElementField
        {
            InputType = FormInputType.Schedule,
            Name = "CustomSchedule",
            Parameters = new ()
            {
                { nameof(InputSchedule.HideLabel), true }
            },
            Conditions = new List<Condition>
            {
                new (efTaskType, item.Type, value: TaskType.Schedule),
                new (efSchedule, timeSchedule, value: TimeSchedule.Custom)
            }
        });
        await Editor.Open(new()
        {
            TypeName = "Pages.Task", Title = "Pages.Task.Title", Fields = fields, Model = new
            {
                item.Uid,
                item.Name,
                item.Script,
                item.Type,
                CustomSchedule = customSchedule,
                TimeSchedule = timeSchedule
            },
            SaveCallback = Save
        });
        
        return false;
    }
    
    
    /// <summary>
    /// Saves a task
    /// </summary>
    /// <param name="model">the model of the task to save</param>
    /// <returns>true if successful and if the editor should be closed</returns>
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();
        var task = new FileFlowsTask();
        var dict = model as IDictionary<string, object>;
        task.Name = dict["Name"].ToString() ?? string.Empty;
        task.Script = (Guid)dict["Script"];
        task.Uid = (Guid)dict["Uid"];
        task.Type = (TaskType)dict["Type"];
        if (task.Type == TaskType.Schedule)
        {
            var timeSchedule = (TimeSchedule)dict["TimeSchedule"];
            switch (timeSchedule)
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
                    task.Schedule = (string)dict["CustomSchedule"];
                    break;
            }
        }

        try
        {
            var saveResult = await HttpHelper.Post<FileFlowsTask>($"{ApiUrl}", task);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
    
    /// <summary>
    /// Runs the task on the server
    /// </summary>
    async Task Run()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        try
        {
            var result = await HttpHelper.Post<FileFlowsTaskRun>($"/api/task/run/{item.Uid}");
            if (result.Success && result.Data.Success)
            {
                Toast.ShowSuccess("Script executed");
            }
            else
            {
                Toast.ShowError(result?.Data?.Log?.EmptyAsNull() ?? result.Body?.EmptyAsNull() ?? "Failed to run task");
            }

            await Refresh();
        }
        finally
        {
            this.Blocker.Hide();
        }
    }


    /// <summary>
    /// Gets the run history for the selected task and shows it
    /// </summary>
    async Task RunHistory()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;

        var blockerHidden = false;
        Blocker.Show();
        try
        {
            var taskResponse = await HttpHelper.Get<FileFlowsTask>("/api/task/" + item.Uid);
            if (taskResponse.Success == false)
            {
                Toast.ShowError(taskResponse.Body);
                return;
            }

            if (taskResponse.Data?.RunHistory?.Any() != true)
            {
                Toast.ShowInfo("Pages.Tasks.Messages.NoHistory");
                return;
            }

            var task = taskResponse.Data;
            Blocker.Hide();
            blockerHidden = true;
            _ = TaskHistory(task);
        }
        finally
        {
            if (blockerHidden == false)
                Blocker.Hide();
        }
    }

    /// <summary>
    /// Shows the task history
    /// </summary>
    /// <param name="task">the task to show the history</param>
    async Task TaskHistory(FileFlowsTask task)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField()
        {
            InputType = FormInputType.Table,
            Name = nameof(task.RunHistory),
            Parameters = new ()
            {
                { nameof(InputTable.TableType), typeof(FileFlowsTaskRun) },
                { nameof(InputTable.Columns) , new List<InputTableColumn>
                {
                    new () { Name = lblRunAt, Property = nameof(FileFlowsTaskRun.RunAt) },
                    new () { Name = lblSuccess, Property = nameof(FileFlowsTaskRun.Success) },
                    new () { Name = lblReturnCode, Property = nameof(FileFlowsTaskRun.ReturnValue) }
                }},
                { nameof(InputTable.SelectAction), new Action<object>(item =>
                {
                    if (item is FileFlowsTaskRun run == false)
                        return; // should never happen
                    _ = TaskRun(run);
                })}
            }
        });
        
        await Editor.Open(new()
        {
            TypeName = "Pages.FileFlowsTaskHistory", Title = "Pages.FileFlowsTaskHistory.Title",
            ReadOnly = true,
            Fields = fields,
            Model = new {
                RunHistory = task.RunHistory.OrderByDescending(x => x.RunAt).ToList()
            }
        });
        
    }

    async Task TaskRun(FileFlowsTaskRun fileFlowsTaskRun)
    {
        List<IFlowField> fields = new ();

        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(fileFlowsTaskRun.RunAt)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(fileFlowsTaskRun.Success)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(fileFlowsTaskRun.ReturnValue)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.LogView,
            Name = nameof(fileFlowsTaskRun.Log)
        });

        await Editor.Open(new()
        {
            TypeName = "Pages.FileFlowsTaskRun", Title = "Pages.FileFlowsTaskRun.Title",
            Model = fileFlowsTaskRun,
            ReadOnly = true,
            Fields = fields
        });
    }

    async Task EditScript()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;

        this.Blocker.Show();
        Script script;
        try
        {
            var response =
                await HttpHelper.Get<Script>("/api/script/" + HttpUtility.UrlPathEncode(item.Script.ToString()) + "?type=System");
            if (response.Success == false)
            {
                Toast.ShowError("Failed to load script");
                return;
            }

            script = response.Data!;
        }
        finally
        {
            this.Blocker.Hide();
        }

        var editor = new ScriptEditor(Editor, ScriptImporter, blocker: Blocker);
        await editor.Open(script);
    }

    /// <summary>
    /// Gets the icon for a task
    /// </summary>
    /// <param name="task">the task</param>
    /// <returns>the icon</returns>
    private string GetIcon(FileFlowsTask task)
    {
        return task.Type switch
        {
            TaskType.FileAdded => "fas fa-folder-plus",
            TaskType.FileProcessed => "fas fa-thumbs-up",
            TaskType.FileProcessing => "fas fa-running",
            TaskType.FileProcessFailed => "fas fa-exclamation-circle",
            TaskType.FileProcessSuccess => "fas fa-check-circle",
            TaskType.FileFlowsServerUpdating => "fas fa-hourglass-half",
            TaskType.FileFlowsServerUpdateAvailable => "fas fa-cloud-download-alt",
            _ => "fas fa-clock"
        };
    }

    /// <summary>
    /// Gets the text for a task
    /// </summary>
    /// <param name="task">the task</param>
    /// <returns>the text</returns>
    private string GetScheduleText(FileFlowsTask task)
        => task.Type switch
        {
            TaskType.FileAdded => lblFileAdded,
            TaskType.FileProcessed => lblFileProcessed,
            TaskType.FileProcessing => lblFileProcessing,
            TaskType.FileProcessSuccess => lblFileProcessSuccess,
            TaskType.FileProcessFailed => lblFileProcessFailed,
            TaskType.FileFlowsServerUpdating => lblFileFlowsServerUpdating,
            TaskType.FileFlowsServerUpdateAvailable => lblFileFlowsServerUpdateAvailable,
            _ => GetSchedule(task)
        };
}