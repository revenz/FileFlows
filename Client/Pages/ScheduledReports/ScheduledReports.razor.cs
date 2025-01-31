using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for scheduled reports
/// </summa
public partial class ScheduledReports : ListPage<Guid, ScheduledReport>
{
    /// <summary>
    /// Gets or sets the report form editor component
    /// </summary>
    private Editor ReportFormEditor { get; set; }
    
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject]
    private ClientService ClientService { get; set; }

    /// <inheritdoc />
    public override string ApiUrl => "/api/scheduled-report";

    private List<ReportDefinition> ReportDefinitions;
    private string lblDaily, lblWeekly, lblMonthly, lblLastSent, lblNever;

    /// <summary>
    /// The types we can show in the report editor
    /// </summary>
    private List<ListOption> Flows, Libraries, Nodes, Tags;

    private ElementField efFlows, efLibraries, efNodes, efTags, efDirection;
    
    private ElementField efShowLibraries = new ()
    {
        InputType = FormInputType.Hidden,
        Name = "ShowLibraries"
    };
    private ElementField efShowNodes = new ()
    {
        InputType = FormInputType.Hidden,
        Name = "ShowNodes"
    };
    private ElementField efShowFlows = new ()
    {
        InputType = FormInputType.Hidden,
        Name = "ShowFlows"
    };
    private ElementField efShowTags = new ()
    {
        InputType = FormInputType.Hidden,
        Name = "ShowTags"
    };
    private ElementField efShowDirection = new ()
    {
        InputType = FormInputType.Hidden,
        Name = "ShowDirection"
    };
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblDaily = Translater.Instant($"Enums.{nameof(ReportSchedule)}.{nameof(ReportSchedule.Daily)}");
        lblWeekly = Translater.Instant($"Enums.{nameof(ReportSchedule)}.{nameof(ReportSchedule.Weekly)}");
        lblMonthly = Translater.Instant($"Enums.{nameof(ReportSchedule)}.{nameof(ReportSchedule.Monthly)}");

        lblLastSent = Translater.Instant("Pages.ScheduledReport.Labels.LastSent");
        lblNever = Translater.Instant("Pages.ScheduledReport.Labels.Never");
    }

    /// <inheritdoc />
    public override async Task PostLoad()
    {
        var result = await HttpHelper.Get<List<ReportDefinition>>("/api/report/definitions");
        if (result.Success == false || result.Data?.Any() != true)
            NavigationManager.NavigateTo("/");

        ReportDefinitions = result.Data;
    }

    /// <summary>
    /// Adds a new task
    /// </summary>
    private async Task Add()
    {
        await Edit(new ScheduledReport()
        {
            Schedule = ReportSchedule.Weekly,
            Enabled = true,
            Recipients = string.IsNullOrWhiteSpace(Profile?.Email) == false ? [Profile.Email] : []
        });
    }

    /// <inheritdoc />
    public override async Task<bool> Edit(ScheduledReport item)
    {
        Blocker.Show();
        bool blockerShow = true;
        try
        {
            if (Flows == null)
            {
                var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/library/basic-list");
                Libraries = librariesResult?.Data?.Select(x => new ListOption
                {
                    Label = x.Value,
                    Value = x.Key
                }).OrderBy(x => x.Label?.ToLowerInvariant())?.ToList() ?? [];

                var nodesResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/node/basic-list");
                Nodes = nodesResult?.Data?.Select(x => new ListOption
                {
                    Label = x.Value,
                    Value = x.Key
                }).OrderBy(x => x.Label?.ToLowerInvariant())?.ToList() ?? [];

                var flowsResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/flow/basic-list");
                Flows = flowsResult?.Data?.Select(x => new ListOption
                {
                    Label = x.Value,
                    Value = x.Key
                }).OrderBy(x => x.Label?.ToLowerInvariant())?.ToList() ?? [];

                Tags = (await ClientService.GetTags() ?? []).Select(x => new ListOption()
                {
                    Label = x.Name,
                    Value = x.Uid
                }).OrderBy(x => x.Label?.ToLowerInvariant())?.ToList() ?? [];
            }
            Blocker.Hide();
            blockerShow = false;

            var (fields, model) = GetFieldsAndModel(item);
            
            await Editor.Open(new()
            {
                TypeName = "Pages.ScheduledReport", Title = "Pages.ScheduledReport.Title", 
                Fields = fields, Model = model,
                SaveCallback = Save
            });

            return false;
        }
        finally
        {
            if(blockerShow)
                Blocker.Hide();
        }
    }
    
    private (List<IFlowField> fields, object model) GetFieldsAndModel(ScheduledReport item)
    {
        List<IFlowField> fields = new ();

        int dayOfMonth = Math.Clamp(item.Schedule == ReportSchedule.Monthly ? item.ScheduleInterval : 1, 1, 31);
        int dayOfWeek = Math.Clamp(item.Schedule == ReportSchedule.Weekly ? item.ScheduleInterval : 0, 0, 6);

        var rd = ReportDefinitions.FirstOrDefault(x => x.Uid == item.Report?.Uid);

        var reportOptions = ReportDefinitions.Select(x => new ListOption()
            {
                Label = Translater.Instant($"Reports.{x.Type}.Name"),
                Value = new ObjectReference { Uid = x.Uid, Name = x.Type }
            })
            .OrderBy(x => x.Label.ToLowerInvariant()).ToList();

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<Validator>
            {
                new Required()
            }
        });
        var efReport = new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Report),
            Parameters = new()
            {
                { nameof(InputSelect.Options), reportOptions }
            }
        };
        efReport.ValueChanged += ReportValueChanged;
        fields.Add(efReport);
        
        var efSchedule = new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Schedule),
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<ReportSchedule>().Select(x => new ListOption()
                        { Label = GetScheduleText(x), Value = x }).ToList()
                }
            }
        };
        fields.Add(efSchedule);
        
        fields.Add(new ElementField()
        {
            InputType = FormInputType.Select,
            Name = "DayOfWeek",
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<DayOfWeek>().Select(x => new ListOption()
                        { Label = Translater.Instant($"Enums.Days.{x}"), Value = x }).ToList()
                }
            },
            Conditions = new List<Condition>
            {
                new (efSchedule, item.Schedule, value: ReportSchedule.Weekly)
            }
        });
        
        fields.Add(new ElementField()
        {
            InputType = FormInputType.Int,
            Name = "DayOfMonth",
            Validators = new List<Validator> {
                new Validators.Range() { Minimum = 1, Maximum = 31 } 
            },
            Parameters = new()
            {
                { "Min", 1 },
                { "Max", 31 }
            },
            Conditions = new List<Condition>
            {
                new (efSchedule, item.Schedule, value: ReportSchedule.Monthly)
            }
        });
        
        fields.Add(new ElementField
        {
            InputType = FormInputType.StringArray,
            Name = nameof(item.Recipients),
            Validators = new List<Validator>
            {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(item.Enabled)
        });
        
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        
        var showNodes = rd != null && rd.NodeSelection != ReportSelection.None;
        var showFlows = rd != null && rd.FlowSelection != ReportSelection.None;
        var showLibraries = rd != null && rd.LibrarySelection != ReportSelection.None;
        var showTags = rd != null && rd.TagSelection != ReportSelection.None;
        var showDirection = rd?.Direction == true;
        fields.Add(efShowNodes);
        fields.Add(efShowLibraries);
        fields.Add(efShowFlows);
        fields.Add(efShowTags);
        fields.Add(efShowDirection);
        
        
        efDirection = new ElementField()
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Direction),
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    new List<ListOption>()
                    {
                        new () { Label = Translater.Instant($"Enums.ReportDirection.Inbound"), Value = 0 },
                        new () { Label = Translater.Instant($"Enums.ReportDirection.Outbound"), Value = 1 },
                    }
                }
            },
            Conditions = new List<Condition>()
            {
                new(efShowDirection, showDirection, value: true)
            }
        };
        fields.Add(efDirection);
        
        efNodes = new ElementField()
        {
            Name = nameof(item.Nodes),
            InputType = FormInputType.MultiSelect,
            Parameters = new()
            {
                { nameof(InputMultiSelect.Options), Nodes },
                { nameof(InputMultiSelect.AnyOrAll), true },
                { "LabelAny", Translater.Instant("Pages.Report.Labels.Combined") }
            },
            Conditions = new List<Condition>()
            {
                new (efShowNodes, showNodes, value: true)
            }
        };
        fields.Add(efNodes);
        efLibraries =new ElementField()
        {
            Name = nameof(item.Libraries),
            InputType = FormInputType.MultiSelect,
            Parameters = new()
            {
                { nameof(InputMultiSelect.Options), Libraries },
                { nameof(InputMultiSelect.AnyOrAll), true },
                { "LabelAny", Translater.Instant("Pages.Report.Labels.Combined") }
            },
            Conditions = new List<Condition>()
            {
                new (efShowLibraries, showLibraries, value: true)
            }
        };
        fields.Add(efLibraries);
        efFlows = new ElementField()
        {
            Name = nameof(item.Flows),
            InputType = FormInputType.MultiSelect,
            Parameters = new()
            {
                { nameof(InputMultiSelect.Options), Flows },
                { nameof(InputMultiSelect.AnyOrAll), true },
                { "LabelAny", Translater.Instant("Pages.Report.Labels.Combined") }
            },
            Conditions = new List<Condition>()
            {
                new(efShowFlows, showFlows, value: true)
            }
        };
        fields.Add(efFlows);
        efTags = new ElementField()
        {
            Name = nameof(item.Tags),
            Label = "Pages.Tags.Title",
            InputType = FormInputType.MultiSelect,
            Parameters = new()
            {
                { nameof(InputMultiSelect.Options), Tags },
                { nameof(InputMultiSelect.AnyOrAll), true },
                { "LabelAny", Translater.Instant("Labels.Any") }
            },
            Conditions = new List<Condition>()
            {
                new(efShowTags, showTags, value: true)
            }
        };
        fields.Add(efTags);
        return (fields, new
        {
            item.Uid,
            item.Name,
            item.Schedule,
            item.Report,
            item.Recipients,
            item.Enabled,
            Nodes = GetUidValues(rd?.NodeSelection, item.Nodes),
            Flows = GetUidValues(rd?.FlowSelection, item.Flows),
            Libraries = GetUidValues(rd?.LibrarySelection, item.Libraries),
            Tags = GetUidValues(rd?.TagSelection, item.Tags),
            item.Direction,
            DayOfWeek = dayOfWeek,
            DayOfMonth = dayOfMonth,
            ShowNodes = showNodes,
            ShowFlows = showFlows,
            ShowLibraries = showLibraries,
            ShowTags = showTags,
            ShowDirection = showDirection,
        });
    }

    /// <summary>
    /// Gets the UID values for use in a multiselect
    /// </summary>
    /// <param name="selection">the selection</param>
    /// <param name="uids">the UIDs</param>
    /// <returns>the UIDs value</returns>
    private List<object> GetUidValues(ReportSelection? selection, Guid[] uids)
    {
        if (selection is null or ReportSelection.None)
            return new();
        if (uids == null)
            return new();
        if (uids.Length == 0)
            return new List<object> { null }; // this is any
        return uids.Select(x => (object)x).ToList();
    }

    /// <summary>
    /// Saves a scheduled report
    /// </summary>
    /// <param name="model">the model of the scheduled report to save</param>
    /// <returns>true if successful and if the editor should be closed</returns>
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();
        try
        {
            var report = new ScheduledReport();
            var dict = model as IDictionary<string, object>;
            report.Name = dict[nameof(report.Name)].ToString() ?? string.Empty;
            report.Report = (ObjectReference)dict[nameof(report.Report)];
            report.Uid = (Guid)dict[nameof(report.Uid)];
            report.Schedule = (ReportSchedule)dict[nameof(report.Schedule)];
            report.Recipients = (string[])dict[nameof(report.Recipients)];
            report.Enabled = (bool)dict[nameof(report.Enabled)];

            var rd = ReportDefinitions.FirstOrDefault(x => x.Uid == report.Report.Uid);
            if (rd == null)
            {
                Toast.ShowError("Report not found"); // shouldn't happen
                return false;
            }

            if (report.Schedule == ReportSchedule.Monthly)
                report.ScheduleInterval = Math.Clamp((int)dict["DayOfMonth"], 1, 31);
            else if (report.Schedule == ReportSchedule.Weekly)
                report.ScheduleInterval = Math.Clamp((int)dict["DayOfWeek"], 0, 6);
            else
                report.ScheduleInterval = 0;

            report.Direction = rd.Direction ? (int)dict["Direction"] : 0;
            report.Nodes = rd.NodeSelection != ReportSelection.None ? GetUids(dict, "Nodes") : null;
            report.Libraries = rd.LibrarySelection != ReportSelection.None ? GetUids(dict, "Libraries") : null;
            report.Flows = rd.FlowSelection != ReportSelection.None ? GetUids(dict, "Flows") : null;
            report.Tags = rd.FlowSelection != ReportSelection.None ? GetUids(dict, "Tags") : null;

            var saveResult = await HttpHelper.Post<ScheduledReport>($"{ApiUrl}", report);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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
    /// Gets the UIDs from the model
    /// </summary>
    /// <param name="model">the model</param>
    /// <param name="name">the name of the UIDs to get</param>
    /// <returns>the UIDs</returns>
    private Guid[]? GetUids(IDictionary<string, object> model, string name)
    {
        if (model.TryGetValue(name, out var o) == false || o == null)
            return null;
        if (o is List<object> list == false)
            return null;
        List<Guid> guids = new();
        for (int i = 0; i < list.Count; i++)
        {
            if(list[i] is Guid uid)
                guids.Add(uid);
        }
        return guids.ToArray();
    }

    /// <summary>
    /// Gets the schedule text
    /// </summary>
    /// <param name="schedule">the schedule</param>
    /// <returns>the schedule text</returns>
    private string GetScheduleText(ReportSchedule schedule)
        => schedule switch
        {
            ReportSchedule.Daily => lblDaily,
            ReportSchedule.Weekly => lblWeekly,
            ReportSchedule.Monthly => lblMonthly,
            _ => schedule.ToString()
        };
    
    /// <summary>
    /// The report value was changed
    /// </summary>
    /// <param name="sender">the element that sent this</param>
    /// <param name="value">the new value</param>
    private void ReportValueChanged(object sender, object value)
    {
        if (value == null)
            return;
        var objectReference = value as ObjectReference;
        if (objectReference == null)
            return;
        var rd = ReportDefinitions.FirstOrDefault(x => x.Uid == objectReference.Uid);
        if (rd == null)
            return;
        
        var editor = sender as Editor;
        if (editor == null)
            return;
        if (editor.Model == null)
            editor.Model = new ExpandoObject();
        IDictionary<string, object> model = editor.Model!;
        
        bool showNodes = rd.NodeSelection != ReportSelection.None;
        bool showLibraries = rd.LibrarySelection != ReportSelection.None;
        bool showFlows = rd.FlowSelection != ReportSelection.None;
        bool showTags = rd.TagSelection != ReportSelection.None;
        bool showDirection = rd.Direction;
        
        if (model["ShowNodes"] as bool? != showNodes)
        {
            SetModelProperty("ShowNodes",showNodes);
            efNodes.InvokeChange(efNodes.Conditions[0], showNodes == false);
        }
        if (model["ShowLibraries"] as bool? != showLibraries)
        {
            SetModelProperty("ShowLibraries", showLibraries);
            efLibraries.InvokeChange(efLibraries.Conditions[0], showLibraries == false);
        }
        if (model["ShowFlows"] as bool? != showFlows)
        {
            SetModelProperty("ShowFlows", showFlows);
            efFlows.InvokeChange(efFlows.Conditions[0], showFlows == false);
        }
        if (model["ShowTags"] as bool? != showTags)
        {
            SetModelProperty("ShowTags", showTags);
            efTags.InvokeChange(efTags.Conditions[0], showTags == false);
        }
        if (model["ShowDirection"] as bool? != showDirection)
        {
            SetModelProperty("ShowDirection", showDirection);
            efDirection.InvokeChange(efDirection.Conditions[0], showDirection == false);
        }
        
        editor.TriggerStateHasChanged();
        void SetModelProperty(string property, object value)
        {
            model[property] = value;
        }
    }
}
