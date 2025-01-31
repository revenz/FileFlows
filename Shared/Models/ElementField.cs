namespace FileFlows.Shared.Models;

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json.Serialization;
using FileFlows.Plugin;

/// <summary>
/// An element field is a UI component that is displayed in the web browser
/// </summary>
public class ElementField : IFlowField
{
    /// <summary>
    /// Gets or sets a value that makes this field read-only
    /// Special case, used by the flow editor to show the UID of flow elements
    /// </summary>
    public object ReadOnlyValue { get; set; }
    
    /// <summary>
    /// Gets or sets a value that can be copied to the clipboard for this field
    /// If set, a copy icon will be shown next to the label
    /// </summary>
    public string CopyValue { get; set; }
    
    /// <summary>
    /// A unique identifier for this field
    /// </summary>
    public readonly Guid Uid = Guid.NewGuid(); 
    
    /// <summary>
    /// Gets or sets the order of which to display this filed
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets the type of this field 
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the name of this field
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional label that should be used
    /// If set, this field won't be translated
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets optional help text to show
    /// If set, then translated help will not be looked for
    /// </summary>
    public string HelpText { get; set; }

    /// <summary>
    /// Gets or sets optional place holder text, this can be a translation key
    /// </summary>
    public string Placeholder { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the element, if this is set, this will be used instead of the HelpHint
    /// Note: this is used by the Script which the user defines the description for
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets if this field should flex-grow to fill the remaining content
    /// </summary>
    public bool FlexGrow { get; set; }
    
    /// <summary>
    /// Gets or sets if this should hide the label
    /// </summary>
    public bool HideLabel { get; set; }
    
    /// <summary>
    /// Gets or sets an optional column span to use if this is used inside a Panel
    /// </summary>
    public int? ColSpan { get; set; }
    
    /// <summary>
    /// Gets or sets an optional row span to use if this is used inside a Panel
    /// </summary>
    public int? RowSpan { get; set; }

    /// <summary>
    /// Gets or sets the input type of this field
    /// </summary>
    public FormInputType InputType { get; set; }

    /// <summary>
    /// Gets or sets if this field is only only a UI field
    /// and value will not be saved
    /// </summary>
    public bool UiOnly { get; set; }

    /// <summary>
    /// Gets or sets the variables {} available to this field
    /// </summary>
    public Dictionary<string, object> Variables { get; set; }

    /// <summary>
    /// Gets or sets the parameters of the field
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }

    /// <summary>
    /// Gets a list of change values for this field
    /// </summary>
    public List<ChangeValue> ChangeValues { get; set; }

    /// <summary>
    /// Gets or sets the validators for the field
    /// </summary>
    public List<FileFlows.Validators.Validator> Validators { get; set; }

    /// <summary>
    /// A delegate used when a value change event
    /// </summary>
    public delegate void ValueChangedEvent(object sender, object value);
    /// <summary>
    /// A event that is raised when the value changes
    /// </summary>
    public event ValueChangedEvent ValueChanged;

    /// <summary>
    /// A delegate used for the disabled change event
    /// </summary>
    public delegate void DisabledChangeEvent(bool state);
    /// <summary>
    /// An event that is raised when the disable state of the field is changed
    /// </summary>
    public event DisabledChangeEvent DisabledChange;

    /// <summary>
    /// A delegate for when the conditions of the field changes
    /// </summary>
    public delegate void ConditionsChangeEvent(bool state);
    /// <summary>
    /// An event that is raised when the conditions of the field changes
    /// </summary>
    public event ConditionsChangeEvent ConditionsChange;

    /// <summary>
    /// Invokes the value changed event
    /// </summary>
    /// <param name="sender">The sender of the invoker</param>
    /// <param name="value">The value to invoke</param>
    public void InvokeValueChanged(object sender, object value) => this.ValueChanged?.Invoke(sender, value);

    private List<Condition> _DisabledConditions;
    /// <summary>
    /// Gets or sets the conditions used to disable this field
    /// </summary>
    public List<Condition> DisabledConditions
    {
        get => _DisabledConditions;
        set
        {
            _DisabledConditions = value ?? new List<Condition>();
            foreach (var condition in _DisabledConditions)
                condition.Owner = this;
        }
    }

    private List<Condition> _Conditions;
    /// <summary>
    /// Gets or sets the conditions used to show this field
    /// </summary>
    public List<Condition> Conditions
    {
        get => _Conditions;
        set
        {
            _Conditions = value ?? new List<Condition>();
            foreach (var condition in _Conditions)
                condition.Owner = this;
        }
    }

    /// <summary>
    /// Invokes a condition
    /// </summary>
    /// <param name="condition">the condition to invokte</param>
    /// <param name="state">the condition state</param>
    public void InvokeChange(Condition condition, bool state)
    {
        if(this.DisabledConditions?.Any(x => x == condition) == true)
            this.DisabledChange?.Invoke(state);
        if (this.Conditions?.Any(x => x == condition) == true)
        {
            this.ConditionsChange?.Invoke(state == false); // state is the "disabled" state, for conditions we want the inverse
        }
    }

    /// <summary>
    /// Tests if all the conditions on this field match
    /// </summary>
    /// <returns>if all the conditions on this field match</returns>
    public bool ConditionsAllMatch()
    {
        if (this.Conditions?.Any() != true)
            return true;
        bool matches = true;
        foreach (var condition in this.Conditions)
        {
            if (condition.IsMatch == false)
            {
                matches = false;
            }
        }

        return matches;
    }

    /// <summary>
    /// Constructs a separator
    /// </summary>
    /// <returns>a separator</returns>
    public static ElementField Separator()
        => new() { InputType = FormInputType.HorizontalRule };
}