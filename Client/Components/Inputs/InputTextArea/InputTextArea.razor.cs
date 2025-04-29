
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Represents a partial class for an input text area, inheriting from the generic Input class with a string type parameter.
/// </summary>
public partial class InputTextArea : Input<string>
{  
    /// <summary>
    /// Overrides the Focus method to focus on the element with the specified UID.
    /// </summary>
    /// <returns>Returns true if the focus operation is successful.</returns>
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Gets or sets the number of rows to show in the text area
    /// </summary>
    [Parameter]
    public int Rows { get; set; } = 8;
    
    /// <summary>
    /// Gets or sets if this should flex grow to 100%
    /// </summary>
    [Parameter]
    public bool FlexGrow { get; set; } = false;

    /// <summary>
    /// Gets or sets the variables available
    /// </summary>
    [Parameter]
    public Dictionary<string, object>? Variables { get; set; }
    
    /// <summary>
    /// Overrides the ValueUpdated method to clear any error associated with the input value update.
    /// </summary>
    protected override void ValueUpdated()
    {
        ClearError();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        base.OnInitialized();
        // #if(DEBUG)
        // var jsObjectReference = await jSRuntime.InvokeAsync<IJSObjectReference>("import",
        //     $"./Components/Inputs/InputTextArea/InputTextArea.razor.js?v={Globals.Version}");
        // await jsObjectReference.InvokeVoidAsync("createInputTextArea", DotNetObjectReference.Create(this), Uid, new Dictionary<string, object>
        // {
        //     { "a.alfred", "alfred" },
        //     { "a.batman", "batman" },
        //     { "a.batgirl", "batgirl" },
        //     { "a.b.c", "ccccc" },
        //     { "a.b.d", "dddd" },
        //     { "b.alfred", "alfred" },
        //     { "b.batman", "batman" },
        //     { "b.batgirl", "batgirl" },
        //     { "b.b.c", "ccccc" },
        //     { "b.b.d", "dddd" },
        //     { "c.alfred", "alfred" },
        //     { "c.batman", "batman" },
        //     { "c.batgirl", "batgirl" },
        //     { "c.b.c", "ccccc" },
        //     { "c.b.d", "dddd" },
        //     { "library.Name", "some library" },
        //     { "library.UID", Guid.NewGuid() },
        //     { "MyVariable", "my variable value" },
        // });
        // #else
        if (Variables?.Any() == true)
        {
            var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Inputs/InputTextArea/InputTextArea.razor.js?v={Globals.Version}");
            await jsObjectReference.InvokeVoidAsync("createInputTextArea", DotNetObjectReference.Create(this), Uid, Variables);
        }
        //#endif
    }
    
    /// <summary>
    /// Updates the value from JavaScript
    /// </summary>
    /// <param name="value">the updated value</param>
    /// <returns>a task to await</returns>
    [JSInvokable("updateValue")]
    public Task UpdateValue(string value)
    {
        this.Value = value;
        StateHasChanged(); 
        return Task.CompletedTask;
    }
}