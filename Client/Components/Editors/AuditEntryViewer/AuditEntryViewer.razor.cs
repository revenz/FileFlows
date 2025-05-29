using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Audit entry viewer
/// </summary>
public partial class AuditEntryViewer : ModalEditor
{
    /// <summary>
    /// The entry to render
    /// </summary>
    private AuditEntry Entry;
    /// <summary>
    /// The table data
    /// </summary>
    private List<EntryViewerData> Data = new();
    /// <summary>
    /// The label to show in the value column
    /// </summary>
    private string lblValue;

    /// <summary>
    /// The label to show in the field column
    /// </summary>
    private string lblField;

    private ModalEditorWrapper? Wrapper;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ReadOnly = true; // we dont have a save button

        if (Options is AuditEntryViewerOptions options == false)
        {
            Close();
            return;
        }
        
        if (options.Entry.Changes?.Any() != true)
        {
            feService.Notifications.ShowWarning("Labels.NoChangedDetected");
            Close();
            return;
        }
        
        Entry = options.Entry;
        Data = Entry.Changes.Select(x => new EntryViewerData()
        {
            Name = x.Key,
            Value = x.Key == "Parts" ? RenderParts(x.Value.ToString()) : x.Value
        }).ToList();

        lblClose = Translater.Instant("Labels.Close");
        Title = Translater.Instant("Dialogs.AuditEntry.ViewerTitle");
        lblValue = Translater.Instant("Dialogs.AuditEntry.Columns.Value");
        lblField = Translater.Instant("Dialogs.AuditEntry.Columns.Field");
        StateHasChanged();
        await Task.Delay(50); // fixes rendering issue
        StateHasChanged();
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

/// <summary>
/// Options for configuring the Audit Entry Viewer
/// </summary>
public class AuditEntryViewerOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets the audit entry
    /// </summary>
    public AuditEntry Entry { get; set; } = null!;
}