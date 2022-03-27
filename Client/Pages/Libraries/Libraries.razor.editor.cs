namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using FileFlows.Client.Components;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using FileFlows.Plugin;
    using System;
    using FileFlows.Client.Components.Inputs;

    public partial class Libraries : ListPage<Library>
    {
        ElementField efTemplate;

        private async Task<bool> OpenEditor(Library library)
        {
            Blocker.Show();
            var flowResult = await GetFlows();
            Blocker.Hide();
            if (flowResult.Success == false || flowResult.Data?.Any() != true)
            {
                ShowEditHttpError(flowResult, "Pages.Libraries.ErrorMessages.NoFlows");
                return false;
            }
            var flowOptions = flowResult.Data.Select(x => new ListOption { Value = new ObjectReference { Name = x.Name, Uid = x.Uid, Type = x.GetType().FullName }, Label = x.Name });
            efTemplate = null;

            var tabs = new Dictionary<string, List<ElementField>>();
            tabs.Add("General", await TabGeneral(library, flowOptions));
            tabs.Add("Schedule", TabSchedule(library));
            tabs.Add("Advanced", TabAdvanced(library));
            var result = await Editor.Open("Pages.Library", "Pages.Library.Title", null, library, saveCallback: Save, tabs: tabs);
            if (efTemplate != null)
            {
                efTemplate.ValueChanged -= TemplateValueChanged;
                efTemplate = null;
            }
            return true;
        }


        private async Task<List<ElementField>> TabGeneral(Library library, IEnumerable<ListOption> flowOptions)
        {
            List<ElementField> fields = new List<ElementField>();
#if (!DEMO)
            if (library == null || library.Uid == Guid.Empty)
            {
                // adding
                Blocker.Show();
                try
                {
                    var templateResult = await HttpHelper.Get<Dictionary<string, List<Library>>>("/api/library/templates");
                    if (templateResult.Success == true || templateResult.Data?.Any() == true)
                    {
                        List<ListOption> templates = new();
                        foreach (var group in templateResult.Data)
                        {
                            if (string.IsNullOrEmpty(group.Key) == false)
                            {
                                templates.Add(new ListOption
                                {
                                    Value = Globals.LIST_OPTION_GROUP,
                                    Label = group.Key
                                });
                            }
                            templates.AddRange(group.Value.Select(x => new ListOption
                            {
                                Label = x.Name,
                                Value = x
                            }));
                        }
                        templates.Insert(0, new ListOption
                        {
                            Label = "Custom",
                            Value = null
                        });
                        efTemplate = new ElementField
                        {
                            Name = "Template",
                            InputType = FormInputType.Select,
                            Parameters = new Dictionary<string, object>
                            {
                                { nameof(InputSelect.Options), templates },
                                { nameof(InputSelect.AllowClear), false},
                                { nameof(InputSelect.ShowDescription), true }
                            }
                        };
                        efTemplate.ValueChanged += TemplateValueChanged;
                        fields.Add(efTemplate);
                        fields.Add(new ElementField
                        {
                            InputType = FormInputType.HorizontalRule
                        });
                    }
                }
                finally
                {
                    Blocker.Hide();
                }
            }
#endif
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(library.Name),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Folder,
                Name = nameof(library.Path),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(library.Folders)
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(library.Flow),
                Parameters = new Dictionary<string, object>{
                    { "Options", flowOptions.ToList() }
                },
                Validators = new List<FileFlows.Shared.Validators.Validator>
                {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(library.Priority),
                Parameters = new Dictionary<string, object>{
                    { "AllowClear", false },
                    { "Options", new List<ListOption> {
                        new ListOption { Value = ProcessingPriority.Lowest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Lowest)}" },
                        new ListOption { Value = ProcessingPriority.Low, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Low)}" },
                        new ListOption { Value = ProcessingPriority.Normal, Label =$"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Normal)}" },
                        new ListOption { Value = ProcessingPriority.High, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.High)}" },
                        new ListOption { Value = ProcessingPriority.Highest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Highest)}" }
                    } }
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(library.Enabled)
            });
            return fields;
        }

        private List<ElementField> TabSchedule(Library library)
        {
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FormInputType.Label,
                Name = "ScheduleDescription"
            });

            fields.Add(new ElementField
            {
                InputType = FormInputType.Schedule,
                Name = nameof(library.Schedule),
                Parameters = new Dictionary<string, object>
                {
                    { "HideLabel", true }
                }
            });
            return fields;
        }

        private List<ElementField> TabAdvanced(Library library)
        {
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(library.Filter)
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(library.ExcludeHidden)
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(library.UseFingerprinting)
            });
            var fieldScan = new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(library.Scan)
            };
            fields.Add(fieldScan);
            fields.Add(new ElementField
            {
                InputType = FormInputType.Int,
                Parameters = new Dictionary<string, object>
                {
                    { "Min", 10 },
                    { "Max", 24 * 60 * 60 }
                },
                Name = nameof(library.ScanInterval),
                DisabledConditions = new List<Condition>
                {
                    new EmptyCondition(fieldScan, library.Scan)
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Int,
                Parameters = new Dictionary<string, object>
                {
                    { "Min", 0 },
                    { "Max", 300 }
                },
                Name = nameof(library.FileSizeDetectionInterval),
                DisabledConditions = new List<Condition>
                {
                    new EmptyCondition(fieldScan, library.Scan)
                }
            });
            return fields;
        }
    }
}
