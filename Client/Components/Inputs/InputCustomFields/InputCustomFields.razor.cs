using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Custom fields control
/// </summary>
public partial class InputCustomFields : Input<List<CustomField>>
{

    /// <summary>
    /// Adds a field
    /// </summary>
    private void AddField()
    {
        this.Value ??= new();
        this.Value.Add(new SelectCustomField());
    }
}