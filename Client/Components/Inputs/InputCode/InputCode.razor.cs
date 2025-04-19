using Microsoft.AspNetCore.Components;
using BlazorMonaco;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

public partial class InputCode : Input<string>, IDisposable
{
    private bool Updating = false;
    private string InitialValue;
    
    private MonacoEditor CodeEditor { get; set; }
    
    /// <summary>
    /// Gets or sets the message service
    /// </summary>
    [Inject] MessageService Message { get; set; }
    
    /// <summary>
    /// Gets or sets the language the editor is editing
    /// </summary>
    [Parameter] public string Language { get; set; }

    [Parameter] public Dictionary<string, object> Variables { get; set; } = new();
    
    private IJSObjectReference jsInputCode;
    
    private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    {
        var language = Language?.EmptyAsNull() ?? "javascript";
        if (language?.ToLowerInvariant() == "js")
            language = "javascript";
        else if (language?.ToLowerInvariant() == "sh")
            language = "shell";
        
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Minimap = new EditorMinimapOptions { Enabled = false },
            Theme = "vs-dark",
            Language = language,
            Value = this.Value?.Trim() ?? "",
            ReadOnly = this.Editor.ReadOnly
        };
    }

    private void OnEditorInit(MonacoEditorBase e)
    {
        _ = Task.Run(async () =>
        {
            var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./Components/Inputs/InputCode/InputCode.razor.js?v={Globals.Version}");
            jsInputCode = await jsObjectReference.InvokeAsync<IJSObjectReference>("createInputCode", new object[] { DotNetObjectReference.Create(this) });
            if (Language == "shell")
            {
                await jsInputCode.InvokeVoidAsync("shellEditor");
            }
            else
            {
                await jsInputCode.InvokeVoidAsync("jsEditor", Variables,
                    null); //, shared.Success ? shared.Data : null);
            }
        });
        InitialValue = this.Value;
    }

    protected override void OnInitialized()
    {
        this.InitialValue = Value;
        this.Editor.OnCancel += Editor_OnCancel;
        this.Editor.OnClosed += Editor_Closed;
        
        jsInputCode.InvokeVoidAsync("codeCaptureSave");
        base.OnInitialized();
    }

    /// <summary>
    /// Saves the code
    /// </summary>
    [JSInvokable]
    public async Task SaveCode()
    {
        // first update the code
        this.Updating = true;
        this.Value = await CodeEditor.GetValue();
        this.Updating = false;
        await this.OnSubmit.InvokeAsync();
    }

    private Task Editor_Closed()
    {
        Logger.Instance.ILog("Editor_Closed!");
        this.Editor.OnCancel -= Editor_OnCancel;
        this.Editor.OnClosed -= Editor_Closed;
        return Task.CompletedTask;
    }

    private async Task<bool> Editor_OnCancel()
    {
        this.Updating = true;
        this.Value = await CodeEditor?.GetValue();
        this.Updating = false;
        if (this.InitialValue?.Trim()?.EmptyAsNull() != this.Value?.Trim()?.EmptyAsNull())
        {
            bool cancel = await Message.Confirm(Translater.Instant("Labels.Confirm"), Translater.Instant("Labels.CancelMessage"));
            if (cancel == false)
                return false;
        }
        return true;
    }

    private void OnBlur()
    {
        _ = Task.Run(async () =>
        {
            this.Updating = true;
            this.Value = await CodeEditor.GetValue();
            this.Updating = false;
        });
    }

    protected override void ValueUpdated()
    {
        Logger.Instance.ILog("Updating code 0", this.Updating, this.Value);
        if (this.Updating)
            return;
        if (string.IsNullOrEmpty(this.Value) || CodeEditor == null)
            return;
        Logger.Instance.ILog("Updating code", this.Value);
        CodeEditor.SetValue(this.Value.Trim());
    }
    
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" && e.ShiftKey)
        {
            // for code the shortcut to submit is shift enter

            // need to get value in code block
            this.Updating = true;
            this.Value = await CodeEditor.GetValue();
            this.Updating = false;

            await OnSubmit.InvokeAsync();
        }
        // else if (e.Code == "Escape")
        // {
        //     await OnClose.InvokeAsync();
        // }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        jsInputCode.InvokeVoidAsync("codeUncaptureSave");
        if (this.Editor != null)
        {
            this.Editor.OnCancel -= Editor_OnCancel;
            this.Editor.OnClosed -= Editor_Closed;
        }
    }

    /// <summary>
    /// Adds imports to the current code
    /// </summary>
    /// <param name="imports">the imports to add</param>
    public async Task AddImports(List<string> imports)
    {
        bool changed = false;
        string code = await CodeEditor.GetValue() ?? string.Empty;
        foreach (var item in imports)
        {
            string import = $"import {{ {item} }} from 'Shared/{item}'";
            if (code.IndexOf(import, StringComparison.Ordinal) >= 0)
                continue;
            code = import + "\n" + code;
            changed = true;
        }

        if(changed)
            this.Value = code;
    }
    
    /// <summary>
    /// Gets the code from the editor
    /// </summary>
    /// <returns>the code from the editor</returns>
    public Task<string> GetCode() => CodeEditor.GetValue() ; 
}
