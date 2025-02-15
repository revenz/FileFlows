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
public partial class InputCustomFieldOptions : Input<List<CustomFieldOption>>
{
    private string NewName = string.Empty;
    private string NewValue = string.Empty;
    private int NewTokens = 0;
    /// <summary>
    /// Focuses the input
    /// </summary>
    /// <returns>true if the input gained focus, otherwise false</returns>
    public override bool Focus() => FocusUid();
    
    /// <summary>
    /// Gets or sets if variables are allowed in the values
    /// </summary>
    [Parameter] public bool ShowVariables { get; set; }
    
    /// <summary>
    /// The data for the input
    /// </summary>
    private Dictionary<Guid, CustomFieldOption> Data = new ();
    

    private string DuplicateKey = null; // one time we do want null....

    private string lblName, lblValue, lblTokens, lblMoveUp, lblMoveDown, lblDelete, lblAdd;

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        lblName = Translater.Instant("Labels.Name");
        lblValue = Translater.Instant("Labels.Value");
        lblTokens = Translater.Instant("Labels.Tokens");
        lblMoveUp = Translater.Instant("Labels.MoveUp");
        lblMoveDown = Translater.Instant("Labels.MoveDown");
        lblDelete = Translater.Instant("Labels.Delete");
        lblAdd = Translater.Instant("Labels.Add");
        base.OnInitialized();
        if (Value == null)
            Value = new List<CustomFieldOption>();

        this.Data = Value.Select(x => new CustomFieldOption()
        {
            Name = x.Name,
            Value = x.Value,
            Tokens = x.Tokens
        }).ToDictionary(x => Guid.NewGuid());
        if(Field != null)
            this.Field.ValueChanged += FieldOnValueChanged;
    }

    protected override void FieldOnValueChanged(object sender, object value)
    {
        if (ValueIsUpdating)
            return;
        
        if (value == null)
            return;
        if(value is List<CustomFieldOption> cfos == false)
            return;

        bool differences = false;
        foreach (var cfo in cfos)
        {
            var existing = this.Data.Values.FirstOrDefault(x => x.Name == cfo.Name);
            if (existing == null)
            {
                Data[Guid.NewGuid()] = new () { Name = cfo.Name, Value = cfo.Value, Tokens = cfo.Tokens};
                differences = true;
            }
            else if (existing.Value != cfo.Value)
            {
                existing.Value = cfo.Value;
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
    /// Moves an option either up or down within the sorted dictionary.
    /// </summary>
    /// <param name="key">The key of the option to move.</param>
    /// <param name="up">True to move up, false to move down.</param>
    void Move(Guid key, bool up)
    {
        var keys = Data.Keys.ToList(); // Get ordered list of keys
        int index = keys.IndexOf(key);
    
        if (index < 0) 
            return; // Key not found

        if (up && index == 0)
            return; // Already at the top

        if (!up && index == keys.Count - 1)
            return; // Already at the bottom

        // Determine the target index
        int swapIndex = up ? index - 1 : index + 1;
        Guid swapKey = keys[swapIndex];

        // Swap values
        (Data[key], Data[swapKey]) = (Data[swapKey], Data[key]);
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
        string name = NewName;
        string value = NewValue ?? string.Empty;
        int tokens = NewTokens;
        if (string.IsNullOrWhiteSpace(name))
            return;
        name = name.Trim();

        this.Data[Guid.NewGuid()] = new () {  Name = name, Value = value, Tokens = tokens };

        CheckForDuplicates();

        NewName = string.Empty;
        NewValue = string.Empty;
        NewTokens = 0;
        FocusUid();
        UpdateBindValue();
    }

    /// <summary>
    /// When the input loses focus
    /// </summary>
    void OnBlur()
    {
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

        this.Value = this.Data.Values.Select(x => new CustomFieldOption()
        {
            Name = x.Name,
            Value = x.Value,
            Tokens = x.Tokens
        }).ToList();
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
        DuplicateKey = this.Data?.Values?.GroupBy(x => x.Name, x => x)?.FirstOrDefault(x => x.Count() > 1)
            ?.Select(x => x.Name)?.FirstOrDefault();
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
}