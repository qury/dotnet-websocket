using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dotnet_websocket.Interfaces;
using dotnet_websocket.Libraries.Websocket;
using dotnet_websocket.Libraries.Websocket.Commands;

namespace dotnet_websocket;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Register services in DI container
        builder.Services.AddSingleton<ConcurrentDictionary<int, WebSocket>>();
        builder.Services.AddSingleton<MessageManager>();
        builder.Services.AddTransient<ICommandHandler, PingCommandHandler>(sp =>
        {
            var clients = sp.GetRequiredService<ConcurrentDictionary<int, WebSocket>>();
            return new PingCommandHandler(clients);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        // WebSocket endpoint
        app.Map(
            "/ws",
            async (
                HttpContext context,
                ConcurrentDictionary<int, WebSocket> clients,
                MessageManager manager
            ) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    int clientId = manager.RegisterClient(webSocket);

                    // Send the client ID back to the client
                    var response = new { clientId };
                    var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(responseBytes),
                        WebSocketMessageType.Text,
                        true,
                        default
                    );

                    // Handle messages
                    await manager.HandleClientMessagesAsync(clientId, webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
        );

        app.UseWebSockets();

        app.Run();
    }
}
