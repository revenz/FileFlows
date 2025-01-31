using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class Nodes : ListPage<Guid, ProcessingNode>
{

    public override async Task<bool> Edit(ProcessingNode node)
    {
        bool isServerProcessingNode = node.Address == FileFlowsServer;
        node.Mappings ??= new();
        this.EditingItem = node;
        if (node.ProcessingOrder == null)
            node.ProcessingOrder = (ProcessingOrder)1000;

        Dictionary<Guid, string> scripts;
        var tabs = new Dictionary<string, List<IFlowField>>();
        Blocker.Show();
        try
        {
            scripts = (await HttpHelper.Get<Dictionary<Guid, string>>("/api/script/basic-list?type=System")).Data ?? new ();
            tabs.Add("General", TabGeneral(node, isServerProcessingNode, scripts));
            tabs.Add("Schedule", TabSchedule(node, isServerProcessingNode));
            if (isServerProcessingNode == false)
                tabs.Add("Mappings", TabMappings(node));
            tabs.Add("Processing", await TabProcessing(node));
            if (node.OperatingSystem != OperatingSystemType.Windows)
                tabs.Add("Advanced", TabAdvanced(node));
            tabs.Add("Variables", TabVariables(node));
        }
        finally
        {
            Blocker.Hide();
        }

        await Editor.Open(new()
        {
            TypeName = "Pages.ProcessingNode", Title = "Pages.ProcessingNode.Title", Model = node, Tabs = tabs,
            Large = true,
            SaveCallback = Save, HelpUrl = "https://fileflows.com/docs/webconsole/configuration/nodes",
        });
        return false;
    }

    private List<IFlowField> TabGeneral(ProcessingNode node, bool isServerProcessingNode, Dictionary<Guid, string> scripts)
    {
        List<IFlowField> fields = new ();

        if (isServerProcessingNode)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Label,
                Name = "InternalProcessingNodeDescription"
            });
        }
        else
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(node.Name),
                Validators = new List<Validator> {
                    new Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(node.Address),
                Validators = new List<Validator> {
                    new Required()
                }
            });
        }

        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(node.Enabled)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.FlowRunners),
            Validators = new List<Validator> {
                new FileFlows.Validators.Range() { Minimum = 0, Maximum = 100 } // 100 is insane but meh, let them be insane 
            },
            Parameters = new()
            {
                { "Min", 0 },
                { "Max", 100 }
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.Priority),
            Validators = new List<Validator> {
                new FileFlows.Validators.Range() { Minimum = 0, Maximum = 100 }
            },
            Parameters = new()
            {
                { "Min", 0 },
                { "Max", 100 }
            }
        });
        fields.Add(new ElementField
        {
            InputType = isServerProcessingNode ? FormInputType.Folder : FormInputType.Text,
            Name = nameof(node.TempPath),
            Validators = new List<Validator> {
                new Required()
            }
        });

        if (Profile.LicensedFor(LicenseFlags.Tasks))
        {
            var scriptOptions = scripts.Select(x => new ListOption
            {
                Value = x.Key, Label = x.Value
            }).ToList();
            scriptOptions.Insert(0, new ListOption() { Label = "Labels.None", Value = Guid.Empty});
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(node.PreExecuteScript),
                Parameters = new Dictionary<string, object>
                {
                    { "AllowClear", false},
                    { "Options", scriptOptions }
                }
            });
        }
        
        fields.Add(new ElementField()
        {
            Name = nameof(node.Icon),
            InputType = FormInputType.IconPicker,
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputIconPicker.IncludeSvgs), true},
                { nameof(InputIconPicker.AllowClear), true},
            }
        });

        return fields;
    }

    private List<IFlowField> TabSchedule(ProcessingNode node, bool isServerProcessingNode)
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
            Name = nameof(node.Schedule),
            Parameters = new Dictionary<string, object>
            {
                { "HideLabel", true }
            }
        });
        return fields;

    }
    private List<IFlowField> TabMappings(ProcessingNode node)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Label,
            Name = "MappingsDescription"
        });
        var efMappings = new ElementField
        {
            InputType = FormInputType.KeyValue,
            Name = nameof(node.Mappings),
            Parameters = new()
            {
                { "HideLabel", true }
            }
        };
        var otherNodes = this.Data.Where(x => x.Uid != node.Uid && x.Mappings?.Any() == true).ToList();
        if (otherNodes.Any() == true)
        {
            var onClickCallback = EventCallback.Factory.Create(this, () => { _ = CopyMappingsDialog(node, otherNodes, efMappings); });
            fields.Add(new ElementField()
            {
                Name = "CopyMappings",
                HideLabel = true,
                InputType = FormInputType.Button,
                Parameters = new()
                {
                    { nameof(InputButton.OnClick), onClickCallback }
                }
            });
        }

        fields.Add(efMappings);
        return fields;
    }

    private async Task CopyMappingsDialog(ProcessingNode node, List<ProcessingNode> otherNodes, ElementField efMappings)
    {
        ProcessingNode source = await SelectDialog.Show("Copy Mappings", "Select a Node to copy mappings from", otherNodes.Select(
            x => new ListOption()
            {
                Value = x,
                Label = x.Name
            }).ToList(), otherNodes.First());
        if (source == null)
            return; // was canceled
        if (node.Mappings == null)
            node.Mappings = new();
        
        
        foreach (var mapping in source.Mappings ?? new())
        {
            if (node.Mappings.Any(x => x.Key == mapping.Key))
                continue;
            Logger.Instance.ILog("Adding Mapping: " + mapping.Key);
            node.Mappings.Add(new(mapping.Key, mapping.Value));
        }
        Logger.Instance.ILog("Mappings: " + JsonSerializer.Serialize(node.Mappings));
        efMappings.InvokeValueChanged(this, node.Mappings);
    }
    
    
    private async Task<List<IFlowField>> TabProcessing(ProcessingNode node)
    {
        var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>("/api/library/basic-list");
        var libraries = librariesResult?.Data?.Select(x => new ListOption
        {
            Label = x.Value,
            Value = new ObjectReference
            {
                Uid = x.Key,
                Name = x.Value,
                Type = typeof(Library)?.FullName ?? string.Empty
            }
        })?.OrderBy(x => x.Label)?.ToList() ?? new List<ListOption>();
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Label,
            Name = "ProcessingDescription"
        });
        var efAllLibraries = new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(node.AllLibraries),
            Parameters = new Dictionary<string, object>
            {
                { 
                    nameof(InputChecklist.Options), 
                    new List<ListOption>
                    {
                        new() { Label = "All", Value = ProcessingLibraries.All },
                        new() { Label = "Only", Value = ProcessingLibraries.Only },
                        new() { Label = "All Except", Value = ProcessingLibraries.AllExcept },
                    } 
                }
            }
        };
        fields.Add(efAllLibraries);
        fields.Add(new ElementField
        {
            InputType = FormInputType.Checklist,
            Name = nameof(node.Libraries),
            Parameters = new()
            {
                { nameof(InputChecklist.Options), libraries }
            },
            Conditions = new List<Condition>
            {
                new Condition(efAllLibraries, node.AllLibraries, value: ProcessingLibraries.All, isNot: true)
            }
        });
        
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.MaxFileSizeMb)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.ProcessFileCheckInterval)
        });

        if(Profile.LicensedFor(LicenseFlags.ProcessingOrder))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(ProcessingNode.ProcessingOrder),
                Parameters = new Dictionary<string, object>{
                    { "Options", new List<ListOption> {
                        new () { Value = (ProcessingOrder)1000, Label = $"Labels.Default" },
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
        
        return fields;
    }

    private List<IFlowField> TabAdvanced(ProcessingNode node)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(node.DontChangeOwner),
            Parameters = new()
            {
                { "Inverse", true }
            }
        });
        var efDontSetPermissions = new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(node.DontSetPermissions),
            Parameters = new()
            {
                { "Inverse", true }
            }
        };
        fields.Add(efDontSetPermissions);

        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.PermissionsFiles),
            DisabledConditions = new List<Condition>
            {
                new (efDontSetPermissions,node.DontSetPermissions, value: false)
            },
            Placeholder = FileFlows.Common.Globals.DefaultPermissionsFile.ToString("D3"),
            Parameters = new ()
            {
                { nameof(InputNumber<int>.Max), 777 },
                { nameof(InputNumber<int>.ZeroAsEmpty), true }
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(node.PermissionsFolders),
            DisabledConditions = new List<Condition>
            {
                new (efDontSetPermissions,node.DontSetPermissions, value: false)
            },
            Placeholder = FileFlows.Common.Globals.DefaultPermissionsFolder.ToString("D3"),
            Parameters = new ()
            {
                { nameof(InputNumber<int>.Max), 777 },
                { nameof(InputNumber<int>.ZeroAsEmpty), true }
            }
        });
        return fields;
    }
    
    
    /// <summary>
    /// Adds the variables element fields
    /// </summary>
    /// <param name="node">the processing node</param>
    /// <returns>a list of element fields</returns>
    private List<IFlowField> TabVariables(ProcessingNode node)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Label,
            Name = "VariablesDescription"
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.KeyValue,
            Name = nameof(node.Variables),
            Parameters = new()
            {
                { "HideLabel", true }
            }
        });
        return fields;
    }
}