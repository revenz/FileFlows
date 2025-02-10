using System.Text.Json;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Helpers;
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
        var customFieldEditor = new CustomFieldEditor(CustomFieldEditor, new Flow()
        {
            Fields = Value
        });
        var added = await customFieldEditor.EditField();
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
        
        var customFieldEditor = new CustomFieldEditor(CustomFieldEditor, new Flow()
        {
            Fields = Value
        });
        var updated = await customFieldEditor.EditField(item);
        if (updated == null)
            return; // didnt change
        item.Name = updated.Name;
        item.Data = updated.Data;
        item.Type = updated.Type;
        item.Description = updated.Description;
        item.Variable = updated.Variable;
        item.ConditionField = updated.ConditionField;
        item.ConditionValue = updated.ConditionValue;
        StateHasChanged();
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