using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a period, time in minutes
/// </summary>
public partial class InputPeriod : Input<int>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Gets or sets the selected period
    /// </summary>
    private int Period { get; set; }

    /// <summary>
    /// Gets or sets the number value
    /// </summary>
    private int Number { get; set; }

    /// <summary>
    /// Gets or sets if the weeks is shown
    /// </summary>
    [Parameter]
    public bool ShowWeeks { get; set; } = true;

    /// <summary>
    /// Gets or sets if the is using seconds
    /// </summary>
    [Parameter]
    public bool Seconds { get; set; } = true;

    private const int SECONDS_SECOND = 1;
    private const int SECONDS_MINUTE = 60;
    private const int SECONDS_HOUR = 3600;
    private const int SECONDS_DAY = 86400;
    private const int SECONDS_WEEK = 604800;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Period = SECONDS_DAY;
        var v = Seconds ? Value : Value * 60;

        if (v > 0)
        {
            var ranges = ShowWeeks ? new[] { SECONDS_WEEK, SECONDS_DAY, SECONDS_HOUR, SECONDS_MINUTE, SECONDS_SECOND } 
                : new[] { SECONDS_DAY, SECONDS_HOUR, SECONDS_MINUTE, SECONDS_SECOND };
            foreach (int p in ranges)
            {
                if (v % p == 0)
                {
                    // weeks
                    Number = v / p;
                    Period = p;
                    break;
                }
            }
        }
        else
        {
            Number = 3;
            Period = SECONDS_HOUR;
            SetValue(3 * SECONDS_HOUR);
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
            SECONDS_SECOND => 6_000_000,
            SECONDS_MINUTE => 100_000,
            SECONDS_HOUR => 10_000,
            SECONDS_DAY => 10_000,
            _ => 1_000
        };
        if (value > max)
            value = max;
        else if (value < 1)
            value = 1;
        this.Number = value;
        SetValue(this.Number * this.Period);
        this.ClearError();
    }

    /// <summary>
    /// Event called when a key is pressed
    /// </summary>
    /// <param name="e">the keyboard event</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        // else if (e.Code == "Escape")
        //     await OnClose.InvokeAsync();
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
            SetValue(this.Number * this.Period);
        }
        else
            Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
    }

    /// <summary>
    /// Sets the value
    /// </summary>
    /// <param name="seconds">the number of seconds</param>
    private void SetValue(int seconds)
    {
        if (Seconds == false)
            seconds /= 60; // convert to minuntes
        Value = seconds;
    }
}