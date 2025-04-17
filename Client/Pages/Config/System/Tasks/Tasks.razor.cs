using FileFlows.Client.Components.Editors;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class Tasks : ListPage<Guid, FileFlowsTask>
{
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    public override string ApiUrl => "/api/task";

    private string lblRunAt, lblSuccess, lblReturnCode, lblEditScript;

    private static readonly string SCHEDULE_HOURLY = string.Concat(Enumerable.Repeat("1000", 24 * 7));
    private static readonly string SCHEDULE_3_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 11), 8 * 7));
    private static readonly string SCHEDULE_6_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 23), 4 * 7));
    private static readonly string SCHEDULE_12_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 47), 2 * 7));
    private static readonly string SCHEDULE_DAILY = string.Concat(Enumerable.Repeat("1" + new string('0', 95), 7));

    private Dictionary<Guid, string> Scripts = new ();
    
    private string lblFileAdded, lblFileProcessed, lblFileProcessing, lblFileProcessFailed, lblFileProcessSuccess, 
        lblFileFlowsServerUpdating,  lblFileFlowsServerUpdateAvailable, lblCustomSchedule;

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.Tasks); 
    
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.Tasks.Title"), "fas fa-clock");
        lblRunAt = Translater.Instant("Labels.RunAt");
        lblSuccess = Translater.Instant("Labels.Success");
        lblReturnCode = Translater.Instant("Labels.ReturnCode");
        lblEditScript = Translater.Instant("Pages.Tasks.Buttons.EditScript");

        lblCustomSchedule = Translater.Instant("Pages.Task.Fields.CustomSchedule");
        lblFileAdded = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileAdded)}");
        lblFileProcessed = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessed)}");
        lblFileProcessing = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessing)}");
        lblFileProcessFailed = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessFailed)}");
        lblFileProcessSuccess = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileProcessSuccess)}");
        lblFileFlowsServerUpdating = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileFlowsServerUpdating)}");
        lblFileFlowsServerUpdateAvailable = Translater.Instant($"Enums.{nameof(TaskType)}.{nameof(TaskType.FileFlowsServerUpdateAvailable)}");
        
        
        Scripts = feService.Script.Scripts.Where(x =>
                x.Type == ScriptType.System)
            .ToDictionary(x => x.Uid, x => x.Name);

        base.OnInitialized();
    }

    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;

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
        var result = await ModalService.ShowModal<TaskEditor, FileFlowsTask>(new ModalEditorOptions()
        {
            Model = new FileFlowsTask()
            {
                Schedule = SCHEDULE_DAILY,
                Enabled = true
            }
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(FileFlowsTask item)
    {
        var result = await ModalService.ShowModal<TaskEditor, FileFlowsTask>(new ModalEditorOptions()
        {
            Uid = item.Uid
        });
        if(result.IsFailed == false)
            await Load(result.Value.Uid);
        return false;
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
                feService.Notifications.ShowSuccess("Script executed");
            }
            else
            {
                feService.Notifications.ShowError(result?.Data?.Log?.EmptyAsNull() ?? result.Body?.EmptyAsNull() ?? "Failed to run task");
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
                feService.Notifications.ShowError(taskResponse.Body);
                return;
            }

            if (taskResponse.Data?.RunHistory?.Any() != true)
            {
                feService.Notifications.ShowInfo("Pages.Tasks.Messages.NoHistory");
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

        await ModalService.ShowModal<FileFlows.Client.Components.Editors.ScriptEditor, Script>(new ModalEditorOptions()
        {
            Uid = item.Script
        });
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