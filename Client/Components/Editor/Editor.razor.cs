using Microsoft.AspNetCore.Components;
using FileFlows.Plugin;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Client.ClientModels;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

public partial class Editor : EditorBase, IDisposable
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; private set; }
    
    /// <summary>
    /// Gets or sets the message service
    /// </summary>
    [Inject] MessageService Message { get; set; }
    
    
    /// <summary>
    /// Gets or sets if this is the flow element editor, and if it is, renders slightly differently
    /// </summary>
    [Parameter] public bool FlowElementEditor { get; set; }

    private readonly List<ActionButton> AdditionalButtons = new();

    /// <summary>
    /// The visible value
    /// </summary>
    private bool _Visible;
    /// <summary>
    /// Gtes or sets if this is visible
    /// </summary>
    public bool Visible
    {
        get => _Visible;
        set
        {
            if (_Visible == value)
                return;
            if(value)
                App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
            else
                App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
            _Visible = value;
        }
    }

    public string Title { get; set; }
    public string HelpUrl { get; set; }
    public string Icon { get; set; }

    private string Uid = Guid.NewGuid().ToString();

    private bool UpdateResizer; // when set to true, the afterrender method will reinitailize the resizer in javascript
    
    /// <summary>
    /// Gets or sets if inputs should be full width and not use a maximum width
    /// </summary>
    public bool FullWidth { get; set; }
    
    protected bool Maximised { get; set; }

    private RenderFragment FieldsFragment;
    protected bool IsSaving { get; set; }

    protected string lblSave, lblSaving, lblNext, lblCancel, lblClose, lblHelp, lblDownloadButton;

    /// <summary>
    /// Gets or sets the tabs
    /// </summary>
    //protected Dictionary<string, List<IFlowField>> Tabs { get; set; }
    protected List<EditorTab> Tabs { get; set; } =  new ();

    TaskCompletionSource<(bool Success, ExpandoObject? Model)> OpenTask;

    public delegate Task<bool> SaveDelegate(ExpandoObject model);
    protected SaveDelegate SaveCallback;

    /// <summary>
    /// Gets or sets if the fields scrollbar should be hidden
    /// </summary>
    private bool HideFieldsScroller { get; set; }

    /// <summary>
    /// Gets if the editor has loaded and past the initial render
    /// </summary>
    public bool Loaded { get;private set; }

    protected bool ShowDownload { get; set; }
    /// <summary>
    /// Gets if this editor is readonly
    /// </summary>
    public bool ReadOnly { get; private set; }

    /// <summary>
    /// Gets if a confirmation prompt should be shown if there are changes made when the user cancels the editor
    /// </summary>
    public bool PromptUnsavedChanges { get; private set; }
    public bool Large { get; set; }

    public string EditorDescription { get; set; }


    protected bool FocusFirst = false;
    private bool _needsRendering = false;

    public delegate Task<bool> CancelDeletgate();
    public delegate Task BasicActionDelegate();
    public string DownloadUrl;
    public event CancelDeletgate OnCancel;
    public event BasicActionDelegate OnClosed;

    private string CleanModelJson;


    private RenderFragment _AdditionalFields;
    public RenderFragment AdditionalFields
    {
        get => _AdditionalFields;
        set
        {
            _AdditionalFields = value;
            this.StateHasChanged();
        }
    }

    protected override void OnInitialized()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");
        lblNext = Translater.Instant("Labels.Next");
        this.Maximised = false;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (args.HasModal || this.Visible == false || args.HasLogPartialViewer)
            return;
        Cancel();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Loaded = true;

        if(UpdateResizer)
            jsRuntime.InvokeVoidAsync("ff.resizableEditor", this.Uid);
        if (FocusFirst)
        {
            foreach (var input in RegisteredInputs.Values)
            {
                if (input.Focus())
                    break;
            }
            FocusFirst = false;
        }
    }

    /// <summary>
    /// Opens an editor
    /// </summary>
    /// <param name="args">the opening arguments</param>
    /// <returns>the updated model from the edit</returns>
    internal Task<(bool Success, ExpandoObject? Model)> Open(EditorOpenArgs args)
    {
        this.Loaded = false;
        this.RegisteredInputs.Clear();
        var expandoModel = ConvertToExando(args.Model);
        this.Model = expandoModel;
        this.SaveCallback = args.SaveCallback;
        this.HideFieldsScroller = args.HideFieldsScroller;
        this.PromptUnsavedChanges = args.PromptUnsavedChanges;
        if (args.PromptUnsavedChanges && args.ReadOnly == false) 
            this.CleanModelJson = ModelToJsonForCompare(expandoModel);
        this.TypeName = args.TypeName;
        this.Maximised = false;
        this.FullWidth = args.FullWidth;
        this.Uid = Guid.NewGuid().ToString();
        this.UpdateResizer = true;
        this.AdditionalButtons.Clear();
        if(args.AdditionalButtons?.Any() == true)
            this.AdditionalButtons.AddRange(args.AdditionalButtons);
        if (args.NoTranslateTitle)
            this.Title = args.Title;
        else
            this.Title = Translater.TranslateIfNeeded(args.Title);
        this.Fields = args.Fields;
        this.Tabs = args.Tabs ?? [];
        this.ReadOnly = args.ReadOnly;
        this.Large = args.Large;
        this.ShowDownload = string.IsNullOrWhiteSpace(args.DownloadUrl) == false;
        this.lblDownloadButton = Translater.TranslateIfNeeded(args.DownloadButtonLabel);
        this.DownloadUrl = args.DownloadUrl;
        this.Visible = true;
        this.HelpUrl = args.HelpUrl ?? string.Empty;
        this.AdditionalFields = args.AdditionalFields;


        this.lblSave = args.SaveLabel.EmptyAsNull() ?? "Labels.Save";
        this.lblCancel = Translater.TranslateIfNeeded(args.CancelLabel.EmptyAsNull() ?? "Labels.Cancel");

        if (lblSave == "Labels.Save") {
            this.lblSaving = Translater.Instant("Labels.Saving");
            this.lblSave = Translater.Instant(lblSave);
        }
        else
        {
            this.lblSave = Translater.Instant(lblSave);
            this.lblSaving = lblSave;
        }

        this.EditorDescription = Translater.TranslateIfNeeded(args.Description?.EmptyAsNull() ?? (args.TypeName + ".Description"));
        
        BuildFieldsRenderFragment();
        
        OpenTask = new ();
        this.FocusFirst = true;
        this.StateHasChanged();
        return OpenTask.Task;
    }

    /// <summary>
    /// Gets the total number of buttons
    /// </summary>
    private int NumberOfButtons =>
        (AdditionalButtons?.Count ?? 0) + (ReadOnly ? 1 : 2) + (string.IsNullOrEmpty(HelpUrl) ? 0 : 1) + (ShowDownload ? 1 : 0); 

    private void BuildFieldsRenderFragment()
    {
        FieldsFragment = (builder) => { };
        _ = this.WaitForRender();
        FieldsFragment = (builder) =>
        {
            int count = -1;
            if (string.IsNullOrEmpty(EditorDescription) == false)
            {
                builder.OpenElement(++count, "div");
                builder.AddAttribute(++count, "class", "description");
                string desc = Markdig.Markdown.ToHtml(EditorDescription).Trim();
                if (desc.StartsWith("<p>") && desc.EndsWith("</p>"))
                    desc = desc[3..^4].Trim();
                desc = desc.Replace("<a ", "<a onclick=\"ff.openLink(event);return false;\" ");
                builder.AddContent(++count, new MarkupString(desc));
                builder.CloseElement();
            }

            if (Fields?.Any() == true)
            {
                builder.OpenComponent<FlowPanel>(++count);
                builder.AddAttribute(++count,  nameof(FlowPanel.Fields), Fields);
                builder.AddAttribute(++count, nameof(FlowPanel.OnSubmit), EventCallback.Factory.Create(this, OnSubmit));
                builder.AddAttribute(++count, nameof(FlowPanel.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
                if (AdditionalFields != null)
                {
                    builder.AddContent(++count, AdditionalFields);
                }
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
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnSubmit), EventCallback.Factory.Create(this, OnSubmit));
                builder.AddAttribute(++count, nameof(FlowTabsBuilder.OnClose), EventCallback.Factory.Create(this, OnClose));
                builder.CloseComponent();
            }
        };
    }

    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _needsRendering = false;
        return base.OnAfterRenderAsync(firstRender);
    }


    protected async Task OnSubmit()
    {
        await this.Save();
    }

    protected void OnClose()
    {
        this.Cancel();
    }

    protected async Task Save()
    {
        if (ReadOnly)
        {
            Logger.Instance.ILog("Cannot save, readonly");
            return;
        }
        
        bool valid = await this.Validate();
        if (valid == false)
            return;

        if (SaveCallback != null)
        {
            bool saved = await SaveCallback(this.Model);
            if (saved == false)
                return;
        }
        OpenTask?.TrySetResult((true, this.Model));

        this.Visible = false;
        this.Fields?.Clear();
        this.Tabs?.Clear();
        this.OnClosed?.Invoke();
    }

    protected async void Cancel()
    {
        if(OnCancel != null)
        {
            bool result = await OnCancel.Invoke();
            if (result == false)
                return;
        }

        if (PromptUnsavedChanges && ReadOnly == false)
        {
            string currentModelJson = ModelToJsonForCompare(Model);
            if(currentModelJson != CleanModelJson)
            {
                Logger.Instance.ILog("CleanModelJson");
                Logger.Instance.ILog(CleanModelJson);
                Logger.Instance.ILog("currentModelJson");
                Logger.Instance.ILog(currentModelJson);
                bool confirmResult = await Message.Confirm("Labels.Confirm", "Labels.CancelMessage");
                if(confirmResult == false)
                    return;
            }
        }

        OpenTask?.TrySetResult((false, null));
        this.Visible = false;
        if(this.Fields != null)
            this.Fields.Clear();
        if(this.Tabs != null)
            this.Tabs.Clear();

        await this.WaitForRender();
        this.OnClosed?.Invoke();
    }

    /// <summary>
    /// Closes the editor, intended to be called by an external source
    /// E.g. to allow something that opened the editor, the ability to close it outside of it
    /// </summary>
    public async Task Closed()
    {
        OpenTask?.TrySetCanceled();
        this.Visible = false;
        if(this.Fields != null)
            this.Fields.Clear();
        if(this.Tabs != null)
            this.Tabs.Clear();

        await this.WaitForRender();
        this.OnClosed?.Invoke();
    }

    /// <summary>
    /// Opens the help URL if set
    /// </summary>
    protected void OpenHelp()
    {
        if (string.IsNullOrWhiteSpace(HelpUrl))
            return;
        _ = App.Instance.OpenHelp(HelpUrl.ToLowerInvariant());
    }

    private async Task DoDownload()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl))
            return;
        
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", DownloadUrl);
    }

    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }

    /// <summary>
    /// Triggers state has changed on the editor
    /// </summary>
    public void TriggerStateHasChanged() => StateHasChanged();

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}
