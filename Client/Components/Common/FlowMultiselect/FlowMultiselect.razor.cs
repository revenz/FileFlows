using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Multiselect
/// </summary>
public partial class FlowMultiselect : ComponentBase
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the options
    /// </summary>
    [Parameter] public List<ListOption> Options { get; set; } 
    
    /// <summary>
    /// Gets or sets if this input is currently invalid
    /// </summary>
    [Parameter] public bool Invalid { get; set; }
    
    /// <summary>
    /// Gets or sets the value
    /// </summary>
    [Parameter]
    public object[] Value { get; set; }

    /// <summary>
    /// Event called when the value changes
    /// </summary>
    [Parameter] 
    public EventCallback<object[]> ValueChanged { get; set; }
   
    /// <summary>
    /// The UID of the component
    /// </summary>
    private readonly string Uid = "ff" + Guid.NewGuid().ToString("N");

    private IJSObjectReference jsMultiselect;
    private Dictionary<string, ListOption> MappedValues = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        foreach (var option in Options)
        {
            MappedValues[Guid.NewGuid().ToString()] = option;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Common/FlowMultiselect/FlowMultiselect.razor.js?v={Globals.Version}");
            var mappedValueList = new List<ListOption>();
            foreach (var kv in MappedValues)
            {
                mappedValueList.Add(new ListOption()
                {
                    Label = kv.Value.Label,
                    Value = kv.Key
                });
            }
            List<string> initialValues = [];
            foreach (var value in Value)
            {
                var option = Options.FirstOrDefault(x =>
                    x.Value == value || (x.Value != null && value != null && x.Value.Equals(value)));
                if (option != null)
                {
                    var key = MappedValues.FirstOrDefault(x => x.Value == option);
                    if(string.IsNullOrEmpty(key.Key) == false)
                        initialValues.Add(key.Key);
                }
            }

            jsMultiselect = await jsObjectReference.InvokeAsync<IJSObjectReference>("createMultiselect", DotNetObjectReference.Create(this), 
                Uid, mappedValueList, initialValues);
        }
    }
    
    /// <summary>
    /// Method called by javascript to update the selected values
    /// </summary>
    /// <param name="values">the updated values</param>
    [JSInvokable]
    public void UpdateSelectedValues(string[] values)
    {
        List<object> actualValues = new List<object>();
        foreach (var value in values)
        {
            if(MappedValues.TryGetValue(value, out var option))
                actualValues.Add(option.Value);
        }
        var array = actualValues.ToArray();
        Value = array;
        ValueChanged.InvokeAsync(array);
    }
}