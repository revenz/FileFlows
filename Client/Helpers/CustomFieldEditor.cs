using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Plugin;

namespace FileFlows.Client.Helpers;

/// <summary>
/// A custom field editor
/// </summary>
/// <param name="editor">the editor to show this in</param>
public class CustomFieldEditor(Editor editor, Flow flow)
{
    
    /// <summary>
    /// Edits a field
    /// </summary>
    /// <param name="editingItem">Optional item being edited</param>
    /// <returns>the updated field, or null if canceled</returns>
    public async Task<CustomField> EditField(CustomField? editingItem = null, bool canChangeVariable = true)
    {
        var fields = new List<IFlowField>();
        fields.Add(new ElementField()
        {
            Name = nameof(CustomField.Name),
            InputType = FormInputType.Text
        });
        if (canChangeVariable)
        {
            fields.Add(new ElementField()
            {
                Name = nameof(CustomField.Variable),
                InputType = FormInputType.Text
            });
        }
        fields.Add(new ElementField()
        {
            Name = nameof(CustomField.Description),
            InputType = FormInputType.Text
        });
        var efType = new ElementField()
        {
            Name = nameof(CustomField.Type),
            InputType = FormInputType.Select,
            Parameters = new Dictionary<string, object>
            {
                { "AllowClear", false },
                {
                    "Options", Enum.GetValues(typeof(CustomFieldType))
                        .Cast<CustomFieldType>()
                        .Select(e => new ListOption
                        {
                            Value = e,
                            Label = $"Enums.{nameof(CustomFieldType)}.{e}"
                        }).ToList()
                }
            }
        };
        fields.Add(efType);
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });

        if (flow.Fields?.Any(x => x.Variable != editingItem?.Variable) ?? false)
        {
            var conFieldOptions = flow.Fields.Where(x => x.Variable != editingItem?.Variable)
                .Select(e => new ListOption
                {
                    Value = e.Variable,
                    Label = e.Name
                })
                .OrderBy(x => x.Label.ToLowerInvariant()).ToList();
            conFieldOptions.Insert(0, new ()
            {
                Label   = Translater.Instant("Labels.None"),
                Value =  string.Empty
            });

            var efConditionField = new ElementField()
            {
                Name = nameof(CustomField.ConditionField),
                InputType = FormInputType.Select,
                Parameters = new Dictionary<string, object>
                {
                    { "AllowClear", false },
                    {
                        "Options", conFieldOptions
                    }
                }
            }; 
            fields.Add(efConditionField);
            fields.Add(new ElementField()
            {
                Name = nameof(CustomField.ConditionValue),
                InputType = FormInputType.Text,
                Conditions = [
                    new (efConditionField, editingItem?.ConditionValue ?? string.Empty, value: "", isNot: true)
                ]
            });
            fields.Add(new ElementField()
            {
                InputType = FormInputType.HorizontalRule
            });
        }


        fields.Add(new ElementField()
        {
            Name = "Minimum",
            InputType = FormInputType.Int,
            Conditions = [
                new AnyCondition(efType, editingItem?.Type ?? CustomFieldType.Text, new []
                {
                    CustomFieldType.Integer, CustomFieldType.Slider
                })
            ]
        });
        fields.Add(new ElementField()
        {
            Name = "Maximum",
            InputType = FormInputType.Int,
            Conditions = [
                new AnyCondition(efType, editingItem?.Type ?? CustomFieldType.Text, new []
                {
                    CustomFieldType.Integer, CustomFieldType.Slider
                })
            ]
        });
        fields.Add(new ElementField()
        {
            Name = "DefaultNumber",
            InputType = FormInputType.Int,
            Conditions = [
                new AnyCondition(efType, editingItem?.Type ?? CustomFieldType.Text, new []
                {
                    CustomFieldType.Integer, CustomFieldType.Slider
                })
            ]
        });
        fields.Add(new ElementField()
        {
            Name = "Options",
            InputType = FormInputType.KeyValue,
            Conditions = [
                new AnyCondition(efType, editingItem?.Type ?? CustomFieldType.Text, new []
                {
                    CustomFieldType.OptionGroup, CustomFieldType.Select
                })
            ]
        });
        
        
        var result = await editor.Open(new()
        {
            TypeName = "Pages.Resellers.Flows", Title = "Pages.Resellers.Flows.Single", Model = GetEditingModel(editingItem),
            Fields = fields, SaveCallback = (ExpandoObject model) =>
            {
                var saving = ExpandoToCustomField(model);
                if (IsUnique(saving, editingItem) == false)
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
        });
        if (result.Success == false)
            return null;

        return ExpandoToCustomField(result.Model);
    }
    

    /// <summary>
    /// Checks if the field is unique
    /// </summary>
    /// <param name="field">the field to check</param>
    /// <param name="editing">an optional field to ignore, ie a field being edited</param>
    /// <returns>true if unique, otherwise false</returns>
    private bool IsUnique(CustomField field, CustomField? editing)
    {
        if (flow.Fields == null || flow.Fields.Count == 0)
            return true;
        return flow.Fields.Any(x =>
        {
            if (x == editing)
                return false;
            if (x.Name.Equals(field.Name, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (x.Variable.Equals(field.Variable, StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }) == false;
    }

    
    private CustomField ExpandoToCustomField(ExpandoObject expandoObject)
    {
        try
        {
            var dict = expandoObject as IDictionary<string, object>;
            var cf = new CustomField();
            if(dict.TryGetValue(nameof(cf.Name), out var name))
                cf.Name = name?.ToString()?.Trim() ?? string.Empty;
            if(dict.TryGetValue(nameof(cf.Description), out var desc))
                cf.Description = desc?.ToString()?.Trim() ?? string.Empty;
            if(dict.TryGetValue(nameof(cf.Variable), out var variable))
                cf.Variable = variable?.ToString()?.Trim() ?? string.Empty;
            if (dict.TryGetValue(nameof(cf.ConditionField), out var conditionField))
            {
                cf.ConditionField = conditionField?.ToString()?.Trim() ?? string.Empty;
                if (dict.TryGetValue(nameof(cf.ConditionValue), out var conditionValue))
                    cf.ConditionValue = conditionValue?.ToString()?.Trim() ?? string.Empty;
            }

            if(dict.TryGetValue(nameof(cf.Type), out var type) && type is CustomFieldType cft)
                cf.Type = cft;

            if (cf.Type is CustomFieldType.Integer or CustomFieldType.Slider)
            {
                if(dict.TryGetValue("Minimum", out var min) && int.TryParse(min?.ToString(), out var minInt))
                    cf.Data["Minimum"] = minInt;
                if(dict.TryGetValue("Maximum", out var max) && int.TryParse(max?.ToString(), out var maxInt))
                    cf.Data["Maximum"] = maxInt;
                if (dict.TryGetValue("DefaultNumber", out var defaultNumber) &&
                    int.TryParse(defaultNumber?.ToString(), out var defaultInt))
                    cf.Data["Default"] = defaultInt;
            }
            else if (cf.Type is CustomFieldType.Select or CustomFieldType.OptionGroup)
            {
                if (dict.TryGetValue("Options", out var options) &&
                    options is List<KeyValuePair<string, string>> listOptions)
                {
                    cf.Data["Options"] = listOptions;
                }
            }
            
            return cf;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog(ex.Message);
            return null;
        }
    }
    
    private object GetEditingModel(CustomField? editingItem)
    {
        dynamic editingModel = new ExpandoObject();
        editingModel.Type = editingItem?.Type ?? CustomFieldType.Text;
        editingModel.Variable = editingItem?.Variable ?? string.Empty;
        editingModel.Description = editingItem?.Description ?? string.Empty;
        editingModel.Name = editingItem?.Name ?? string.Empty;
        editingModel.ConditionField = editingItem?.ConditionField ?? string.Empty;
        editingModel.ConditionValue = editingItem?.ConditionValue ?? string.Empty;
        if (editingItem != null && editingItem.Data?.Any() == true)
        {
            switch (editingItem.Type)
            {
                case CustomFieldType.Integer:
                case CustomFieldType.Slider:
                    if (editingItem.Data.TryGetValue("Minimum", out var minimum) && int.TryParse(minimum.ToString(), out var minimumNumber))
                        editingModel.Minimum = minimumNumber;
                    if (editingItem.Data.TryGetValue("Maximum", out var maximum) && int.TryParse(maximum.ToString(), out var maximumNumber))
                        editingModel.Maximum = maximumNumber;
                    if (editingItem.Data.TryGetValue("Default", out var defaultObj) && int.TryParse(defaultObj.ToString(), out var defaultNumber))
                        editingModel.DefaultNumber = defaultNumber;
                        break;
                case CustomFieldType.Select:
                case CustomFieldType.OptionGroup:
                    if (editingItem.Data.TryGetValue("Options", out var options))
                    {   
                        if(options is List<KeyValuePair<string, string>> kvp)
                            editingModel.Options = kvp;
                        else if (options is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var list = jsonElement.EnumerateArray()
                                    .Select(e => new KeyValuePair<string, string>(
                                        e.GetProperty("Key").GetString() ?? string.Empty, 
                                        e.GetProperty("Value").GetString() ?? string.Empty))
                                    .ToList();

                                editingModel.Options = list;
                            }
                            catch (Exception)
                            {
                                // Handle error (log it, rethrow, etc.)
                            }
                        }
                        
                    }
                    break;
            }
        }
        return editingModel;
    }

}