using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Audit history popup
/// </summary>
public partial class AuditHistory : ModalEditor
{
    /// <summary>
    /// The UID of the object being audited
    /// </summary>
    private Guid Uid;
    /// <summary>
    /// The type of object being audited
    /// </summary>
    private string Type;
    /// <summary>
    /// Data in the table
    /// </summary>
    private List<AuditEntry> Data = new ();
    /// <summary>
    /// The table instance
    /// </summary>
    public FlowTable<AuditEntry> Table { get; set; }
    /// <summary>
    /// The column titles
    /// </summary>
    private string lblDate, lblAction, lblOperator, lblIPAddress, lblSummary;
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }

    private ModalEditorWrapper? Wrapper;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ReadOnly = true; // we dont have a save button

        if (Options is AuditHistoryOptions options == false)
        {
            Close();
            return;
        }
        
        Uid = options.Uid;
        Type = options.Type;
        Title = Translater.Instant("Labels.Audit");
        lblDate = Translater.Instant("Pages.Audit.Columns.Date"); 
        lblAction = Translater.Instant("Pages.Audit.Columns.Action");
        lblOperator = Translater.Instant("Pages.Audit.Columns.Operator");
        lblIPAddress = Translater.Instant("Pages.Audit.Columns.IPAddress");
        lblSummary = Translater.Instant("Pages.Audit.Columns.Summary");
        
        
        Wrapper?.ShowBlocker();
        var response = await HttpHelper.Get<AuditEntry[]>($"/api/audit/{options.Type}/{options.Uid}");

        if (response.Data?.Any() != true)
        {
            feService.Notifications.ShowWarning(Translater.Instant("Labels.NoAuditHistoryAvailable"));
            Close();
            return;
        }
        
        if (response.Data.First().Parameters.TryGetValue("Name", out var oName))
            Title = oName.ToString();

        foreach (var d in response.Data)
        {
            d.Parameters ??= new();
            if(string.IsNullOrEmpty(d.ObjectType) == false)
                d.Parameters["Type"] = d.ObjectType[(d.ObjectType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize();
            d.Parameters["User"] = d.OperatorName;
            d.Summary = Translater.Instant($"AuditActions.{d.Action}", d.Parameters);
        }
            
        Data = response.Data.ToList();
        
        Wrapper?.HideBlocker();
    }
    
    /// <summary>
    /// Views the object
    /// </summary>
    /// <param name="entry">the audit entry</param>
    private async Task View(AuditEntry entry)
    {
        if (entry?.Changes?.Any() != true)
            return;
        await ModalService.ShowModal<AuditEntryViewer>(new AuditEntryViewerOptions()
        {
            Entry = entry,
        });
    } 
}

/// <summary>
/// Options for configuring and displaying the audit history modal.
/// </summary>
public class AuditHistoryOptions : IModalOptions
{
    /// <summary>
    /// Gest or sets the UID of the model being opened
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the type of object being audited
    /// </summary>
    public string Type { get; set; } = string.Empty;
}