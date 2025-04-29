using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using System.Timers;
using System.Web;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Log Page
/// </summary>
public partial class Log : ComponentBase
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    private string DownloadUrl;
    private bool scrollToBottom = false;

    private bool Searching = false;
    
    private Timer AutoRefreshTimer;
    
    private Dictionary<string, List<LogFile>> LoggingSources = new ();
    /// <summary>
    /// The selected source/key from the Logging Sources
    /// </summary>
    private string SelectedSource { get; set; }
    /// <summary>
    /// Gets or sets the log entries in the current log file being viewed
    /// </summary>
    private List<LogEntry> LogEntries { get; set; } = new();
    /// <summary>
    /// Gets or sets the log entries in the current log file being viewed
    /// </summary>
    private List<LogEntry> FilteredLogEntries { get; set; } = new();

    private List<MarkupString> FilteredLines = new();

    /// <summary>
    /// The active log file
    /// </summary>
    private LogFile? SearchFile;
    
    /// <summary>
    /// Gets the current log text
    /// </summary>
    private string? CurrentLogText;

    /// <summary>
    /// Gets or sets the search text
    /// </summary>
    private string SearchText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets if higher severity messages should be included
    /// </summary>
    public bool SearchIncludeHigherSeverity { get; set; } = true;
    /// <summary>
    /// Gets or sets the search severity
    /// </summary>
    public LogType SearchSeverity { get; set; } = LogType.Info;

    /// <summary>
    /// The active search model
    /// </summary>
    private LogSearchModel ActiveSearchModel;

    /// <summary>
    /// The error message if the search failed
    /// </summary>
    private string? ErrorMessage;

    /// <summary>
    /// If there is an error
    /// </summary>
    public bool HasError = false;

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblDownload, lblSearch, lblSearching, lblInfo, lblWarning, lblError, lblDebug, lblText, 
        lblIncludeHigherSeverity, lblSource, lblFile, lblSeverity, lblNodes, lblNoMatchingData;
    
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.Log.Title"), "fas fa-file-alt", noPadding: true);
        ActiveSearchModel = new()
        {
            Message = SearchText,
            Type = SearchSeverity,
            TypeIncludeHigherSeverity = SearchIncludeHigherSeverity
        };
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblSearching = Translater.Instant("Labels.Searching");
        this.lblDownload = Translater.Instant("Labels.Download");
        lblInfo = Translater.Instant("Enums.LogType.Info");
        lblWarning = Translater.Instant("Enums.LogType.Warning");
        lblError = Translater.Instant("Enums.LogType.Error");
        lblDebug = Translater.Instant("Enums.LogType.Debug"); 
        lblText = Translater.Instant("Pages.Log.Fields.Text"); 
        lblIncludeHigherSeverity = Translater.Instant("Pages.Log.Fields.IncludeHigherSeverity");
        lblSource = Translater.Instant("Pages.Log.Fields.Source");
        lblFile = Translater.Instant("Pages.Log.Fields.File");
        lblSeverity = Translater.Instant("Pages.Log.Fields.Severity");
        lblNodes = Translater.Instant("Pages.Log.Fields.Nodes");
        lblNoMatchingData = Translater.Instant("Pages.Log.Labels.NoMatchingData");
#if (DEBUG)
        this.DownloadUrl = "http://localhost:6868/api/fileflows-log/download";
#else
        this.DownloadUrl = "/api/fileflows-log/download";
