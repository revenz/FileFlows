using System.Globalization;

namespace FileFlows.Client.Components.Inputs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;

public partial class InputNumber<TItem> : Input<TItem>
{
    public override bool Focus() => FocusUid();

    private TItem _Min, _Max;


    [Parameter] public bool AllowFloat { get; set; }
    
    /// <summary>
    /// Gets or sets if 0 is shown as empty
    /// </summary>
    [Parameter] public bool ZeroAsEmpty { get; set; }

    [Parameter]
    public TItem Min { get => _Min; set => _Min = value; }
#pragma warning disable BL0007
    [Parameter]
    public TItem Max 
    { 
        get => _Max; 
        set => _Max = value.Equals(0) ? (TItem)(object)int.MaxValue : value; 
    }
#pragma warning restore BL0007

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_Max.ToString() == "0")
        {
            if (typeof(TItem) == typeof(float))
                _Max = (TItem)Convert.ChangeType((float)int.MaxValue, typeof(TItem));
            else
                _Max = (TItem)Convert.ChangeType(int.MaxValue, typeof(TItem));
        }
    }
    

    private async Task ChangeValue(ChangeEventArgs e)
    {
        string strValue = e.Value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(strValue) && ZeroAsEmpty)
            strValue = "0";
        if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
        {
            if (value > int.MaxValue)
                value = int.MaxValue;
            else if (value < int.MinValue)
                value = int.MinValue;
            this.Value = (TItem)Convert.ChangeType(value, typeof(TItem));
            float fValue = Convert.ToSingle(Value);
            if (fValue > Convert.ToSingle(Max) && Convert.ToSingle(Max) != 0)
            {
                this.Value = Max;
                await UpdateInputValue();
            }
            else if (fValue < Convert.ToSingle(Min))
            {
                this.Value = Min;
                await UpdateInputValue();
            }
        }
        else
        {
            await UpdateInputValue();
        }
        this.ClearError();
    }

    async Task UpdateInputValue()
    {
        await jsRuntime.InvokeVoidAsync("eval", new object[] { $"document.getElementById('{Uid}').value = " + this.Value });
    }
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
        // else if (e.Code == "Escape")
        //     await OnClose.InvokeAsync();
    }

    private string GetBoundValue()
    {
        if(Value?.ToString() == "0" && ZeroAsEmpty)
            return string.Empty;
        double dValue = Convert.ToDouble(Value);
        return dValue.ToString("0.##", CultureInfo.InvariantCulture);
    }
}