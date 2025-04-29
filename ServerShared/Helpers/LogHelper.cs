using System.Globalization;
using System.Text.Json;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Helper for log messages
/// </summary>
public class LogHelper
{
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    public static string FormatMessage(LogType type, params object[] args)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string prefix = type switch
        {
            LogType.Info => $"{date} [INFO] -> ",
            LogType.Error => $"{date} [ERRR] -> ",
            LogType.Warning => $"{date} [WARN] -> ",
            LogType.Debug => $"{date} [DBUG] -> ",
            _ => string.Empty
        };

        string text = string.Join(
            ", ", args.Select(x =>
            {
                if (x == null)
                    return "null";
                if (x.GetType().IsPrimitive)
                    return x.ToString();
                if (x is string str)
                    return str;
                if (x is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.True)
                        return "true";
                    if (je.ValueKind == JsonValueKind.False)
                        return "false";
                    if (je.ValueKind == JsonValueKind.String)
                        return je.GetString();
                    if (je.ValueKind == JsonValueKind.Number)
                        return je.GetInt64().ToString();
                    return je.ToString();
                }

                return JsonSerializer.Serialize(x);
            }));

        string message = prefix + text;
        if (message.IndexOf((char)0) >= 0)
        {
            message = message.Replace(new string((char)0, 1), string.Empty);
        }

        return message;
    }
}