#endif
        _ = Initialise();
    }
    
    async Task Initialise()
    {
        Blocker.Show();

        var logSource = await LocalStorage.GetItemAsync<string?>("LOG-Source");
        var logSeverity = await LocalStorage.GetItemAsync<LogType?>("LOG-Severity");
        if (logSeverity != null)
        {
            SearchSeverity = logSeverity.Value;
            ActiveSearchModel.Type = logSeverity.Value;
        }

        LoggingSources = (await HttpHelper.Get<Dictionary<string, List<LogFile>>>("/api/fileflows-log/log-sources")).Data;

        if (logSource != null && LoggingSources.ContainsKey(logSource))
            SelectedSource = logSource;
        else
            SelectedSource =  LoggingSources.Keys.FirstOrDefault();
        
        if (string.IsNullOrEmpty(SelectedSource) == false)
        {
            ActiveSearchModel.ActiveFile = LoggingSources[SelectedSource].First();
            SearchFile = LoggingSources[SelectedSource].First();
        }
        NavigationManager.LocationChanged += NavigationManager_LocationChanged!;
        
        await Refresh(true);
        
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed!;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        
        Blocker.Hide();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 100ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(400); // 500ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(200); // 700ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(300); // 1second
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
            });
        }
        if (scrollToBottom)
        {
            await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log"]);
            scrollToBottom = false;
        }
    }

    private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        Dispose();
    }

    /// <summary>
    /// Disposes of this page and its timer
    /// </summary>
    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed!;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }
    
    /// <summary>
    /// The timer elapsed
    /// </summary>
    /// <param name="sender">the sender who triggered this timer</param>
    /// <param name="e">the event arguments</param>
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (Searching || ActiveSearchModel.ActiveFile?.Active != true)
            return;
        
        _ = Refresh();
    }

    /// <summary>
    /// Performs a search
    /// </summary>
    async Task Search()
    {
        this.Searching = true;
        try
        {
            ActiveSearchModel.Message = SearchText;
            ActiveSearchModel.Type = SearchSeverity;
            ActiveSearchModel.TypeIncludeHigherSeverity = SearchIncludeHigherSeverity;
            ActiveSearchModel.ActiveFile = SearchFile;
            
            await LocalStorage.SetItemAsync("LOG-Source", SelectedSource);
            await LocalStorage.SetItemAsync("LOG-Severity", SearchSeverity);
            
            Blocker.Show(lblSearching);
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.Searching = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// The current filter, used to determine if new log lines should be appended or replace the existin
    /// </summary>
    private string CurrentFilter = string.Empty;

    async Task Refresh(bool forceScrollToBottom = false)
    {
        var filter =
            $"{ActiveSearchModel.ActiveFile}|{ActiveSearchModel.Type}|{ActiveSearchModel.TypeIncludeHigherSeverity}|{ActiveSearchModel.Message}";
        bool sameFilter = filter == CurrentFilter;
        CurrentFilter = filter;
        if (sameFilter || SearchFile?.Active == true)
        {
            HasError = false;
            ErrorMessage = null;
            var response = await HttpHelper.Get<string>("/api/fileflows-log/download?source=" +
                                                        HttpUtility.UrlEncode(ActiveSearchModel.ActiveFile.FileName));
            if (response.Success)
            {
                if (sameFilter && ActiveSearchModel.ActiveFile.Active && response.Body.Length > 0 && response.Body.Length >= CurrentLogText.Length)
                {
                    if (response.Body.Length == CurrentLogText.Length)
                        return; // no more log, nothing extra to do 
                    
                    string log = response.Body[CurrentLogText.Length..].TrimStart();
                    if (string.IsNullOrWhiteSpace(log) == false)
                    {
                        bool nearBottom = filter == CurrentFilter && ActiveSearchModel.ActiveFile.Active && LogEntries?.Any() == true && 
                                          await jsRuntime.InvokeAsync<bool>("ff.nearBottom", [".log-view .log"]);
                        
                        var newLines = SplitLog(log);
                        var newFiltered = FilterData(newLines);
                        if (newFiltered.Count > 0)
                        {
                            FilteredLogEntries.AddRange(newFiltered);
                            FilteredLines.AddRange(newFiltered.SelectMany(x => x.HtmlLines));
                        }

                        this.LogEntries.AddRange(newLines);
                
                        this.scrollToBottom = forceScrollToBottom || nearBottom;
                    }
                }
                else
                {
                    this.LogEntries = SplitLog(response.Data);
                    this.FilteredLogEntries = FilterData(LogEntries);
                    FilteredLines = FilteredLogEntries.SelectMany(x => x.HtmlLines).ToList();
                }
                
                if(ActiveSearchModel.ActiveFile.Active)
                    CurrentLogText = response.Body;
                this.StateHasChanged();
            }
            else
            {
                HasError = false;
                ErrorMessage = response.Body;
            }
        }
        else
        {
            this.FilteredLogEntries = FilterData(this.LogEntries);
            FilteredLines = FilteredLogEntries.SelectMany(x => x.HtmlLines).ToList();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Applies the filter
    /// </summary>
    private List<LogEntry> FilterData(List<LogEntry> entries)
    {
        bool hasSearchText = string.IsNullOrWhiteSpace(ActiveSearchModel.Message) == false;
        return entries.Where(x =>
        {
            if (x.Severity != ActiveSearchModel.Type)
            {
                if (ActiveSearchModel.TypeIncludeHigherSeverity == false)
                    return false;

                if ((int)x.Severity > (int)ActiveSearchModel.Type)
                    return false;
            }

            if (hasSearchText == false)
                return true;

            return x.Message.Contains(ActiveSearchModel.Message, StringComparison.InvariantCultureIgnoreCase);
        }).ToList();
    }

    /// <summary>
    /// Handles the active file selection change
    /// </summary>
    /// <param name="args">the change event arguments</param>
    private void HandleSelection(ChangeEventArgs args)
    {
        // Find the LogFile object corresponding to the selected ShortName
        SearchFile = LoggingSources.SelectMany(kv => kv.Value)
            .FirstOrDefault(file => file.FileName == args.Value?.ToString());
    }
    /// <summary>
    /// Handles the source selection chaning
    /// </summary>
    /// <param name="args">the change event arguments</param>
    private void HandleSourceSelection(ChangeEventArgs args)
    {
        SelectedSource = args.Value?.ToString();
        if (LoggingSources.TryGetValue(SelectedSource, out var list) == false)
            return;

        // Find the LogFile object corresponding to the selected ShortName
        SearchFile = list.FirstOrDefault();
    }

    
    /// <summary>
    /// Downloads the log
    /// </summary>
    private async Task DownloadLog()
    {
        var result = await HttpHelper.Get<string>(DownloadUrl + "?source=" + HttpUtility.UrlEncode(SearchFile.FileName));
        if (result.Success == false)
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Log.Labels.FailedToDownloadLog"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", SearchFile.FileName, result.Body);
    }
    
    /// <summary>
    /// Splits the log string into individual log entries.
    /// Each log entry includes the date, severity, and message.
    /// </summary>
    /// <param name="log">The complete log string to split.</param>
    /// <returns>A list of LogEntry objects representing individual log entries.</returns>
    public List<LogEntry> SplitLog(string log)
    {
        var logEntries = new List<LogEntry>();
    
        // Regex pattern to match the beginning of a log entry
        var entryPattern = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[([A-Z]+)\] (.*)$";
        var entryRegex = new Regex(entryPattern, RegexOptions.Multiline);

        var lines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        LogEntry? currentEntry = null;
        foreach (var line in lines)
        {
            var entryMatch = entryRegex.Match(line);
            if (entryMatch.Success)
            {
                // If a new log entry is found, add the current entry to the list
                if (currentEntry != null)
                {
                    SetHtmlLines(currentEntry);
                    logEntries.Add(currentEntry);
                }

                currentEntry = new LogEntry
                {
                    Date = entryMatch.Groups[1].Value.Trim()[11..], // remove date from string, only show time
                    Severity = entryMatch.Groups[2].Value.Trim().ToLowerInvariant() switch
                    {
                        "errr" => LogType.Error,
                        "warn" => LogType.Warning,
                        "dbug" => LogType.Debug,
                        _ => LogType.Info
                    },
                    SeverityText = entryMatch.Groups[2].Value.Trim(),
                    Message = entryMatch.Groups[3].Value.Trim()
                };
            }
            else if (currentEntry != null)
            {
                // Append the line to the current log entry's message
                currentEntry.Message += "\n" + line.TrimEnd();
            }
        }

        // Add the last entry to the list if it exists
        if (currentEntry != null)
        {
            SetHtmlLines(currentEntry);
            logEntries.Add(currentEntry);
        }

        return logEntries;

        void SetHtmlLines(LogEntry logLine)
        {
            string html = $@"<span class=""log-date"">{HttpUtility.HtmlEncode(logLine.Date)}</span> " +
                          $@"[<span class=""log-severity {logLine.Severity.ToString().ToLowerInvariant()}"">" +
                          $@"{logLine.SeverityText}</span>] <span class=""log-message"">{HttpUtility.HtmlEncode(logLine.Message.Replace("\r\n", "\n"))}</span>";
            logLine.HtmlLines = html.Split('\n').Select(x => (MarkupString)x).ToArray();
        }
    }
    
    /// <summary>
    /// Represents a log entry containing date, severity, and message.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the date and time of the log entry.
        /// </summary>
        public string Date { get; init; }

        /// <summary>
        /// Gets or sets the severity level of the log entry.
        /// </summary>
        public LogType Severity { get; init; }
        
        /// <summary>
        /// Gets or sets the severity text label
        /// </summary>
        public string SeverityText { get; init; }

        /// <summary>
        /// Gets or sets the message content of the log entry.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the HTML lines
        /// </summary>
        public MarkupString[] HtmlLines { get; set; }
    }
    
    
}




/// <summary>
/// A model used to search the log 
/// </summary>
public class LogSearchModel
{
    /// <summary>
    /// Gets or sets the file being searched
    /// </summary>
    public LogFile ActiveFile { get; set; }
    /// <summary>
    /// Gets or sets what to search for in the message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets what log type to search for
    /// </summary>
    public LogType Type { get; set; }
    
    /// <summary>
    /// Gets or sets if the search results should include log messages greater than the specified type
    /// </summary>
    public bool TypeIncludeHigherSeverity { get; set; }
}