using System.Text.Json;
using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Custom fields control
/// </summary>
public partial class InputCustomFields : Input<List<CustomField>>
{
    /// <summary>
    /// The custom field editor
    /// </summary>
    private Editor CustomFieldEditor;

    /// <summary>
    /// Gets or sets the table instance
    /// </summary>
    protected FlowTable<CustomField> Table { get; set; }
    
    /// <summary>
    /// Adds a field
    /// </summary>
    private async Task AddField()
    {
        var added = await EditField(new()
        {

        });
        if (added != null)
        {
            this.Value = (Value ?? []).Union([added]).ToList();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Edits an item
    /// </summary>
    public async Task Edit()
    {
        var items = Table?.GetSelected()?.ToList();
        if (items?.Any() != true)
            return;
        var selected = items.First();
        if (selected == null)
            return;
        
        var item = Value.FirstOrDefault(x => x.Name == selected.Name);
        if (item == null)
            return;
        
        var updated = await EditField(new ()
        {
            Name = item.Name,
            Data = item.Data.ToDictionary(x => x.Key, x => x.Value),
            Type = item.Type,
            Description = item.Description,
            Variable = item.Variable
        }, item);
        if (updated == null)
            return; // didnt change
        item.Name = updated.Name;
        item.Data = updated.Data;
        item.Type = updated.Type;
        item.Description = updated.Description;
        item.Variable = updated.Variable;
        StateHasChanged();
    }

    /// <summary>
    /// Checks if the field is unique
    /// </summary>
    /// <param name="field">the field to check</param>
    /// <param name="editing">an optional field to ignore, ie a field being edited</param>
    /// <returns>true if unique, otherwise false</returns>
    private bool IsUnique(CustomField field, CustomField? editing)
    {
        if (Value == null || Value.Count == 0)
            return true;
        return Value.Any(x =>
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

    /// <summary>
    /// Edits a field
    /// </summary>
    /// <param name="field">the field to edit</param>
    /// <param name="editingItem">Optional item being edited</param>
    /// <returns>the updated field, or null if canceled</returns>
    private async Task<CustomField> EditField(CustomField field, CustomField? editingItem = null)
    {
        var fields = new List<IFlowField>();
        fields.Add(new ElementField()
        {
            Name = nameof(field.Name),
            InputType = FormInputType.Text
        });
        fields.Add(new ElementField()
        {
            Name = nameof(field.Variable),
            InputType = FormInputType.Text
        });
        fields.Add(new ElementField()
        {
            Name = nameof(field.Description),
            InputType = FormInputType.Text
        });
        fields.Add(new ElementField()
        {
            Name = nameof(field.Type),
            InputType = FormInputType.Select,
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", Enum.GetValues(typeof(CustomFieldType))
                    .Cast<CustomFieldType>()
                    .Select(e => new ListOption
                    {
                        Value = e,
                        Label = $"Enums.{nameof(CustomFieldType)}.{e}"
                    }).ToList()
                }
            }
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        
        var result = await CustomFieldEditor.Open(new()
        {
            TypeName = "Pages.Resellers.Flows", Title = "Pages.Resellers.Flows.Single", Model = field,
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

    private CustomField ExpandoToCustomField(ExpandoObject expandoObject)
    {
        try
        {
            string json = JsonSerializer.Serialize(expandoObject);
            var cf = JsonSerializer.Deserialize<CustomField>(json);
            cf.Name = cf.Name?.Trim() ?? string.Empty;
            cf.Variable = cf.Variable?.Trim() ?? string.Empty;
            cf.Description = cf.Description?.Trim() ?? string.Empty;
            return cf;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog(ex.Message);
            return null;
        }
    }
    
    private void Delete()
    {
        var items = Table.GetSelected().ToList();
        if (items.Count == 0)
            return; // nothing to delete
        Value = Value.Where(x => items.Contains(x) == false).ToList();
    }


    /// <summary>
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    void MoveUp() => Move(true);

    /// <summary>
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    void MoveDown() => Move(false);

    /// <summary>
    /// Moves the items up or down
    /// </summary>
    /// <param name="up">if moving up</param>
    void Move(bool up)
    {
        var fields = Table.GetSelected().ToList();
        if (fields.Count == 0)
            return; // nothing to move

        if (Value.Count <= 1)
            return; // nothing to sort

        
        var newValue = Value.ToList(); // Create a copy of the list to modify
        var sortedFields = fields.OrderBy(f => newValue.IndexOf(f)).ToList();
        if (!up)
            sortedFields.Reverse(); // Reverse order if moving down

        foreach (var field in sortedFields)
        {
            int index = newValue.IndexOf(field);
            if (index == -1 || (up && index == 0) || (!up && index == newValue.Count - 1))
                continue; // Skip if at the boundary

            int swapIndex = up ? index - 1 : index + 1;
            (newValue[index], newValue[swapIndex]) = (newValue[swapIndex], newValue[index]); // Swap elements
        }

        Value = newValue; // Assign the updated list

    }

}