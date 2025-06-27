using FileFlows.Client.ClientModels;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Common;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

/// <summary>
/// Inline editor
/// </summary>
public partial class InlineEditor : EditorBase
{
    /// <inheritdoc />
    [Parameter]
    public override string TypeName { get; set; }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; private set; }

    /// <summary>
    /// Gets or sets the ubttons
    /// </summary>
    [Parameter] public List<ActionButton> Buttons { get; set; } = new();

    private string Uid = Guid.NewGuid().ToString();

    private RenderFragment FieldsFragment;

    protected List<EditorTab> Tabs { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the submit callback
    /// </summary>
    [Parameter] public EventCallback SubmitCallback { get; set; }

    #pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the fields
    /// </summary>
    [Parameter]
    public new List<IFlowField> Fields
    {
        get => base.Fields;
        set => base.Fields = value;
    }
    /// <inheritdoc />
    [Parameter] public override ExpandoObject Model 
    {
        get => base.Model;
        set => base.Model = value;
    }
    #pragma warning restore BL0007

    /// <summary>
    /// Gets or sets if the fields scrollbar should be hidden
    /// </summary>
    private bool HideFieldsScroller { get; set; }

    /// <summary>
    /// Gets if this editor is readonly
    /// </summary>
    public bool ReadOnly { get; private set; }

    /// <summary>
    /// Gets if a confirmation prompt should be shown if there are changes made when the user cancels the editor
    /// </summary>
    public bool PromptUnsavedChanges { get; private set; }

    protected bool FocusFirst = false;
    /// <summary>
    /// Indicates if this component needs rendering
    /// </summary>
    private bool _needsRendering = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        BuildFieldsRenderFragment();
    }

    //
    // /// <summary>
    // /// Sets the fields
    // /// </summary>
    // /// <param name="fields">the fields arguments</param>
    // /// <returns>the updated model from the edit</returns>
    // public void SetFields(List<ElementField> fields, List<ActionButton> buttons, object model)
    // {
    //     this.RegisteredInputs.Clear();
    //     var expandoModel = ConvertToExando(model);
    //     this.Model = expandoModel;
    //     //this.HideFieldsScroller = args.HideFieldsScroller;
    //     //this.PromptUnsavedChanges = args.PromptUnsavedChanges;
    //     //if (args.PromptUnsavedChanges && args.ReadOnly == false) 
    //         this.CleanModelJson = ModelToJsonForCompare(expandoModel);
    //     //this.TypeName = args.TypeName;
    //     this.Uid = Guid.NewGuid().ToString();
    //     this.Fields = fields;
    //     this.Buttons.Clear();
    //     if(buttons?.Any() == true)
    //         this.Buttons.AddRange(buttons);
    //     //this.Tabs = args.Tabs;
    //
    //     BuildFieldsRenderFragment();
    //     
    //     this.FocusFirst = true;
    //     this.StateHasChanged();
    // }

    /// <summary>
    /// Gets the total number of buttons
    /// </summary>
    private int NumberOfButtons => Buttons.Count; 

    private void BuildFieldsRenderFragment()
    {
        FieldsFragment = (builder) => { };
        _ = this.WaitForRender();
        FieldsFragment = (builder) =>
        {
            int count = -1;

            if (Fields?.Any() == true)
            {
                builder.OpenComponent<FlowPanel>(++count);
                builder.AddAttribute(++count,  nameof(FlowPanel.Fields), Fields);
                builder.AddAttribute(++count, nameof(FlowPanel.OnSubmit), EventCallback.Factory.Create(this, SubmitCallback));
                //builder.AddAttribute(++count, nameof(FlowPanel.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
                if (Fields.Count > 4)
                {
                    builder.OpenElement(++count, "div");
                    builder.AddAttribute(++count, "class", "empty");
                    builder.CloseElement();
                }
            }

            if (Tabs?.Any() == true)
            {
                builder.OpenComponent<FlowTabsBuilder>(++count);
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.Tabs), Tabs);
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnSubmit), EventCallback.Factory.Create(this, SubmitCallback));
                //builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
            }
        };
    }

    /// <summary>
    /// Waits for the component to render
    /// </summary>
    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _needsRendering = false;
        return base.OnAfterRenderAsync(firstRender);
    }
}