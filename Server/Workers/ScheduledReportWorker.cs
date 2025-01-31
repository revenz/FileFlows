using FileFlows.DataLayer.Reports;
using FileFlows.Managers;
using FileFlows.Server.Helpers;
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
        
        var service = ServiceLoader.Load<ScheduledReportService>();
        var reports = service.GetAll().Result.Where(x => x.Enabled).ToList();
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
                    break;
                case ReportSchedule.Weekly:
                    if ((int)DateTime.Now.DayOfWeek != report.ScheduleInterval)
                        continue;
                    
                    startLocal = DateTime.Now.Date.AddDays(-7);
                    endLocal = DateTime.Now.Date.AddMilliseconds(-1);
                    forceSend = report.LastSentUtc < startLocal.ToUniversalTime().AddDays(-1);
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
            model["Library"] = report.Libraries;
            model["Direction"] = report.Direction;
            model["StartUtc"] = startLocal.ToUniversalTime();
            model["EndUtc"] = endLocal.ToUniversalTime();
            
            Logger.Instance.ILog($"Scheduled Report '{report.Name}' [{startLocal}] to [{endLocal}]");

            try
            {
                var result = manager.Generate(report.Report.Uid, true, model).Result;
                if (result.Failed(out var rError))
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record(NotificationSeverity.Warning, $"Scheduled Report '{report.Name}' failed to generate",
                            rError);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(result.Value))
                {
                    _ = ServiceLoader.Load<INotificationService>()
                        .Record(NotificationSeverity.Information,
                            $"Scheduled Report '{report.Name}' had not matching data", rError);
                    return;
                }

                _ = service.Email(report.Report.Name, report.Recipients, report.Name, result.Value);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Failed running scheduled report '{report.Name}': {ex.Message}");
            }

            report.LastSentUtc = DateTime.UtcNow;
            service.Update(report, null).Wait();
        }
    }
}