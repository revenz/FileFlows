using FileFlows.Client.Components.Editors;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Flow Icon Value component
/// </summary>
public partial class FlowIconValue : ComponentBase
{
    private string _icon = string.Empty;
    private string _color = string.Empty;
    private string _value = string.Empty;
    private static string _InternalProcessingNode;
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject]
    protected FrontendService feService { get; set; }
    
    static FlowIconValue()
    {
        _InternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
    }
    
    /// <summary>
    /// Gets or sets the Icon
    /// </summary>
    [Parameter]
    public string Icon
    {
        get => _icon;
        set => _icon = value;
    }

    /// <summary>
    /// Gets or set the string value
    /// </summary>
    [Parameter] public string Value 
    {
        get => _value;
        set => _value = value;
    }
    
    /// <summary>
    /// Gets or sets the color
    /// </summary>
    [Parameter] public string Color
    {
        get => _color;
        set => _color = value;
    }
    
    /// <summary>
    /// Gets or sets a value uid for onclick events
    /// </summary>
    [Parameter] public Guid? ValueUid { get; set; }
    
    /// <summary>
    /// Gets or sets the on click event
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Gets if this is clickable
    /// </summary>
    private bool Clickable => OnClick.HasDelegate || _IsUidClickable;

    private bool _IsFlow, _IsScript, _IsUidClickable;

    /// <summary>
    /// Handles the click event
    /// </summary>
    private void ClickHandler()
    {
        if (OnClick.HasDelegate)
        {
            _ = OnClick.InvokeAsync();
        }
        else if(ValueUid != null)
        {
            if(_IsFlow)
                NavigationManager.NavigateTo($"/flows/{ValueUid.Value}");
            if (_IsScript)
                OpenScript();
        }
    }

    /// <summary>
    /// Opens a script for editing
    /// </summary>
    private void OpenScript()
    {
        _ = ModalService.ShowModal<Editors.ScriptEditor, Script>(new ModalEditorOptions()
        {
            Uid = ValueUid!.Value
        });
    }

    protected override void OnParametersSet()
    {
        _icon = _icon.ToLowerInvariant();
        if (_icon == "library")
        {
            _icon = "fas fa-folder";
            _color = _color?.EmptyAsNull() ?? "green";
        }
        else if (_icon == "flow")
        {
            _IsUidClickable = ValueUid != null;
            _IsFlow = true;
            _icon = "fas fa-sitemap";
            _color = _color?.EmptyAsNull() ?? "blue";
        }
        else if (_icon.StartsWith("node"))
        {
            _color = _color?.EmptyAsNull() ?? "purple";
            _icon = _icon switch
            {
                "node:docker" => "fab fa-docker",
                "node:windows" => "fab fa-windows",
                "node:apple" or "node:mac" or "node:macos" => "fab fa-apple",
                "node:linux" => "fab fa-linux",
                _ => "fas fa-desktop"
            };
            if (_value == "FileFlowsServer")
                _value = _InternalProcessingNode;
        }
        else if (_icon == "script")
        {
            _icon = "fas fa-scroll";
            _color = _color?.EmptyAsNull() ?? "blue";
            _IsScript = true;
            if (feService.HasRole(UserRole.Scripts))
            {
                _IsUidClickable = ValueUid != null;
            }
        }
        
    }
}