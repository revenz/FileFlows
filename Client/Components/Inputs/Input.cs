using System.Text.Json;
using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NPoco;

namespace FileFlows.Client.Components.Inputs;

public delegate void DisabledChangeEvent(bool state);
public interface IInput
{
    string Label { get; set; }
    string Help { get; set; }
    string Placeholder { get; set; }
    string ErrorMessage { get; set; }
    bool HideLabel { get; set; }
    bool Disabled { get; set; }
    bool Visible { get; set; }
    string? CustomXID { get; set; }

    void Dispose();

    EventCallback OnSubmit { get; set; }

    FileFlows.Shared.Models.ElementField Field { get; set; }

    Task<bool> Validate();

    EventHandler<bool> ValidStateChanged { get; set; }

    bool Focus();
}

public abstract class Input<T> : ComponentBase, IInput, IDisposable
{
    [CascadingParameter] protected InputRegister InputRegister { get; set; }
    [CascadingParameter] protected Editor Editor { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }

    private Guid UidasGuid = Guid.NewGuid();
    protected string Uid => UidasGuid.ToString();
    private string _Label;
    private string _LabelOriginal;
    private string _Help;
    public EventHandler<bool> ValidStateChanged { get; set; }

    /// <summary>
    /// Gets or sets the suffix
    /// </summary>
    public string Suffix { get; set; }
    /// <summary>
    /// Gets or sets the prefix
    /// </summary>
    public string Prefix { get; set; }

    protected string LabelOriginal => _LabelOriginal;
    
    /// <summary>
    /// Gets or set the custom x-id
    /// </summary>
    [Parameter] public string? CustomXID { get; set; }

    /// <summary>
    /// Gets or sets the on submit event callback
    /// </summary>
    [Parameter] public EventCallback OnSubmit { get; set; }
    /// <summary>
    /// Gets or sets the on close event callback
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or ses if the label should be hidden
    /// </summary>
    [Parameter] public bool HideLabel { get; set; }

#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets label for the Input
    /// </summary>
    [Parameter]
    public string Label
    {
        get => _Label;
        set
        {
            if (Disposed) return;
            if (_LabelOriginal == value)
                return;
            _LabelOriginal = value;
            if (_LabelOriginal.StartsWith("Flow.Parts.Script.Fields."))
            {
                // special case, thee dont have translations
                _LabelOriginal = _LabelOriginal["Flow.Parts.Script.Fields.".Length..];
                _Label = FlowHelper.FormatLabel(_LabelOriginal);
            }
            else if (Translater.NeedsTranslating(_LabelOriginal))
            {
                _Label = Translater.Instant(_LabelOriginal);
                if(string.IsNullOrEmpty(_Help))
                    _Help = Translater.Instant(_LabelOriginal + "-Help");
                Suffix = Translater.Instant(_LabelOriginal + "-Suffix");
                Prefix = Translater.Instant(_LabelOriginal + "-Prefix");
                Placeholder = Translater.Instant(_LabelOriginal + "-Placeholder").EmptyAsNull() ?? _Label;
            }
            else
            {
                _Label = value;
            }
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets if this is read only
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets if this is disabled
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets if this is visible
    /// Don't make this a Parameter, it breaks stuff
    /// </summary>
    public bool Visible { get; set; }

    private ElementField _Field;
    
#pragma warning disable BL0007

    /// <summary>
    /// Gets or sets the element field bound to this
    /// </summary>
    [Parameter] public ElementField Field
    {
        get => _Field;
        set
        {
            if (_Field == value)
                return;
            if (_Field != null && value != null)
                return;  // field is already set and wired up, if we change the instance it will break the conditions.  this should be changing its blazor doing the changing
            _Field = value;
        }
    }

    /// <summary>
    /// Gets or sets the Help text for this 
    /// </summary>
    [Parameter] public string Help { get => _Help; set { if (string.IsNullOrEmpty(value) == false) _Help = value; } }
    
#pragma warning restore BL0007
    
    /// <summary>
    /// Gets or sets the placeholder text
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the validators text
    /// </summary>
    [Parameter] public List<Validator> Validators { get; set; }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets the text to show as the placeholder
    /// </summary>
    /// <returns>the text to show as the placeholder</returns>
    protected string GetPlaceholder()
    {
        if (string.IsNullOrEmpty(this.Placeholder) == false)
            return this.Placeholder;
        if (this.HideLabel || this.Field?.HideLabel == true)
            return this.Label;
        return string.Empty;
    }

    protected T _Value;
    private bool _ValueUpdating = false;
    /// <summary>
    /// Gets if the value is currently updating
    /// </summary>
    protected bool ValueIsUpdating => _ValueUpdating;
    
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter]
    public T Value
    {
        get => _Value;
        set
        {
            try
            {
                if (Disposed) return;
                _ValueUpdating = true;

                if (_Value == null && value == null)
                    return;

                if (_Value != null && value != null && _Value.Equals(value)) return;

                bool areEqual = System.Text.Json.JsonSerializer.Serialize(_Value) ==
                                System.Text.Json.JsonSerializer.Serialize(value);
                if (areEqual ==
                    false) // for lists/arrays if they haven't really changed, empty to empty, dont clear validation
                    ErrorMessage = ""; // clear the error

                _Value = value;
                
                if (Editor?.Loaded != true)
                    return;
                ValueUpdated();
                ValueChanged.InvokeAsync(value);
                Field?.InvokeValueChanged(this.Editor, value);

                if (this.Field?.ChangeValues?.Any() == true)
                {
                    foreach (var cv in this.Field.ChangeValues)
                    {
                        if (cv.Matches(value))
                        {
                            if (cv.Field == null)
                            {
                                var field = Editor?.FindField(cv.Property);
                                if(field != null)
                                    cv.Field = field;
                            }

                            if (cv.Field != null)
                            {
                                cv.Field.InvokeValueChanged(this, cv.Value);
                            }
                        }
                    }
                }

                if (areEqual == false)
                    _ = OnChangedValue.InvokeAsync(value);
            }
            finally
            {
                _ValueUpdating = false;
            }
        }
    }
#pragma warning restore BL0007

    protected virtual void ValueUpdated() { }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }
    /// <summary>
    /// Gets or sets the On changed event, this is similar to the ValueChanged but can be used with the bound value
    /// Only use this if you want to subscribe to events when the value changes but not update the bound model
    /// </summary>
    [Parameter] public EventCallback<T> OnChangedValue { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if(this.Field != null)
            InputRegister.RegisterInput(this.Field.Uid, this);
        else if (InputRegister != null)
            InputRegister.RegisterInput(this.UidasGuid, this);
        this.Visible = true;

        if (this.Field != null)
        {
            this.Field.DisabledChange += Field_DisabledChange;

            if (this.Field.DisabledConditions?.Any() == true)
            {
                bool disabled = false;
                foreach (var condition in this.Field.DisabledConditions)
                    disabled |= condition.IsMatch;
                this.Disabled = disabled;
            }

            this.Field.ConditionsChange += Field_ConditionsChange;
            if (this.Field.Conditions?.Any() == true)
            {
                bool visible = true;
                foreach (var condition in this.Field.Conditions)
                    visible &= condition.IsMatch == false; // conditions IsMatch stores the inverse for Disabled states, for condtions we want the inverse
                this.Visible = visible;
            }
            
            this.Field.ValueChanged += FieldOnValueChanged;

            if (string.IsNullOrEmpty(Field.Description) == false)
                this.Help = Field.Description;
        }
    }

