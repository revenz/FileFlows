using System.Text.Json.Serialization;

namespace FileFlows.Shared.Models;


/// <summary>
/// A condition that determines if a element field is shown or disabled
/// </summary>
public class Condition
{
    /// <summary>
    /// Get or sets the Field this condition is attached to
    /// </summary>
    [JsonIgnore]
    public ElementField Field { get; private set; }
    /// <summary>
    /// Gets or sets the property this condition evaluates
    /// </summary>
    public string Property { get; set; }
    /// <summary>
    /// Gets or sets the value used to when evaluating the condition
    /// </summary>
    public object Value { get; set; }
    /// <summary>
    /// Gets or sets if the match is inversed, ie is not the value
    /// </summary>
    public bool IsNot { get; set; }

    /// <summary>
    /// Gets or sets if this condition is a match
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Gets or sets the owner who owns this condition
    /// </summary>
    [JsonIgnore]
    public ElementField Owner { get; set; }

    /// <summary>
    /// Constructs a condition
    /// </summary>
    public Condition()
    {

    }

    /// <summary>
    /// Constructs a condition
    /// </summary>
    /// <param name="field">the field the condition is attached to</param>
    /// <param name="initialValue">the initial value of the field</param>
    /// <param name="value">the value to evaluate for</param>
    /// <param name="isNot">if the condition should NOT match the value</param>
    public Condition(ElementField field, object? initialValue, object value = null, bool isNot = false)
    {
        this.Property = field.Name;
        this.Value = value;
        this.IsNot = isNot;
        this.SetField(field, initialValue);
    }

    /// <summary>
    /// Sets the field 
    /// </summary>
    /// <param name="field">the field</param>
    /// <param name="initialValue">the fields initial value</param>
    public void SetField(ElementField field, object? initialValue)
    {
        this.Field = field;
        this.Field.ValueChanged += Field_ValueChanged;
        Field_ValueChanged(this, initialValue);
    }

    /// <summary>
    /// Fired when the field value changes
    /// </summary>
    /// <param name="sender">the sender object</param>
    /// <param name="value">the new field value</param>
    private void Field_ValueChanged(object sender, object? value)
    {
        bool matches = this.Matches(value);
        matches = !matches; // reverse this as we matches mean enabled, so we want disabled
        this.IsMatch = matches;
        this.Owner?.InvokeChange(this, matches);
    }

    /// <summary>
    /// Test if the condition matches the given object value
    /// </summary>
    /// <param name="value">the value to test the condition against</param>
    /// <returns>true if the condition is matches</returns>
    public virtual bool Matches(object? value)
        => Matches(this.Value, value, this.IsNot);

    /// <summary>
    /// Tests if a value matches the expected value
    /// </summary>
    /// <param name="expected">the expected value</param>
    /// <param name="value">the value to test</param>
    /// <param name="isNot">if the result should be inverted</param>
    /// <returns>the result of the match</returns>
    public static bool Matches(object? expected, object? value, bool isNot)
    {
        bool matches = false;
        string strValue = expected?.ToString() ?? string.Empty;
        if (strValue.Length > 1 && strValue.StartsWith("/") && strValue.EndsWith("/"))
        {
            // special case, regex
            try
            {
                matches = new Regex(strValue[1..^1]).IsMatch(value.ToString());
            }
            catch (Exception ex)
            {
                Logger.Instance.ILog("matches error: " + ex.Message);
            }
        }
        else
        {
            matches = Helpers.ObjectHelper.ObjectsAreSame(value, expected);
        }

        if (isNot)
            matches = !matches;
        return matches;
    }
}

