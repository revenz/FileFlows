using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// List options
/// </summary>
public partial class FlowListOptions : ComponentBase
{

    private List<ListOption> _Value = [];

    /// <summary>
    /// Gets or sets the values
    /// </summary>
    [Parameter]
    #pragma warning disable BL0007
    public List<ListOption> Value
    {
        get => _Value;
        set => _Value = value ?? [];
    }
    #pragma warning restore BL0007
    
    /// <summary>
    /// The new value
    /// </summary>
    private string NewValue = string.Empty;
    /// <summary>
    /// The new label
    /// </summary>
    private string NewLabel = string.Empty;

    /// <summary>
    /// The duplicate label
    /// </summary>
    private string? DuplicateLabel = null; // one time we do want null....

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblKey, lblValue;

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        lblKey = "Label";
        lblValue = "Value";
    }


    /// <summary>
    /// Remove an item from the list
    /// </summary>
    /// <param name="item">the item to remove</param>
    void Remove(ListOption item)
    {
        this.Value.Remove(item);
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
        var value = NewValue ?? string.Empty;
        var label = NewLabel ?? string.Empty;

        Value.Add(new () { Label = label, Value = value });

        CheckForDuplicates();

        NewValue = string.Empty;
        //FocusUid();
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

    /// <summary>
    /// Update the bind value
    /// </summary>
    /// <returns>true if updated</returns>
    bool UpdateBindValue()
    {
        this._Value ??= new();

        if (CheckForDuplicates() == false)
            return false;


        if (_Value.Any() == false)
        {
            //this.ErrorMessage = Translater.Instant("Validators.Required");
            return false;
        }


        //this.Value = this.Data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
        return true;
    }

    /// <summary>
    /// Check the input for duplicates
    /// </summary>
    /// <returns>true if there are duplicates, otherwise false</returns>
    private bool CheckForDuplicates()
    {
        DuplicateLabel = this.Value?.GroupBy(x => x.Value, x => x)?
            .FirstOrDefault(x => x.Count() > 1)?
            .Select(x => x.Label)?.FirstOrDefault();
        if (DuplicateLabel != null)
        {
            Logger.Instance.WLog("Duplicates found, " + DuplicateLabel);
            //ErrorMessage = Translater.Instant("ErrorMessages.DuplicatesFound");
            this.StateHasChanged();
            return false;
        }
        //ErrorMessage = string.Empty;
        return true;
    }
    
    /// <summary>
    /// When a key is pressed
    /// </summary>
    /// <param name="e">the event</param>
    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" || e.Code == "Tab")
        {
            BlurAdd();
            //FocusUid();
        }
    }
}