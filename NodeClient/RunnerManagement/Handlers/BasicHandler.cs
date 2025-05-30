using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient.Handlers;

/// <summary>
/// Basic handler of node/runner communication
/// </summary>
public class BasicHandler
{
    private JsonRpcServer rpcServer;
    private ClientConnection _client;

    /// <summary>
    /// Constructs a new instance of the Basic Handler
    /// </summary>
    /// <param name="rpcServer">the RPC server</param>
    /// <param name="rpcRegister">the RPC register</param>
    public BasicHandler(JsonRpcServer rpcServer, RpcRegister rpcRegister)
    {
        this.rpcServer = rpcServer;
        _client = rpcServer._client.Connection;
        #if(DEBUG)
        rpcRegister.Register(nameof(LogMessage), LogMessage);
        #endif
        rpcRegister.Register(nameof(GetRunnerParameters), GetRunnerParameters);
        rpcRegister.Register(nameof(GetNode), GetNode);
        rpcRegister.Register<EmailModel, string>(nameof(SendEmail), SendEmail);
        rpcRegister.Register<RecordNotificationModel>(nameof(RecordNotification), RecordNotification);
    }

    /// <summary>
    /// Get the runner parameters
    /// </summary>
    /// <returns>the runner parameters</returns>
    public RunnerParameters GetRunnerParameters()
        => rpcServer.runnerParameters;

    /// <summary>
    /// Gets the processing node
    /// </summary>
    /// <returns>the node executing this runner</returns>
    public ProcessingNode GetNode()
        => rpcServer._client.Node!;


    /// <summary>
    /// Sends an email to the provided recipients
    /// </summary>
    /// <param name="model">the email model</param>
    /// <returns>true if successfully sent, otherwise false</returns>
    public async Task<string> SendEmail(EmailModel model)
    {
        if(await _client.AwaitConnection() == false)
            throw new Exception("Not connected to server.");
        return await _client.InvokeAsync<string>(nameof(SendEmail), model.To, model.Subject, model.Body);
    }

    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="model">the notification model</param>
    public void RecordNotification(RecordNotificationModel model)
    {
        if(_client.AwaitConnection().GetAwaiter().GetResult() == false)
            return;
        try
        {
            _ = _client.InvokeAsync<string>(nameof(RecordNotification), model.Severity, model.Title, model.Message);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

#if(DEBUG)
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="message">the message to log</param>
    public void LogMessage(string message)
    {
        if(string.IsNullOrEmpty(message) == false)
            rpcServer._logMessage(message);
    }
    #endif

    /// <summary>
    /// Email model
    /// </summary>
    /// <param name="To">a list of email addresses</param>
    /// <param name="Subject">the subject of the email</param>
    /// <param name="Body">the plain text body of the email</param>
    public record EmailModel(string[] To, string Subject, string Body);

    /// <summary>
    /// Notification model
    /// </summary>
    /// <param name="Severity">The severity level of the notification.</param>
    /// <param name="Title">The title of the notification.</param>
    /// <param name="Message">The message content of the notification.</param>
    public record RecordNotificationModel(NotificationSeverity Severity, string Title, string? Message);
}