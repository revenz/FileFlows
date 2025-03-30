using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner.JsonRpc.Handlers;

/// <summary>
/// Basic/General handler
/// </summary>
/// <param name="client">the JSON RPC Client</param>
public class BasicHandler(JsonRpcClient client)
{

    /// <summary>
    /// Retrieves the runner parameters from the server.
    /// </summary>
    /// <returns>The <see cref="RunnerParameters"/> containing the parameters.</returns>
    public async Task<RunnerParameters> GetRunnerParameters()
    {
        var result = await client.SendRequest<RunnerParameters>(nameof(GetRunnerParameters));
        client.LibraryFile = result.LibraryFile;
        return result;
    }

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the node executing this runner</returns>
    public async Task<ProcessingNode> GetNode()
    {
        client.Node ??= await client.SendRequest<ProcessingNode>(nameof(GetNode));
        return client.Node;
    }
    
    /// <summary>
    /// Sends an email to the provided recipients
    /// </summary>
    /// <param name="to">a list of email addresses</param>
    /// <param name="subject">the subject of the email</param>
    /// <param name="body">the plain text body of the email</param>
    /// <returns>true if successfully sent, otherwise false</returns>
    public async Task<string> SendEmail(string[] to, string subject, string body)
        => await client.SendRequest<string>(nameof(SendEmail), new { To = to, Subject = subject, Body = body });
    
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    public async Task RecordNotification(NotificationSeverity severity, string title, string? message)
        => await client.SendRequest(nameof(RecordNotification), new { Severity = severity, Title = title, Message = message });

    #if(DEBUG)
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="message">the message being logged</param>
    /// <returns>a task to await</returns>
    public async Task LogMessage(object message)
        => await client.SendRequest(nameof(LogMessage), message);
    #endif
}