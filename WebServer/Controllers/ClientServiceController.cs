using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for WebSocket communication with clients.
/// </summary>
[Route("/api/client-service")]
public class WebSocketController : ControllerBase
{
    // Collection to store active WebSocket connections
    private static ConcurrentBag<WebSocket> _webSockets = new ();

    /// <summary>
    /// Handles the WebSocket connection request.
    /// </summary>
    /// <returns>A task representing the WebSocket connection.</returns>
    [HttpGet]
    [SwaggerIgnore]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _webSockets.Add(webSocket);
            await ReceiveLoop(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    /// <summary>
    /// Continuously receives messages from the WebSocket.
    /// </summary>
    /// <param name="webSocket">The WebSocket to receive messages from.</param>
    /// <returns>A task representing the receive loop.</returns>
    private async Task ReceiveLoop(WebSocket? webSocket)
    {
        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Handle received message
                Console.WriteLine($"Received message: {message}");

                // Parse the received message as JSON
                var commandData = JsonSerializer.Deserialize<CommandData>(message);

                if (commandData?.Command == "MyCommand")
                {
                    // Process the command data here
                    Console.WriteLine($"Received MyCommand: {commandData.Data}");
                }
            }
        }

        _webSockets.TryTake(out webSocket);
    }

    /// <summary>
    /// Sends a message to all connected clients.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="data">The data associated with the command.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SendMessageToAll(string command, string data)
    {
        var commandData = new CommandData
        {
            Command = command,
            Data = data
        };

        var message = JsonSerializer.Serialize(commandData);
        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var webSocket in _webSockets)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Represents the command data sent to the clients.
    /// </summary>
    private class CommandData
    {
        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the command.
        /// </summary>
        public string Data { get; set; }
    }
}
