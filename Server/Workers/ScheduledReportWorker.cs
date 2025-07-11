using FileFlows.Managers;
using FileFlows.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that sends the scheduled reports
/// </summary>
public class ScheduledReportWorker:ServerWorker
{
    
    /// <summary>
    /// Initializes a new instance of the scheduled report worker
    /// </summary>
    public ScheduledReportWorker() : base (ScheduleType.Hourly, 1)
    {
        Trigger();
    }

    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        if (LicenseService.IsLicensed(LicenseFlags.Reporting) == false)
            return; // not licensed

        _ = ExecuteAsync();
    }

    /// <summary>
    /// Executes scheduled reports asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ExecuteAsync()
    {
        var service = ServiceLoader.Load<ScheduledReportService>();
        var reports = (await service.GetAll()).Where(x => x.Enabled).ToList();
        Logger.ILog("Scheduled Reports: " + reports.Count);
        if (reports.Count == 0)
            return;
            
        var manager = new ReportManager();
        DateTime startLocal, endLocal;

        foreach (var report in reports)
        {
#if(!DEBUG)
            if (report.LastSentUtc > DateTime.UtcNow.AddHours(-12))
                continue; // prevent the system being reboot and sending the same report multiple times
#endif
            bool forceSend = false;
            
            switch (report.Schedule)
            {
                case ReportSchedule.Daily:
                    startLocal = DateTime.Now.Date.AddDays(-1);
                    endLocal = DateTime.Now.Date.AddMilliseconds(-1);
                    forceSend = report.LastSentUtc < startLocal.ToUniversalTime().AddDays(-1);
                    Logger.ILog("Report: " +  report.Name + $" daily, {startLocal} to {endLocal}");
                    break;
                case ReportSchedule.Weekly:
                    if ((int)DateTime.Now.DayOfWeek != report.ScheduleInterval)
                        continue;
                    
                    startLocal = DateTime.Now.Date.AddDays(-7);
                    endLocal = DateTime.Now.Date.AddMilliseconds(-1);
                    forceSend = report.LastSentUtc < startLocal.ToUniversalTime().AddDays(-1);
                    Logger.ILog("Report: " +  report.Name + $" weekly, {startLocal} to {endLocal}, force send: {forceSend}");
                    break;
                case ReportSchedule.Monthly:
                    int currentDay = DateTime.Now.Day;
                    int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
    
                    if (report.ScheduleInterval <= daysInMonth)
                    {
                        // Trigger on the ScheduleInterval day of the month
                        if (currentDay != report.ScheduleInterval)
                            continue;
                    }
                    else
                    {
                        // If ScheduleInterval exceeds the days in the month, trigger on the last day of the month
                        if (currentDay != daysInMonth)
                            continue;
                    }
                    
                    int currentYear = DateTime.Now.Year;
                    int currentMonth = DateTime.Now.Month;
                    int previousMonth = currentMonth - 1;
                    if (previousMonth == 0) {
                        previousMonth = 12;
                        currentYear--;
                    }

                    int daysInPreviousMonth = DateTime.DaysInMonth(currentYear, previousMonth);
                    int daysInCurrentMonth = DateTime.DaysInMonth(DateTime.Now.Year, currentMonth);
                    int scheduleDay = Math.Min(report.ScheduleInterval, daysInPreviousMonth);
                    startLocal = new DateTime(currentYear, previousMonth, scheduleDay);

                    scheduleDay = Math.Min(report.ScheduleInterval, daysInCurrentMonth);
                    endLocal = new DateTime(DateTime.Now.Year, currentMonth, scheduleDay).AddMilliseconds(-1);
                    forceSend = report.LastSentUtc < startLocal.ToUniversalTime().AddDays(-1);
                    Logger.ILog("Report: " +  report.Name + $" monthly, {startLocal} to {endLocal}, force send: {forceSend}");
                    
                    break;
                default:
                    continue;
            }
#if(!DEBUG)
            if (forceSend == false && DateTime.Now.Hour != 1)
                return; // only run this at 1 am
#endif

            Dictionary<string, object> model = new();
            model["Flow"] = report.Flows;
            model["Node"] = report.Nodes;
            model["Tags"] = report.Tags;
            model["Library"] = report.Libraries;
            model["Direction"] = report.Direction;
            model["StartUtc"] = startLocal.ToUniversalTime();
            model["EndUtc"] = endLocal.ToUniversalTime();
            
            Logger.ILog($"Scheduled Report '{report.Name}' [{startLocal}] to [{endLocal}]");

            try
            {
                var result = await manager.Generate(report.Report.Uid, true, model);
                if (result.Failed(out var rError))
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record(NotificationSeverity.Warning, $"Scheduled Report '{report.Name}' failed to generate",
                            rError);
                    continue;
                }

                string html = result.Value;

                if (string.IsNullOrWhiteSpace(result.Value))
                    html = $"Your scheduled report <b>{report.Name}</b> was generated, but there was no data available for the selected criteria.";

                _ = service.Email(report.Report.Name, report.Recipients, report.Name, html);
            }
            catch (Exception ex)
            {
                Logger.WLog($"Failed running scheduled report '{report.Name}': {ex.Message}");
            }

            report.LastSentUtc = DateTime.UtcNow;
            await service.Update(report, null);
        }
    }
}