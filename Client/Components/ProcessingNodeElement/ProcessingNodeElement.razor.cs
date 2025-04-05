using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Processing node
/// </summary>
public partial class ProcessingNodeElement : ComponentBase
{
    /// <summary>
    /// Gets or sets the processing node
    /// </summary>
    [Parameter] public NodeStatusSummary Node { get; set; }
    
    /// <summary>
    /// Gets or sets if this is rendered on the list page
    /// </summary>
    [Parameter] public bool ListPage { get; set; }
    
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblSchedule, lblLastSeen, lblOperatingSystem, lblArchitecture, lblMemory, lblVersion, lblStatus, 
        lblInternalProcessingNode, lblRunners, lblPriority, lblMomemtsAgo;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblSchedule = Translater.Instant("Labels.Schedule");
        lblLastSeen = Translater.Instant("Labels.LastSeen");
        lblOperatingSystem = Translater.Instant("Labels.OperatingSystem");
        lblArchitecture = Translater.Instant("Labels.Architecture");
        lblMemory = Translater.Instant("Labels.Memory");
        lblVersion = Translater.Instant("Labels.Version");
        lblStatus = Translater.Instant("Labels.Status");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblPriority = Translater.Instant("Pages.ProcessingNode.Fields.Priority");
        lblInternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
        lblMomemtsAgo = Translater.Instant("Times.MomentsAgo");
    }

    /// <summary>
    /// Gets the default icon
    /// </summary>
    /// <param name="item">the node</param>
    /// <returns>the default icon</returns>
    private string GetDefaultIcon(NodeStatusSummary item)
    {
        if (item.OperatingSystem == OperatingSystemType.Mac)
            return "svg:apple";
        if (item.OperatingSystem == OperatingSystemType.Docker)
            return "svg:docker";
        if (item.HardwareInfo?.OperatingSystem?.Contains("ubuntu", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/ubuntu";
        if (item.HardwareInfo?.OperatingSystem?.Contains("fedora", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/fedora";
        if (item.HardwareInfo?.OperatingSystem?.Contains("pop!", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/popos";
        if (item.HardwareInfo?.OperatingSystem?.Contains("debian", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/debian";
        if (item.HardwareInfo?.OperatingSystem?.Contains("arch", StringComparison.InvariantCultureIgnoreCase) == true)
            return "svg:distros/arch";
        if (item.OperatingSystem == OperatingSystemType.Windows)
            return "svg:windows";
        if (item.OperatingSystem == OperatingSystemType.Linux)
            return "svg:linux";
        return "fas fa-desktop";
    }

    /// <summary>
    /// Called when the enable state changes
    /// </summary>
    /// <param name="state">the new state</param>
    private void EnableChanged(bool state)
    {
        if(Node.Enabled == state)
            return;
        Node.Enabled = state;
        _ = HttpHelper.Put<ProcessingNode>($"/api/node/state/{Node.Uid}?enable={state}");
    }

    /// <summary>
    /// Returns the node last seen as human readable
    /// </summary>
    /// <param name="nodeLastSeen">the UTC date when it was last seen</param>
    /// <returns>human readable last seen</returns>
    private string FormatLastSeen(DateTime nodeLastSeen)
    {
        // Get the current time in UTC
        DateTime now = DateTime.UtcNow;

        // Calculate the time difference
        TimeSpan timeDifference = now - nodeLastSeen;

        // If less than 60 seconds ago
        if (timeDifference.TotalSeconds < 360)
            return lblMomemtsAgo;
    
        // If less than 60 minutes ago
        if (timeDifference.TotalMinutes < 60)
            return Translater.Instant("Times.MinutesAgo", new { num = (int)timeDifference.TotalMinutes });
    
        // If less than 24 hours ago
        if (timeDifference.TotalHours < 24)
            return Translater.Instant("Times.HoursAgo", new { num = (int)timeDifference.TotalHours });
    
        // If less than 7 days ago
        if (timeDifference.TotalDays <= 7)
            return Translater.Instant("Times.DaysAgo", new { num = (int)timeDifference.TotalDays });
    
        // If more than 7 days ago, show the local date
        return nodeLastSeen.ToLocalTime().ToShortDateString();
    }
}