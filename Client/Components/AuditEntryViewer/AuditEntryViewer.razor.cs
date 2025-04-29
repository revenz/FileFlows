using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Audit entry viewer
/// </summary>
public partial class AuditEntryViewer
{
    /// <summary>
    /// Gets the static instance of the audit entry viewrt
    /// </summary>
    public static AuditEntryViewer Instance { get;private set; }
    
    /// <summary>
    /// Gets or sets the blocker tho show
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    TaskCompletionSource ShowTask;
    /// <summary>
    /// The entry to render
    /// </summary>
    private AuditEntry Entry;
    /// <summary>
    /// If the viewer is visible or not
    /// </summary>
    private bool Visible;
    /// <summary>
    /// The close label
    /// </summary>
    private string lblClose;
    /// <summary>
    /// If the component is waiting a render
    /// </summary>
    private bool AwaitingRender = false;
    /// <summary>
    /// The table instance
    /// </summary>
    private FlowTable<EntryViewerData> Table { get; set; }
    /// <summary>
    /// The table data
    /// </summary>
    private List<EntryViewerData> Data = new();
    /// <summary>
    /// The title of the viewer
    /// </summary>
    private string lblTitle;
    /// <summary>
    /// The label to show in the value column
    /// </summary>
    private string lblValue;

    /// <summary>
    /// The label to show in the field column
    /// </summary>
    private string lblField;

    /// <summary>
    /// Constructs a new instance of the Audit entry viewer component
    /// </summary>
    public AuditEntryViewer()
    {
        Instance = this;
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.Instant("Dialogs.AuditEntry.ViewerTitle");
        lblValue = Translater.Instant("Dialogs.AuditEntry.Columns.Value");
        lblField = Translater.Instant("Dialogs.AuditEntry.Columns.Field");
    }
    
    /// <summary>
    /// Closes the viewer
    /// </summary>
    private void Close()
    {
        this.Visible = false;
        Entry = null;
        this.ShowTask.SetResult();
    }

    /// <summary>
    /// Shows the audit entry changes
    /// </summary>
    /// <param name="entry">the entry to view</param>
    /// <returns>a task to await</returns>
    public Task Show(AuditEntry entry)
    {
        if (entry.Changes?.Any() != true)
        {
            feService.Notifications.ShowWarning("Labels.NoChangedDetected");
            return Task.CompletedTask;
        }
        
        this.Entry = entry;
        Instance.ShowTask = new ();
        Visible = true;
        _ = ShowActual(entry);
        
        return Instance.ShowTask.Task;
    }

    /// <summary>
    /// Performs the actual show
    /// </summary>
    /// <param name="entry">the entry to view</param>
    private async Task ShowActual(AuditEntry entry)
    {
        Data = entry.Changes.Select(x => new EntryViewerData()
        {
            Name = x.Key,
            Value = x.Key == "Parts" ? RenderParts(x.Value.ToString()) : x.Value
        }).ToList();

        await AwaitRender();
        
        Table?.SetData(Data);
        await AwaitRender();
    }


    /// <summary>
    /// Waits for the component to re-render
    /// </summary>
    private async Task AwaitRender()
    {
        AwaitingRender = true;
        this.StateHasChanged();
        await Task.Delay(10);
        while (AwaitingRender)
            await Task.Delay(10);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (AwaitingRender)
            AwaitingRender = false;
    }

    private MarkupString RenderParts(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new MarkupString(string.Empty);
        List<string> lines = new();
        Regex fromTo = new Regex(@"([^:]+):\s*'([^']+)'\s+to\s+'([^']+)'$");
        foreach (var line in value.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                lines.Add(string.Empty);
                continue;
            }

            var fromToMatch = fromTo.Match(line);
            if (fromToMatch.Success)
            {
                lines.Add("<b>" + HttpUtility.HtmlEncode(fromToMatch.Groups[1].Value) + ":</b> " +
                          "<b>'</b>" + HttpUtility.HtmlEncode(fromToMatch.Groups[2].Value) + "<b>'</b> to " +
                          "<b>'</b>" + HttpUtility.HtmlEncode(fromToMatch.Groups[3].Value) + "<b>'</b>");
                continue;
            }

            int index = line.IndexOf(":", StringComparison.Ordinal);
            if (index > 0)
            {
                lines.Add("<b>" + HttpUtility.HtmlEncode(line[..index]) + ":</b> " +
                          HttpUtility.HtmlEncode(line[(index + 1)..]));
                continue;
            }
            lines.Add(HttpUtility.HtmlEncode(line));
        }

        return new MarkupString(string.Join("<br/>", lines));
    }
    
    /// <summary>
    /// Gets or sets if this is maximised
    /// </summary>
    protected bool Maximised { get; set; }
    /// <summary>
    /// Maximises the viewer
    /// </summary>
    /// <param name="maximised">true to maximise otherwise false to return to normal</param>
    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }

    /// <summary>
    /// Entry viewer data
    /// </summary>
    private class EntryViewerData
    {
        /// <summary>
        /// Gets the name of the value
        /// </summary>
        public string Name { get; init; }
        /// <summary>
        /// Gets the value
        /// </summary>
        public object Value { get; init; }
    }
}
