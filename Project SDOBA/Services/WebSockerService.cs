using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Project_SDOBA.Models;

namespace Project_SDOBA.Services;

public class WebSocketService
{
    private ClientWebSocket? _webSocket;

    public bool IsConnected =>
        _webSocket?.State == WebSocketState.Open;

    public async Task ConnectAsync(uint conversationId)
    {
        if (IsConnected)
            return;

        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("JWT token отсутствует");

        _webSocket = new ClientWebSocket();

        _webSocket.Options.SetRequestHeader(
            "Authorization",
            $"Bearer {token}");

        var uri = new Uri(
            $"ws://192.168.31.167:8080/api/v1/ws/conversations/{conversationId}");

        await _webSocket.ConnectAsync(
            uri,
            CancellationToken.None);
    }

    public async Task SendMessageAsync(string content)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "WebSocket не подключён");

        var payload = new
        {
            content
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    public async Task ReceiveMessagesAsync(
        Func<Message, Task> onMessage,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "WebSocket не подключён");

        var buffer = new byte[4096];

        while (
            _webSocket.State == WebSocketState.Open &&
            !cancellationToken.IsCancellationRequested)
        {
            using var stream = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                stream.Write(
                    buffer,
                    0,
                    result.Count);

            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            var json = Encoding.UTF8.GetString(
                stream.ToArray());

            var message = JsonSerializer.Deserialize<Message>(
                json,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web));

            if (message != null)
                await onMessage(message);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket == null)
            return;

        try
        {
            if (_webSocket.State == WebSocketState.Open ||
                _webSocket.State == WebSocketState.CloseReceived)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing connection",
                    CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            _webSocket.Dispose();
            _webSocket = null;
        }
    }
}
