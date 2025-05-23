using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input date range component
/// </summary>
public partial class InputDateCompare : Input<DateCompareModel>
{
    /// <summary>
    /// Gets or sets the allowed modes
    /// </summary>
    [Parameter]
    public DateCompareMode AllowedModes { get; set; } = DateCompareMode.GreaterThan |
                                                        DateCompareMode.LessThan |
                                                        DateCompareMode.Between |
                                                        DateCompareMode.NotBetween |
                                                        DateCompareMode.Before |
                                                        DateCompareMode.After;

    /// <summary>
    /// The comparison modes
    /// </summary>
    private List<ListOption> comparisonModeOptions = [];
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (string.IsNullOrWhiteSpace(Help))
            Help = Translater.Instant("Inputs.InputDateCompare.Help");
        
        DateCompareMode? firstMode = null;
        foreach (DateCompareMode mode in Enum.GetValues(typeof(DateCompareMode)))
        {
            if (AllowedModes.HasFlag(mode))
            {
                if (firstMode == null)
                {
                    firstMode = mode;
                }
                comparisonModeOptions.Add(new ListOption 
                { 
                    Value = mode, 
                    Label = Translater.Instant($"Enums.{nameof(DateCompareMode)}.{mode}") 
                });
            }
        }

        if(Value == null)
        {
            Value = new()
            {
                Comparison = firstMode ?? DateCompareMode.GreaterThan,
                Value1 = 5,
                Value2 = 10,
                DateValue = DateTime.Today.ToUniversalTime()
            };
        }
    }


    /// <summary>
    /// Gets or sets the comparison mode
    /// </summary>
    public DateCompareMode Comparison
    {
        get => Value.Comparison;
        set
        {
            Value.Comparison = value;
            ValueChanged.InvokeAsync(Value);
        }
    }
    /// <summary>
    /// Gets or sets the first minute value
    /// </summary>
    public int Value1
    {
        get => Value.Value1;
        set
        {
            Value.Value1 = value;
            ValueChanged.InvokeAsync(Value);
        }
    }
    /// <summary>
    /// Gets or sets the second minute value
    /// </summary>
    public int Value2
    {
        get => Value.Value2;
        set
        {
            Value.Value2 = value;
            ValueChanged.InvokeAsync(Value);
        }
    }

    /// <summary>
    /// Gets or sets the local date
    /// </summary>
    private DateTime LocalDate
    {
        get => Value.DateValue.ToLocalTime();
        set
        {
            Value.DateValue = value.ToUniversalTime();
            ValueChanged.InvokeAsync(Value);
        }
    }
}