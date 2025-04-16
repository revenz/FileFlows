using FileFlows.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FileFlows.Plugin;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a KeyValue pair
/// </summary>
public partial class InputKeyValue : Input<List<KeyValuePair<string, string>>>
{
    private string NewKey = string.Empty;
    private string NewValue = string.Empty;
    /// <summary>
    /// Focuses the input
    /// </summary>
    /// <returns>true if the input gained focus, otherwise false</returns>
    public override bool Focus() => FocusUid();
    
    /// <summary>
    /// Gets or sets options that can appear in the key/value list as the key
    /// </summary>
    [Parameter]
    public List<ListOption> Options { get; set; }
    
    /// <summary>
    /// Gets or sets if variables are allowed in the values
    /// </summary>
    [Parameter] public bool ShowVariables { get; set; }

    /// <summary>
    /// Gets or sets if duplicates are allowed
    /// </summary>
    [Parameter] public bool AllowDuplicates { get; set; }
    
    /// <summary>
    /// Gets or sets if the key value labels are hidden
    /// </summary>
    [Parameter] public bool HideKeyValueLabels { get; set; }
    
    /// <summary>
    /// The data for the input
    /// </summary>
    private Dictionary<Guid, KeyValue> Data = new ();
    

    /// <summary>
    /// Gets or sets the variables available
    /// </summary>
    [Parameter] public Dictionary<string, object> Variables { get; set; } = new();

    private string DuplicateKey = null; // one time we do want null....

    private string lblKey, lblValue;
    private bool HasOptions;

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        if (HideKeyValueLabels == false)
        {
            lblKey = Translater.Instant(this.LabelOriginal + "Key");
            lblValue = Translater.Instant(this.LabelOriginal + "Value");
        }
        

        base.OnInitialized();
        if (Value == null)
            Value = new List<KeyValuePair<string, string>>();

        this.Data = Value.Select(x => new KeyValue {  Key = x.Key, Value = x.Value }).ToDictionary(x => Guid.NewGuid());
        if(Field != null)
            this.Field.ValueChanged += FieldOnValueChanged;

        HasOptions = Options?.Any() == true;
    }

    protected override void FieldOnValueChanged(object sender, object value)
    {
        if (ValueIsUpdating)
            return;
        
        if (value == null)
            return;
        if(value is List<KeyValuePair<string,string>> kvps == false)
            return;

        if (Options?.Any() == true && Data.Count == kvps.Count)
            return;
        
        bool differences = false;
        foreach (var kvp in kvps)
        {
            var existing = this.Data.Values.FirstOrDefault(x => x.Key == kvp.Key);
            if (existing == null)
            {
                Data[Guid.NewGuid()] = new () { Key = kvp.Key, Value = kvp.Value };
                differences = true;
            }
            else if (existing.Value != kvp.Value)
            {
                existing.Value = kvp.Value;
                differences = true;
            }
        }

        if (differences)
            this.StateHasChanged();
    }


    /// <summary>
    /// Remove an item from the list
    /// </summary>
    /// <param name="key">the key of the item to remove</param>
    void Remove(Guid key)
    {
        this.Data.Remove(key);
        CheckForDuplicates();
        UpdateBindValue();
    }

    /// <summary>
    /// Called when the the new value is blurred
    /// </summary>
    void BlurAdd()
    {
        if (string.IsNullOrWhiteSpace(NewValue))
            return; // dont add if now value set
        
        Add();
    }

    /// <summary>
    /// Add a new item to the list
    /// </summary>
    void Add()
    {
        string key = NewKey;
        string value = NewValue ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
            return;
        key = key.Trim();

        this.Data[Guid.NewGuid()] = new () {  Key = key, Value = value };

        CheckForDuplicates();

        NewKey = string.Empty;
        NewValue = string.Empty;
        FocusUid();
        UpdateBindValue();
    }

    /// <summary>
    /// When the input loses focus
    /// </summary>
    void OnBlur()
    {
        if (ReadOnly)
            return;
        // CheckForDuplicates();
        UpdateBindValue();
    }

    bool UpdateBindValue()
    {
        this.Data ??= new();

        if (CheckForDuplicates() == false)
            return false;


        if (this.Data.Any() == false && Validators?.Any(x => x.Type == "Required") == true)
        {
            this.ErrorMessage = Translater.Instant("Validators.Required");
            return false;
        }


        this.Value = this.Data.Values.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList();
        return true;
    }

    /// <summary>
    /// Validates the input
    /// </summary>
    /// <returns>whether or not the input is valid</returns>
    public override async Task<bool> Validate()
    {
        if (UpdateBindValue() == false)
            return false;

        return await base.Validate();
    }

    /// <summary>
    /// Check the input for duplicates
    /// </summary>
    /// <returns>true if there are duplicates, otherwise false</returns>
    private bool CheckForDuplicates()
    {
        if (AllowDuplicates)
            return true; // theyre allowed duplicates
        if (HasOptions)
            return true; // dont care about duplicates using dropdown
        DuplicateKey = this.Data?.Values?.GroupBy(x => x.Key, x => x)?.FirstOrDefault(x => x.Count() > 1)?.Select(x => x.Key)?.FirstOrDefault();
        if (DuplicateKey != null)
        {
            Logger.Instance.WLog("Duplicates found, " + DuplicateKey);
            ErrorMessage = Translater.Instant("ErrorMessages.DuplicatesFound");
            this.StateHasChanged();
            return false;
        }
        ErrorMessage = string.Empty;
        return true;
    }

    private void UpdateKeyValue(ChangeEventArgs e, KeyValue kv)
        => kv.Key = e.Value?.ToString();
    private void UpdateNewKey(ChangeEventArgs e) 
        => NewKey= e.Value?.ToString();
    
    /// <summary>
    /// A key value
    /// </summary>
    class KeyValue
    {
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public string Value { get; set; }   
    }
}