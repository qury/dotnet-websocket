using Microsoft.AspNetCore.Mvc;


using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace dotnet_websocket.Controllers;


[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly ConcurrentDictionary<int, WebSocket> _clients;

    public MessageController(ConcurrentDictionary<int, WebSocket> clients)
    {
        _clients = clients;
    }

    [HttpPost("{clientId}/send")]
    public async Task<IActionResult> SendMessage(int clientId, [FromBody] MessageRequest request)
    {
        if (_clients.TryGetValue(clientId, out var webSocket) && webSocket.State == WebSocketState.Open)
        {
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, default);
            return Ok(new { success = true, message = "Message sent successfully." });
        }

        return NotFound(new { success = false, message = "Client not connected or WebSocket is closed." });
    }
}

public class MessageRequest
{
    public string Message { get; set; }
}
