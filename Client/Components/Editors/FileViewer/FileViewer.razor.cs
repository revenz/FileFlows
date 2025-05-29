using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// File Viewer
/// </summary>
public partial class FileViewer : ModalEditor
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    /// <summary>
    /// Gets or sets the model being edited
    /// </summary>
    public LibraryFile Model { get; set; }
    
    /// <summary>
    /// Gets or sets the log file
    /// </summary>
    private string Log { get; set; }
    
    /// <summary>
    /// Gets or sets the server log
    /// </summary>
    private string LogServer { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/files";

    private string LogUrl;

    private ActionButton[] AdditionalButtons = [];

    private List<KeyValuePair<string, string>> CustomVariables = [];
    private string Tags = string.Empty;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();;
        ReadOnly = true; // removes the save button
        Title = "File";
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        var uid = GetModelUid();
        
        var fileResult = await HttpHelper.Get<LibraryFile>("/api/library-file/" + uid);
        if (fileResult.Success == false || fileResult.Data == null)
        {
            Container.HideBlocker();
            Close();
        }
        
        Model = fileResult.Data;
        Title = Model.Additional?.DisplayName?.EmptyAsNull() ?? Model.RelativePath?.EmptyAsNull() ?? Model.Name;
        
        LogUrl = $"/api/library-file/{uid}/log";
        if (Model.Status == FileStatus.Processing)
            LogUrl += "?lines=5000";
        var taskLog = HttpHelper.Get<string>(LogUrl);
        var taskLogServer = HttpHelper.Get<string>($"/api/library-file/{uid}/server-log");
        
        await Task.WhenAll(taskLogServer, taskLog);

        List<ActionButton> buttons = Model.Status is FileStatus.Processed or FileStatus.ProcessingFailed
            ? new List<ActionButton>
            {
                new ()
                {
                    Label = "Labels.Reprocess",
                    Clicked = (_, _) => _ = Reprocess()
                }
            }
            : [];


        if (taskLog.Result.Success)
        {
            Log = taskLog.Result.Data;
            if (Model.Status != FileStatus.Processing && string.IsNullOrWhiteSpace(Log) == false)
            {
                buttons.Add(new()
                {
                    Label = "Labels.DownloadLog",
                    Clicked = (_, _) =>
                    {
                        _ = DownloadLog();
                    }
                });
            }
        }

        if(taskLogServer.Result.Success)
            LogServer = taskLogServer.Result.Data;
        

        CustomVariables = Model.CustomVariables?.Select(x =>
            new KeyValuePair<string, string>(x.Key, x.Value.ToString()))?.ToList() ?? [];
                
        if(Model.Node?.Name == "FileFlowsServer")
            Model.Node.Name = Translater.Instant("Labels.InternalProcessingNode");

        if (Model.Tags?.Any() == true && feService.Tag.Tags.Count > 0)
        {
            var known = feService.Tag.Tags.Where(x => Model.Tags.Contains(x.Uid)).Select(x => x.Name)
                .OrderBy(x => x.ToLowerInvariant())
                .ToList();
            Tags = string.Join(", ", known);
        }

        AdditionalButtons = buttons.ToArray();
        
        StateHasChanged();
    }

    /// <summary>
    /// Reprocess the file
    /// </summary>
    private async Task Reprocess()
    {
        var result = await ModalService.ShowModal<ReprocessDialog, bool>(new ReprocessOptions()
        {
            Files = [Model],
            ProcessOptionsMode = Model.Status == FileStatus.Unprocessed
        });
        if(result)
            Close();
    }
    
    /// <summary>
    /// Downloads the log
    /// </summary>
    private async Task DownloadLog()
    {
        var result = await HttpHelper.Get<string>($"/api/library-file/{Model.Uid}/log/download");
        if (result.Success == false)
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Log.Labels.FailedToDownloadLog"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", Model.Uid + ".log", result.Body);
    }
}