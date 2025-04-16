namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Log Viewer
/// </summary>
public partial class LogViewer : ModalEditor
{
    /// <summary>
    /// Gets or sets the log file
    /// </summary>
    private string Log { get; set; }

    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();;
        ReadOnly = true; // removes the save button
        Title = "Log";
    }

    /// <inheritdoc />
    public override async Task LoadModel()
    {
        string? log = (Options as ModalEditorOptions)?.Model as string;

        if (string.IsNullOrWhiteSpace(log))
        {
            Container.HideBlocker();
            Close();
            return;
        }

        Log = log;
        await Task.CompletedTask;
    }
}