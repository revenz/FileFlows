using System.Text.Json;

namespace FileFlows.Client.Services.Frontend;

/// <summary>
/// Front end register
/// </summary>
public class FrontendRegister : RegisterHandler
{
    /// <summary>
    /// Handles an incoming RPC request, executing the corresponding registered method.
    /// </summary>
    /// <param name="message">The message string representing the request.</param>
    /// <returns>A JSON string containing the result.</returns>
    public async Task<string> HandleRequest(string message)
    {
        var parts = message.Split([':'], 2);
        
        if (parts == null || !_handlers.TryGetValue(parts[0], out var handler))
            return JsonSerializer.Serialize(new { Result = "Unknown method" });

        // Deserialize parameters into the expected types based on the registered handler
        object? result = null;

        try
        {
            result = await InvokeAsync(parts[0], parts[1]);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Result = "Error", Error = ex.Message });
        }

        return JsonSerializer.Serialize(new { Result = result });
    }
}