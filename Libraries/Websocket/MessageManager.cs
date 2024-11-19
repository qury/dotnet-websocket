using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dotnet_websocket.Interfaces;

namespace dotnet_websocket.Libraries.Websocket;

public class MessageManager
{
    private readonly ConcurrentDictionary<int, WebSocket> _clients;
    private readonly IEnumerable<ICommandHandler> _serviceProvider;
    private int _currentClientId;

    public MessageManager(
        ConcurrentDictionary<int, WebSocket> clients,
        IEnumerable<ICommandHandler> serviceProvider
    )
    {
        _clients = clients;
        _serviceProvider = serviceProvider;
    }

    public int RegisterClient(WebSocket webSocket)
    {
        int clientId = Interlocked.Increment(ref _currentClientId);
        _clients[clientId] = webSocket;
        Console.WriteLine($"Client {clientId} connected.");
        return clientId;
    }

    public async Task HandleClientMessagesAsync(int clientId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var segment = new ArraySegment<byte>(buffer);

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(segment, default);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"Client {clientId} disconnected.");
                _clients.TryRemove(clientId, out _);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", default);
                return;
            }

            var message = Encoding.UTF8.GetString(segment.Array, 0, result.Count);
            Console.WriteLine($"Message from Client {clientId}: {message}");

            await ProcessMessageAsync(clientId, message);
        }
    }

    private async Task ProcessMessageAsync(int clientId, string message)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(message);
            if (json.TryGetProperty("cmd", out var cmdProperty))
            {
                var cmd = cmdProperty.GetString();

                var handler = _serviceProvider.FirstOrDefault(s => s.GetType().Name == cmd);
                if (handler != null)
                {
                    await handler.HandleAsync(clientId, message);
                    return;
                }

                Console.WriteLine($"No handler found for command: {cmd}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message from client {clientId}: {ex.Message}");
        }
    }
}
