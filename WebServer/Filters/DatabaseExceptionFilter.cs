using System.Net;
using FileFlows.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileFlows.WebServer.Filters;

/// <summary>
/// Represents a custom exception filter that redirects to "/database-offline"
/// if the exception message contains "at Npgsql.NpgsqlConnection.Open".
/// </summary>
public class DatabaseExceptionFilter : IExceptionFilter
{
    /// <summary>
    /// Called when an exception occurs during the processing of a request.
    /// </summary>
    /// <param name="context">The context for the action that threw the exception.</param>
    public void OnException(ExceptionContext context)
    {
        // Check if an exception occurred
        if (context.Exception == null)
            return;
        
        // Check if the request URL contains "/remote/"
        var requestPath = context.HttpContext.Request.Path;
        if (requestPath.HasValue && requestPath.Value.Contains("/remote/"))
            return;
        
        // Get the exception message
        string exceptionMessage = context.Exception.StackTrace ?? string.Empty;

        // Check if the exception message contains "at Npgsql.NpgsqlConnection.Open"
        if (exceptionMessage.Contains("at Npgsql.NpgsqlConnection.Open") == false && 
            exceptionMessage.Contains("at Npgsql.Internal.NpgsqlConnector.Open") == false && 
            exceptionMessage.Contains("NpgsqlConnector.Connect") == false)
            return;
        
        // Redirect to "/database-offline"
        // Prepare the response with the redirect URL
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
        context.HttpContext.Response.Headers["Location"] = "/database-offline";
        context.HttpContext.Response.ContentType = "text/plain";
        context.HttpContext.Response.WriteAsync("/database-offline");
        context.ExceptionHandled = true; // Ensure the exception is handled
        _ = ((NotificationService)ServiceLoader.Load<INotificationService>()).RecordDatabaseOffline();
    }
}
