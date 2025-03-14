using System.Threading;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.ServerShared.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for access control 
/// </summary>
public partial class Audit : ComponentBase
{
    private SemaphoreSlim fetching = new(1);
    /// <summary>
    /// Gets or sets the table instance
    /// </summary>
    protected FlowTable<AuditEntry> Table { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }

    /// <summary>
    /// The data shown
    /// </summary>
    List<AuditEntry> Data = new List<AuditEntry>();
    /// <summary>
    /// If this component needs rendering
    /// </summary>
    private bool _needsRendering = false;

    /// <summary>
    /// The search filter
    /// </summary>
    private AuditSearchFilter Filter = new();
    /// <summary>
    /// The column titles
    /// </summary>
    private string lblTitle, lblDate, lblType, lblAction, lblOperator, lblIPAddress, lblSummary;

    private Dictionary<AuditAction, string> AuditActionTranslations = new();
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        if (Profile.LicensedFor(LicenseFlags.Auditing) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        lblTitle = Translater.Instant("Pages.Audit.Title");
        lblDate = Translater.Instant("Pages.Audit.Columns.Date"); 
        lblType = Translater.Instant("Pages.Audit.Columns.Type");
        lblAction = Translater.Instant("Pages.Audit.Columns.Action");
        lblOperator = Translater.Instant("Pages.Audit.Columns.Operator");
        lblIPAddress = Translater.Instant("Pages.Audit.Columns.IPAddress");
        lblSummary = Translater.Instant("Pages.Audit.Columns.Summary");
        foreach (var action in Enum.GetValues<AuditAction>())
            AuditActionTranslations[action] = Translater.Instant($"Enums.{nameof(AuditAction)}.{action}"); 
        _ = Load();
    }
    
    /// <summary>
    /// Waits for a render to occur
    /// </summary>
    async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }


    public virtual async Task Load()
    {
        Blocker.Show("Loading Data");
        await this.WaitForRender();
        try
        {
            await fetching.WaitAsync();
            var result = await HttpHelper.Post<List<AuditEntry>>("/api/audit", Filter);
            if (result.Success)
            {
                foreach (var d in result.Data)
                {
                    if (d.ObjectType?.EndsWith(".Settings") == true)
                    {
                        d.Summary = Translater.Instant("AuditActions.SettingsUpdated");
                        continue;
                    }

                    d.Parameters ??= new();
                    if (string.IsNullOrEmpty(d.ObjectType) == false)
                    {
                        d.Parameters["Type"] = d.ObjectType.Contains(".Plugin")
                            ? "Plugin"
                            : d.ObjectType[(d.ObjectType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize();
                    }

                    d.Parameters["User"] = d.OperatorName;
                    d.Summary = Translater.Instant($"AuditActions.{d.Action}", d.Parameters);
                }
                this.Data = result.Data;
                if (Table != null)
                    SetTableData(this.Data);
            }
        }
        finally
        {
            fetching.Release();
            Blocker.Hide();
            await this.WaitForRender();
        }
    }
    
    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected virtual void SetTableData(List<AuditEntry> data) => Table?.SetData(data, clearSelected: false);
    
    

    /// <summary>
    /// Views the object
    /// </summary>
    /// <param name="entry">the audit entry</param>
    private async Task View(AuditEntry entry)
    {
        if (entry?.Changes?.Any() != true)
            return;
        await AuditEntryViewer.Instance.Show(entry);
    }

    /// <summary>
    /// Gets the type name to show
    /// </summary>
    /// <param name="fullname">the fullname of the type</param>
    /// <returns>the type name</returns>
    private string GetTypeName(string fullname)
    {
        if (string.IsNullOrWhiteSpace(fullname))
            return string.Empty;
        string name = fullname.Split('.').Last();
        if (name.StartsWith("Plugin", StringComparison.InvariantCulture))
            name = "Plugin";
        if(Translater.CanTranslate($"Pages.{name}.Title", out string pageTitle))
            return pageTitle;
        return name.Humanize();
    }

    /// <summary>
    /// Gets the audit action name
    /// </summary>
    /// <param name="action">the action</param>
    /// <returns>the audit action name</returns>
    private string GetAuditActionName(AuditAction action)
    {
        if (AuditActionTranslations.TryGetValue(action, out var name))
            return name;
        return action.ToString();
    }
}