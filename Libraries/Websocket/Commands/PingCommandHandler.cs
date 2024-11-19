using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dotnet_websocket.Interfaces;

namespace dotnet_websocket.Libraries.Websocket.Commands
{
    public class PingCommandHandler : ICommandHandler
    {
        private readonly ConcurrentDictionary<int, WebSocket> _clients;

        public PingCommandHandler(ConcurrentDictionary<int, WebSocket> clients)
        {
            _clients = clients;
        }

        public async Task HandleAsync(int clientId, string payload)
        {
            if (
                _clients.TryGetValue(clientId, out var webSocket)
                && webSocket.State == WebSocketState.Open
            )
            {
                var response = new { message = "pong", clientId };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes),
                    WebSocketMessageType.Text,
                    true,
                    default
                );
                Console.WriteLine($"Client {clientId}: Sent pong response.");
            }
            else
            {
                Console.WriteLine($"Client {clientId}: WebSocket is not in an open state.");
            }
        }
    }
}
