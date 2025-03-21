namespace FileFlows.Client.Services.Frontend.Handlers;

/// <summary>
/// Report related data
/// </summary>
/// <param name="feService">the front end service</param>
public class ReportHandler(FrontendService feService)
{
    /// <summary>
    /// Gets or sets the report definitions
    /// </summary>
    public List<ReportDefinition> ReportDefinitions { get; private set; } = [];
    /// <summary>
    /// Gets or sets the scheduled reports
    /// </summary>
    public List<ScheduledReport> ScheduledReports { get; private set; } = [];
    
    /// <summary>
    /// Event raised when the node status is updated
    /// </summary>
    public event Action<List<ScheduledReport>> ScheduledReportsUpdated; 

    /// <summary>
    /// Initializes the handler
    /// </summary>
    /// <param name="data">the initial data</param>
    public void Initialize(InitialClientData data)
    {
        ReportDefinitions = data.ReportDefinitions;
        ScheduledReports = data.ScheduledReports;
        feService.Registry.Register<List<ScheduledReport>>("ScheduledReports", (ed) =>
        {
            ScheduledReports = ed;
            ScheduledReportsUpdated?.Invoke(ed);
        });
    }
}