    /// <summary>
    /// Called when the field value changes
    /// </summary>
    /// <param name="sender">the sender of the event</param>
    /// <param name="value">the new value</param>
    protected virtual void FieldOnValueChanged(object sender, object value)
    {
        if (Disposed) return;
        if (_ValueUpdating)
            return;
        if (value is JsonElement je)
        {
            if (typeof(T) == typeof(int))
                value = je.GetInt32();
            else if (typeof(T) == typeof(string))
                value = je.GetString() ?? string.Empty;
            else if (typeof(T) == typeof(bool))
                value = je.GetBoolean();
        }

        try
        {
            this.Value = (T)value;
        }
        catch (InvalidCastException)
        {
            Logger.Instance.ILog($"Could not cast '{value.GetType().FullName}' to '{typeof(T).FullName}'");
        }
    }

    private void Field_DisabledChange(bool state)
    {
        if (Disposed) return;
        if(this.Disabled != state)
        {
            this.Disabled = state;
            this.StateHasChanged();
        }
    }

    private void Field_ConditionsChange(bool state)
    {
        if (Disposed) return;
        if (this.Field.Conditions.Count > 1)
        {
            state = true;
            foreach (var condition in this.Field.Conditions)
            {
                state &= condition.IsMatch == false; // is false since condition was for disabled state
            }
        }
        if(this.Visible != state)
        {
            this.Visible = state;
            VisibleChanged(state);
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Called when the visibility changed
    /// </summary>
    /// <param name="visible">the new visibility</param>
    protected virtual void VisibleChanged(bool visible)
    {
    }

    protected void ClearError() => this.ErrorMessage = "";

    public virtual async Task<bool> Validate()
    {
        if (Disposed) return true;
        if (this.Validators?.Any() != true)
            return true;
        if (this.Visible == false)
            return true;

        bool isValid = string.IsNullOrEmpty(ErrorMessage);
        foreach (var val in this.Validators)
        {
            var validResult = await val.Validate(this.Value);
            if (validResult.Valid == false)
            {
                ErrorMessage = validResult.Error?.EmptyAsNull() ?? Translater.Instant($"Validators.{val.Type}", val);
                this.StateHasChanged();
                if (isValid)
                    ValidStateChanged?.Invoke(this, false);
                Logger.Instance.DLog($"Invalid '{this.Label}' validator: " + val.GetType().FullName);
                return false;
            }
        }
        if(isValid == false)
            ValidStateChanged?.Invoke(this, true);
        return true;
    }

    public virtual bool Focus() => false;

    protected bool FocusUid()
    {
        if (Disposed) return false;
        _ = jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        return true;
    }

    private bool Disposed = false;
    public virtual void Dispose()
    {
        Disposed = true;
        if (this.Field != null)
        {
            this.Field.ConditionsChange -= Field_ConditionsChange;
            this.Field.DisabledChange -= Field_DisabledChange;
            this.Field.ValueChanged -= FieldOnValueChanged;
            this.Field = null;
        }
    }
}