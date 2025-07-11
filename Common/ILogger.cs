using System.Text;

namespace FileFlows.Common;

/// <summary>
/// Logging interface used to print log messages
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a information message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void ILog(params object[] args);
    
    /// <summary>
    /// Logs a message with no prefix
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void Raw(params object[] args);
    
    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void DLog(params object[] args);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void WLog(params object[] args);

    /// <summary>
    /// Logs a error message
    /// </summary>
    /// <param name="args">the arguments for the log message</param>
    void ELog(params object[] args);

    /// <summary>
    /// Builds and prints a list of objects as a table to the log, with column headers and aligned formatting.
    /// </summary>
    /// <typeparam name="T">The type of the objects in the collection.</typeparam>
    /// <param name="items">The list of objects to display in the table.</param>
    /// <param name="title">Optional title printed above the table.</param>
    /// <param name="propertyNames">
    /// Optional list of property names to display in the table. If not provided, all public properties are shown in declared order.
    /// </param>
    void Table<T>(IEnumerable<T> items, string? title = null, string[]? propertyNames = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        var type = typeof(T);
        var props = (propertyNames?.Length > 0
                ? propertyNames.Select(p => type.GetProperty(p)).Where(p => p != null)
                : type.GetProperties()
            ).ToList();

        if (props.Count == 0)
        {
            sb.AppendLine("No properties to display.");
            ILog(sb.ToString());
            return;
        }

        var headers = props.Select(p => p!.Name).ToList();
        var colWidths = headers.Select(h => h.Length).ToArray();

        var rows = items.Select(item =>
            props.Select(p =>
            {
                var val = p!.GetValue(item)?.ToString() ?? "";
                return val;
            }).ToList()
        ).ToList();

        for (int col = 0; col < colWidths.Length; col++)
        {
            foreach (var row in rows)
            {
                colWidths[col] = Math.Max(colWidths[col], row[col].Length);
            }
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            sb.AppendLine(title);
        }

        var headerLine = "| " + string.Join(" | ", headers.Select((h, i) => h.PadRight(colWidths[i]))) + " |";
        var separator = "|-" + string.Join("-|-", colWidths.Select(w => new string('-', w))) + "-|";

        sb.AppendLine(headerLine);
        sb.AppendLine(separator);

        foreach (var row in rows)
        {
            var line = "| " + string.Join(" | ", row.Select((val, i) => val.PadRight(colWidths[i]))) + " |";
            sb.AppendLine(line);
        }

        sb.AppendLine();
        ILog(sb.ToString());
    }

    /// <summary>
    /// Logs a section to the log file that contains a header
    /// </summary>
    /// <param name="header">the head section</param>
    /// <param name="content">the content</param>
    /// <param name="logType">the type of log message</param>
    void Section(string header, string content, LogType logType = LogType.Info)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        const int totalWidth = 120;

        // Construct padded header
        string paddedHeader = $" {header} ";
        int headerLength = paddedHeader.Length;

        if (headerLength >= totalWidth)
            paddedHeader = paddedHeader[..totalWidth];

        int dashCount = totalWidth - paddedHeader.Length;
        int dashLeft = dashCount / 2;
        int dashRight = dashCount / 2;

        // Add 1 to right side if the dash count is odd
        if (dashCount % 2 != 0)
            dashRight += 1;

        string topLine = new string('-', dashLeft) + paddedHeader + new string('-', dashRight);
        string bottomLine = new string('-', totalWidth);

        string complete = $"\n{topLine}\n{content}\n{bottomLine}";
        switch (logType)
        {
            case LogType.Warning:
                WLog(complete);
                break;
            case LogType.Error:
                ELog(complete);
                break;
            case LogType.Debug:
                DLog(complete);
                break;
            default:
                ILog(complete);
                break;
        }
    }

    /// <summary>
    /// Gets the last number of log lines
    /// </summary>
    /// <param name="length">The maximum number of lines to grab</param>
    /// <returns>The last number of log lines</returns>
    string GetTail(int length = 50);
}

/// <summary>
/// The type of log message
/// </summary>
public enum LogType
{
    /// <summary>
    /// A error message
    /// </summary>
    Error, 
    /// <summary>
    /// a warning message
    /// </summary>
    Warning,
    /// <summary>
    /// A informational message
    /// </summary>
    Info,
    /// <summary>
    /// A debug message
    /// </summary>
    Debug,
    /// <summary>
    /// A raw message with no prefix
    /// </summary>
    Raw
}
