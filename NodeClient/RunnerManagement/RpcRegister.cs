using System.Text.Json;
using FileFlows.Shared;

namespace FileFlows.NodeClient;

/// <summary>
/// Handles registration and execution of RPC methods with flexible signatures.
/// </summary>
public class RpcRegister : RegisterHandler
{
    /// <summary>
    /// Handles an incoming RPC request, executing the corresponding registered method.
    /// </summary>
    /// <param name="json">The JSON string representing the request.</param>
    /// <returns>A JSON string containing the result.</returns>
    public async Task<string?> HandleRequest(string json)
    {
        var request = JsonSerializer.Deserialize<RpcRequest>(json);
        if (request == null || !_handlers.TryGetValue(request.Method, out var handler))
            return JsonSerializer.Serialize(new { Result = $"Unknown method '{request?.Method ?? "null"}'" });

        // Deserialize parameters into the expected types based on the registered handler
        object? result = null;

        try
        {
            result = await InvokeAsync(request.Method, request.Params);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { request.Id, Error = ex.Message });
        }

        if (result == null && request.Id == 0)
            return null; // no response

        return JsonSerializer.Serialize(new { request.Id, Result = result });
    }
}

/// <summary>
/// Represents an RPC request.
/// </summary>
public class RpcRequest
{
    /// <summary>
    /// Gets or sets the ID of the request
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The name of the method to be called.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The parameters for the method call.
    /// </summary>
    public object[] Params { get; set; } = Array.Empty<object>();
}