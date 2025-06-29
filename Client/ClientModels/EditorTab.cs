namespace FileFlows.Client.ClientModels;

/// <summary>
/// A tab shown in an editor
/// </summary>
public class EditorTab
{
    /// <summary>
    /// Gets or sets the name of the tab
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the fields
    /// </summary>
    public List<IFlowField> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the a condition property to check if this tab is shown
    /// </summary>
    public string ConditionProperty { get; set; }

    /// <summary>
    /// Gets or sets a condition value to check if this tab is shown
    /// </summary>
    public object ConditionValue { get; set; }

    /// <summary>
    /// Gets or sets if the condition check should be inversed
    /// </summary>
    public bool ConditionInverse { get; set; }

    private bool _hidden;

    /// <summary>
    /// Gets or sets if the tab is hidden
    /// </summary>
    public bool Hidden
    {
        get => _hidden;
        set
        {
            if (_hidden == value)
                return;
            _hidden = value;
            HiddenChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Called when hidden is changed
    /// </summary>
    public event Action<EditorTab, bool>? HiddenChanged;
}