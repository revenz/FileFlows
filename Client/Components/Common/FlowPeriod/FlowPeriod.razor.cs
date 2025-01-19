using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Flow period
/// </summary>
public partial class FlowPeriod : ComponentBase
{
    /// <summary>
    /// The unique identifier for this element
    /// </summary>
    private string Uid = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter]
    public int Value { get; set; }

    /// <summary>
    /// Event called when the value changes
    /// </summary>
    [Parameter] 
    public EventCallback<int> ValueChanged { get; set; }
    
    /// <summary>
    /// Gets or sets if this control is read-only
    /// </summary>
    [Parameter]
    public bool ReadOnly { get;set; }
    
    /// <summary>
    /// Gets or sets if this control is disabled
    /// </summary>
    [Parameter]
    public bool Disabled { get;set; }

    /// <summary>
    /// Gets or sets if the weeks is shown
    /// </summary>
    [Parameter] public bool ShowWeeks { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the selected period
    /// </summary>
    private int Period { get; set; }

    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    private int Number { get; set; }

    /// <summary>
    /// The translation labels
    /// </summary>
    private string lblWeeks, lblDays, lblHours, lblMinutes;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        lblWeeks = Translater.Instant("Enums.Periods.Weeks");
        lblDays = Translater.Instant("Enums.Periods.Days");
        lblHours = Translater.Instant("Enums.Periods.Hours");
        lblMinutes = Translater.Instant("Enums.Periods.Minutes");

        this.Period = 1440;

        if (Value > 0)
        {
            var ranges = ShowWeeks ? new[] { 10080, 1440, 60, 1 } : new[] { 1440, 60, 1 };
            foreach (int p in ranges)
            {
                if (Value % p == 0)
                {
                    // weeks
                    Number = Value / p;
                    Period = p;
                    break;
                }
            }
        }
        else
        {
            Number = 3;
            Period = 1440;
            Value = Number * Period;
        }
    }
    

    /// <summary>
    /// Changes the value
    /// </summary>
    /// <param name="e">the change event</param>
    private void ChangeValue(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString() ?? "", out int value) == false)
            return;
        
        int max = Period switch
        {
            1 => 100_000,
            60 => 10_000,
            1440 => 10_000,
            _ => 1_000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        this.Value = this.Number * this.Period;
        ValueChanged.InvokeAsync(Value);
    }
    
    /// <summary>
    /// Event when the period changed
    /// </summary>
    /// <param name="args">the change event</param>
    private void PeriodSelectionChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args?.Value?.ToString(), out int index))
        {
            Period = index;
            Value = Number * Period;
            ValueChanged.InvokeAsync(Value);
        }
        else
            Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
    }
}