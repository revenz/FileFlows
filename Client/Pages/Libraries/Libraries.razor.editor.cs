using FileFlows.Plugin;
using FileFlows.Client.Components.Inputs;

namespace FileFlows.Client.Pages;

/// <summary>
/// Editor for Libraries
/// </summary>
public partial class Libraries : ListPage<Guid, Library>
{
    ElementField efTemplate;

    /// <summary>
    /// Opens the editor
    /// </summary>
    /// <param name="library">the library to edit</param>
    /// <returns>true if the editor was saved, otherwise false</returns>
    private async Task<bool> OpenEditor(Library library)
    {
        if (library.Uid == CommonVariables.ManualLibraryUid)
            return await OpenManualLibraryEditor(library);
        
        Blocker.Show();
        var flowResult = await GetFlows();
        Blocker.Hide();
        if (flowResult.Success == false || flowResult.Data?.Any() != true)
        {
            ShowEditHttpError(flowResult, "Pages.Libraries.ErrorMessages.NoFlows");
            return false;
        }
        var flowOptions = flowResult.Data
            .Select(x => new ListOption { Value = new ObjectReference { Name = x.Value, Uid = x.Key, Type = typeof(Flow).FullName! }, Label = x.Value });
        efTemplate = null;

        var tabs = new Dictionary<string, List<IFlowField>>();
        
        var efFolders = new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.Folders)
        };
        
        var tabGeneral = await TabGeneral(library, flowOptions, efFolders);
        tabs.Add("General", tabGeneral);
        tabs.Add("Schedule", TabSchedule(library));
        tabs.Add("Detection", TabDetection(library));
        //tabs.Add("Scan", TabScan(library));
        tabs.Add("Advanced", TabAdvanced(library, efFolders));
        await Editor.Open(new()
        {
            TypeName = "Pages.Library", Title = "Pages.Library.Title", Model = library, SaveCallback = Save, Tabs = tabs,
            HelpUrl = "https://fileflows.com/docs/webconsole/configuration/libraries/library"
        });
        if (efTemplate != null)
        {
            efTemplate.ValueChanged -= TemplateValueChanged;
            efTemplate = null;
        }
        return true;
    }


    private async Task<List<IFlowField>> TabGeneral(Library library, IEnumerable<ListOption> flowOptions, ElementField efFolders)
    {
        List<IFlowField> fields = new ();
        if (library == null || library.Uid == Guid.Empty)
        {
            // adding
            Blocker.Show();
            try
            {
                var templateResult = await HttpHelper.Get<Dictionary<string, List<Library>>>("/api/library/templates");
                if (templateResult.Success || templateResult.Data?.Any() == true)
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
                    var loCustom = new ListOption
                    {
                        Label = "Custom",
                        Value = null
                    };
                    
                    if(templates.Any() && (string)templates[0].Value! == Globals.LIST_OPTION_GROUP)
                        templates.Insert(1, loCustom);
                    else
                        templates.Insert(0, loCustom);
                    
                    efTemplate = new ElementField
                    {
                        Name = "Template",
                        InputType = FormInputType.Select,
                        Parameters = new Dictionary<string, object>
                        {
                            { nameof(InputSelect.Options), templates },
                            { nameof(InputSelect.AllowClear), true},
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
        
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(library.Name),
            Validators = new List<Validator> {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Folder,
            Name = nameof(library.Path),
            Validators = new List<Validator> {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.Flow),
            Parameters = new Dictionary<string, object>{
                { "Options", flowOptions.OrderBy(x => x.Label.ToLowerInvariant()).ToList() }
            },
            Validators = new List<Validator>
            {
                new Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.Priority),
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = ProcessingPriority.Lowest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Lowest)}" },
                    new () { Value = ProcessingPriority.Low, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Low)}" },
                    new () { Value = ProcessingPriority.Normal, Label =$"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Normal)}" },
                    new () { Value = ProcessingPriority.High, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.High)}" },
                    new () { Value = ProcessingPriority.Highest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Highest)}" }
                } }
            }
        });
        
        if(Profile.LicensedFor(LicenseFlags.ProcessingOrder))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(library.ProcessingOrder),
                Parameters = new Dictionary<string, object>{
                    { "AllowClear", false },
                    { "Options", new List<ListOption> {
                        new () { Value = ProcessingOrder.AsFound, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.AsFound)}" },
                        new () { Value = ProcessingOrder.Alphabetical, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Alphabetical)}" },
                        new () { Value = ProcessingOrder.SmallestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.SmallestFirst)}" },
                        new () { Value = ProcessingOrder.LargestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.LargestFirst)}" },
                        new () { Value = ProcessingOrder.NewestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.NewestFirst)}" },
                        new () { Value = ProcessingOrder.OldestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.OldestFirst)}" },
                        new () { Value = ProcessingOrder.Random, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Random)}" },
                    } }
                }
            });
        }
        
        fields.Add(new ElementField()
        {
            InputType = FormInputType.StringArray,
            Name = nameof(library.Extensions),
            Conditions = new List<Condition>
            {
                new (efFolders, library.Folders, value: false)
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Period,
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPeriod.ShowWeeks), false },
                { nameof(InputPeriod.Seconds), true }
            },
            Name = nameof(library.ScanInterval)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(library.HoldMinutes)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.Enabled)
        });
        
        return fields;
    }

    private List<IFlowField> TabSchedule(Library library)
    {
        List<IFlowField> fields = new ();
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
        if (Profile.LicensedFor(LicenseFlags.ProcessingOrder))
        {
            fields.Add(new ElementField()
            {
                InputType = FormInputType.Int,
                Name = nameof(library.MaxRunners)
            });
        }

        return fields;
    }

    private List<IFlowField> TabAdvanced(Library library, ElementField efFolders)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.StringArray,
            Name = nameof(library.Filters)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.StringArray,
            Name = nameof(library.ExclusionFilters)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Parameters = new Dictionary<string, object>
            {
                { "Min", 0 },
                { "Max", 300 }
            },
            Name = nameof(library.FileSizeDetectionInterval)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.ExcludeHidden)
        });
        fields.Add(efFolders);
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.SkipFileAccessTests),
            Conditions = new List<Condition>
            {
                new (efFolders, library.Folders, value: false)
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.TopLevelOnly),
            Conditions = new List<Condition>
            {
                new (efFolders, library.Folders, value: false)
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.DisableFileSystemEvents),
            Parameters = new ()
            {
                { nameof(InputSwitch.Inverse) , true}
            }
        });
        // var efFingerprinting = new ElementField
        // {
        //     InputType = FormInputType.Switch,
        //     Name = nameof(library.UseFingerprinting),
        //     Conditions = new List<Condition>
        //     {
        //         new(efFolders, library.Folders, value: false)
        //     }
        // };
        // fields.Add(efFingerprinting);
        // fields.Add(new ElementField
        // {
        //     InputType = FormInputType.Switch,
        //     Name = nameof(library.UpdateMovedFiles),
        //     DisabledConditions = new List<Condition>
        //     {
        //         new (efFingerprinting, library.UseFingerprinting, value: true)
        //     }
        // });
        // fields.Add(new ElementField
        // {
        //     InputType = FormInputType.Switch,
        //     Name = nameof(library.ReprocessRecreatedFiles)
        // });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(library.WaitTimeSeconds),
            Conditions = new List<Condition>
            {
                new Condition(efFolders, library.Folders, value: true)                    
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.DownloadsDirectory)
        });
        return fields;
    }

    private List<IFlowField> TabDetection(Library library)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField()
        {
            InputType = FormInputType.Label,
            Name = "DetectionDescription"
        });
        foreach (var prop in new[]
                 {
                     (Field: nameof(Library.DetectFileCreation), Type: "date", AddSeparator: true, InitialValue: library.DetectFileCreation),
                     (Field: nameof(library.DetectFileLastWritten), Type: "date", AddSeparator: true, InitialValue: library.DetectFileLastWritten),
                     (Field: nameof(library.DetectFileSize), Type: "size", AddSeparator: false, InitialValue: library.DetectFileSize)
                 })
        {
            var matchParameters = new Dictionary<string, object>
            {
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = (int)MatchRange.Any, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Any)}" },
                    new () { Value = MatchRange.GreaterThan, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.GreaterThan)}" },
                    new () { Value = MatchRange.LessThan, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.LessThan)}" },
                    new () { Value = MatchRange.Between, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Between)}" },
                    new () { Value = MatchRange.NotBetween, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.NotBetween)}" }
                } }
            };
            if (prop.Type == "date")
            {
                ((List<ListOption>)matchParameters["Options"]).AddRange(new ListOption[]
                {
                    new () { Value = MatchRange.After, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.After)}" },
                    new () { Value = MatchRange.Before, Label = $"Enums.{nameof(MatchRange)}.{nameof(MatchRange.Before)}" }
                });
            }
            
            var efDetection = new ElementField
            {
                InputType = FormInputType.Select,
                Name = prop.Field,
                Parameters = matchParameters
            };
            fields.Add(efDetection);
            fields.Add(new ElementField
            {
                InputType = prop.Type == "date" ? FormInputType.Period : FormInputType.FileSize,
                Name = prop.Field + "Lower",
                Conditions = new List<Condition>
                {
                    new AnyCondition(efDetection, prop.InitialValue, new []
                    {
                        MatchRange.GreaterThan, MatchRange.LessThan, MatchRange.Between, MatchRange.NotBetween
                    })
                }
            });
            fields.Add(new ElementField
            {
                InputType = prop.Type == "date" ? FormInputType.Period : FormInputType.FileSize,
                Name = prop.Field + "Upper",
                Conditions = new List<Condition>
                {
                    new AnyCondition(efDetection, prop.InitialValue, new [] { MatchRange.Between, MatchRange.NotBetween})
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Date,
                Name = prop.Field + "Date",
                Conditions = new List<Condition>
                {
                    new AnyCondition(efDetection, prop.InitialValue, new []
                    {
                        MatchRange.After, MatchRange.Before
                    })
                }
            });
            
            if(prop.AddSeparator)
                fields.Add(ElementField.Separator());
        }

        return fields;
    }
    
    
    // private List<IFlowField> TabScan(Library library)
    // {
    //     List<IFlowField> fields = new ();
    //     
    //     // var fieldScan = new ElementField
    //     // {
    //     //     InputType = FormInputType.Switch,
    //     //     Name = nameof(library.Scan)
    //     // };
    //     // fields.Add(fieldScan);
    //     
    //     // fields.Add(new ElementField
    //     // {
    //     //     InputType = FormInputType.Int,
    //     //     Parameters = new Dictionary<string, object>
    //     //     {
    //     //         { "Min", 10 },
    //     //         { "Max", 24 * 60 * 60 }
    //     //     },
    //     //     Name = nameof(library.ScanInterval),
    //     //     // Conditions = new List<Condition>
    //     //     // {
    //     //     //     new (fieldScan, library.Scan, value: true)
    //     //     // }
    //     // });
    //     // var efFullScanEnabled = new ElementField
    //     // {
    //     //     InputType = FormInputType.Switch,
    //     //     Name = nameof(library.FullScanDisabled),
    //     //     Conditions = new List<Condition>
    //     //     {
    //     //         new(fieldScan, library.Scan, value: false)
    //     //     }
    //     // };
    //     // fields.Add(efFullScanEnabled);
    //     // fields.Add(new ElementField
    //     // {
    //     //     InputType = FormInputType.Period,
    //     //     Name = nameof(library.FullScanIntervalMinutes),
    //     //     Conditions = new List<Condition>
    //     //     {
    //     //         new (fieldScan, library.Scan, value: false),
    //     //     },
    //     //     DisabledConditions =new List<Condition>
    //     //     {
    //     //         new (efFullScanEnabled, library.FullScanDisabled, value: false),
    //     //     }, 
    //     // });
    //     // if (library.FullScanIntervalMinutes < 1)
    //     //     library.FullScanIntervalMinutes = 60;
    //     
    //     fields.Add(new ElementField
    //     {
    //         InputType = FormInputType.Int,
    //         Parameters = new Dictionary<string, object>
    //         {
    //             { "Min", 0 },
    //             { "Max", 300 }
    //         },
    //         Name = nameof(library.FileSizeDetectionInterval),
    //         // Conditions = new List<Condition>
    //         // {
    //         //     new (fieldScan, library.Scan, value: true)
    //         // }
    //     });
    //
    //     return fields;
    // }
}
