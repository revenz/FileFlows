using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input multi-select
/// </summary>
public partial class InputMultiSelect: Input<List<object>>
{
    /// <summary>
    /// Gets or sets the options of the checklist
    /// </summary>
    [Parameter] public List<ListOption> Options { get; set; }
    
    /// <summary>
    /// Gets or sets if this allows any or all
    /// </summary>
    [Parameter] public bool AnyOrAll { get; set; }
    
    /// <summary>
    /// Gets or sets the any label string
    /// </summary>
    [Parameter] public string LabelAny { get; set; }

    /// <summary>
    /// If the dropdown is opened
    /// </summary>
    private bool dropdownOpen = false;
    
    /// <summary>
    /// The dotnet reference to this component
    /// </summary>
    private DotNetObjectReference<InputMultiSelect> dotnetObjRef;

    /// <summary>
    /// Label for all
    /// </summary>
    private string lblAll, lblAny;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblAll = Translater.Instant("Labels.All");
        lblAny =LabelAny?.EmptyAsNull() ??  Translater.Instant("Labels.Any");
        if (Value == null)
            Value = new List<object>();
        else if(Options != null)
        {
            var allowed  = this.Value.Select(x =>
            {
                foreach (var opt in this.Options)
                {
                    if (opt.Value == x)
                        return x;
                    if (opt.Value is string && x is string)
                        continue;
                    if (x == null)
                        continue;
                    if (x.GetType().IsPrimitive)
                        continue;
                    if (opt.Value == null)
                        continue;
                    string xJson = System.Text.Json.JsonSerializer.Serialize(x);
                    string optJson = System.Text.Json.JsonSerializer.Serialize(opt.Value);
                    if (xJson == optJson)
                        return opt.Value;
                }
                return x;
            }).ToList();
            
            this.Value.Clear();
            if(allowed.Count > 0)
                this.Value.AddRange(allowed!);
        }
    }
    
    /// <inheritdoc />
    public override Task<bool> Validate()
    {
        if (Value.Any() == true)
            return Task.FromResult(true);
        ErrorMessage = Translater.Instant("Validators.Required");
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            dotnetObjRef = DotNetObjectReference.Create(this);
            _ = jsRuntime.InvokeVoidAsync("ff.handleClickOutside", [Uid, dotnetObjRef]);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        if (dotnetObjRef != null)
        {
            _ = jsRuntime.InvokeVoidAsync("ff.removeClickHandler", Uid);
            dotnetObjRef.Dispose();
        }
    }

    /// <summary>
    /// Handles when the user clicked outside this component
    /// </summary>
    [JSInvokable]
    public void OnOutsideClick()
    {
        if (dropdownOpen == false)
            return;
        
        dropdownOpen = false;
        StateHasChanged();
    }
    
    /// <summary>
    /// Gets if all are selected
    /// </summary>
    private bool IsAllSelected => Value?.Count(x => x != null) == Options.Count;

    /// <summary>
    /// Gets if any are selected
    /// </summary>
    private bool IsAnySelected => Value is [null];
    
    /// <summary>
    /// Toggles the dropdown open state 
    /// </summary>
    private void ToggleDropdown()
    {
        dropdownOpen = !dropdownOpen;
    }

    /// <summary>
    /// Toggles if all is clicked
    /// </summary>
    private void ToggleAll()
    {
        if (IsAllSelected)
        {
            Value.Clear();
        }
        else
        {
            Value.Clear();
            foreach (var opt in Options)
                Value.Add(opt.Value);
        }
        ValueUpdated();
    }

    /// <summary>
    /// Toggles if all is clicked
    /// </summary>
    private void ToggleAny()
    {
        if (IsAnySelected)
        {
            Value.Clear();
        }
        else
        {
            Value.Clear();
            Value.Add(null);
        }
        ValueUpdated();
    }

    /// <summary>
    /// Toggles a option on or off
    /// </summary>
    /// <param name="optionValue">the option value</param>
    private void ToggleSelection(object optionValue)
    {
        if (Value.Contains(optionValue))
        {
            Value.Remove(optionValue);
        }
        else
        {
            Value.Remove(null);
            Value.Add(optionValue);
        }

        ValueUpdated();
    }

    private bool updating = false;

    /// <inheriddoc />
    protected override void ValueUpdated()
    {
        if (updating)
            return;
        updating = true;
        // ensures the value is updated
        ValueUpdated();
        ValueChanged.InvokeAsync(Value);
        Field?.InvokeValueChanged(this.Editor, Value);
        updating = false;
    }

    /// <summary>
    /// Gets the selected label
    /// </summary>
    /// <returns></returns>
    private string SelectedLabel()
    {
        if (Value == null || Value.Count == 0)
            return Translater.Instant("Labels.Select") + "...";
        if (Value is [null])
            return lblAny;
        if (Value.Count == Options.Count)
            return lblAll;
        if (Value.Count == 1)
        {
            var firstValue = Value.First();
            return Options.First(o => o.Value == firstValue || firstValue.Equals(o.Value)).Label;
        }

        return Translater.Instant("Labels.SelectedNum", new { num = Value.Count });
    }
}