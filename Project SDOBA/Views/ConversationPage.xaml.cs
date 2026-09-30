using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views;

public partial class ConversationPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;

    private readonly ObservableCollection<Message> _messages = new();

    private CancellationTokenSource? _receiveCancellation;

    private uint _conversationId;
    private uint _currentUserId;

    private Conversation? _conversation;

    public string ConversationId
    {
        set
        {
            if (!uint.TryParse(value, out var id))
                return;

            _conversationId = id;
        }
    }

    public string ConversationTitleText
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ConversationTitle.Text = value;
            }
        }
    }

    public ConversationPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _webSocketService = new WebSocketService();

        MessagesList.ItemsSource = _messages;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_conversationId == 0)
            return;

        await InitializeCurrentUserAsync();
        await LoadConversationAsync();
        await LoadMessagesAsync();
        await ConnectWebSocketAsync();
    }

    private async Task InitializeCurrentUserAsync()
    {
        var token =
            await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            return;

        var parts = token.Split('.');

        if (parts.Length != 3)
            return;

        try
        {
            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            while (payload.Length % 4 != 0)
                payload += "=";

            var jsonBytes =
                Convert.FromBase64String(payload);

            var payloadJson =
                Encoding.UTF8.GetString(jsonBytes);

            using var document =
                JsonDocument.Parse(payloadJson);

            var root = document.RootElement;

            if (root.TryGetProperty(
                    "user_id",
                    out var userId))
            {
                if (userId.ValueKind ==
                        JsonValueKind.Number &&
                    userId.TryGetUInt32(out var id))
                {
                    _currentUserId = id;
                    return;
                }

                if (userId.ValueKind ==
                        JsonValueKind.String &&
                    uint.TryParse(
                        userId.GetString(),
                        out id))
                {
                    _currentUserId = id;
                    return;
                }
            }

            if (root.TryGetProperty(
                    "sub",
                    out var sub))
            {
                if (sub.ValueKind ==
                        JsonValueKind.Number &&
                    sub.TryGetUInt32(out var id))
                {
                    _currentUserId = id;
                    return;
                }

                if (sub.ValueKind ==
                        JsonValueKind.String &&
                    uint.TryParse(
                        sub.GetString(),
                        out id))
                {
                    _currentUserId = id;
                }
            }
        }
        catch
        {
            _currentUserId = 0;
        }
    }

    private async Task LoadConversationAsync()
    {
        try
        {
            var conversation =
                await _apiService.GetConversationAsync(
                    _conversationId);

            if (conversation == null)
                return;

            _conversation = conversation;

            UpdateConversationTitle();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "ERROR",
                ex.Message,
                "OK");
        }
    }

    private void UpdateConversationTitle()
    {
        if (_conversation == null)
            return;

        var otherUsers = _conversation.Members
            .Where(member =>
                member.UserId != _currentUserId)
            .Select(member =>
                member.User?.Username)
            .Where(username =>
                !string.IsNullOrWhiteSpace(username))
            .ToList();

        if (_conversation.Members.Count <= 2)
        {
            ConversationTitle.Text =
                otherUsers.Count > 0
                    ? string.Join(", ", otherUsers)
                    : _conversation.Name;

            return;
        }

        ConversationTitle.Text =
            string.IsNullOrWhiteSpace(
                _conversation.Name)
                ? $"CONVERSATION {_conversation.Id}"
                : _conversation.Name;
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            var messages =
                await _apiService.GetMessagesAsync(
                    _conversationId);

            _messages.Clear();

            DateTime? lastDate = null;

            foreach (var message in messages)
            {
                message.IsOwnMessage =
                    message.SenderId == _currentUserId;

                var messageDate =
                    message.CreatedAt.Date;

                message.ShowDate =
                    lastDate == null ||
                    lastDate.Value != messageDate;

                lastDate = messageDate;

                _messages.Add(message);
            }

            if (_messages.Count > 0)
            {
                MessagesList.ScrollTo(
                    _messages[^1],
                    position: ScrollToPosition.End,
                    animate: false);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "ERROR",
                ex.Message,
                "OK");
        }
    }

    private async Task ConnectWebSocketAsync()
    {
        try
        {
            await _webSocketService.ConnectAsync(
                _conversationId);

            _receiveCancellation =
                new CancellationTokenSource();

            _ = ReceiveMessagesAsync(
                _receiveCancellation.Token);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "WEBSOCKET ERROR",
                ex.Message,
                "OK");
        }
    }

    private async Task ReceiveMessagesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _webSocketService.ReceiveMessagesAsync(
                AddMessageAsync,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    await DisplayAlert(
                        "WEBSOCKET ERROR",
                        ex.Message,
                        "OK");
                });
        }
    }

    private async Task AddMessageAsync(
        Message message)
    {
        await MainThread.InvokeOnMainThreadAsync(
            () =>
            {
                message.IsOwnMessage =
                    message.SenderId == _currentUserId;

                if (_messages.Count == 0)
                {
                    message.ShowDate = true;
                }
                else
                {
                    var previousMessage =
                        _messages[^1];

                    message.ShowDate =
                        previousMessage.CreatedAt.Date !=
                        message.CreatedAt.Date;
                }

                _messages.Add(message);

                MessagesList.ScrollTo(
                    message,
                    position: ScrollToPosition.End,
                    animate: true);
            });
    }

    private async void OnDeleteMessageClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not Message message)
        {
            return;
        }

        if (!message.IsOwnMessage)
            return;

        button.IsEnabled = false;

        try
        {
            await _apiService.DeleteMessageAsync(
                _conversationId,
                message.Id);

            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    _messages.Remove(message);
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "ERROR",
                ex.Message,
                "OK");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async void OnSettingsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ConversationSettingsPage
            {
                ConversationId =
                    _conversationId.ToString()
            });
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSendClicked(
        object sender,
        EventArgs e)
    {
        var content =
            MessageEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return;

        if (!_webSocketService.IsConnected)
        {
            await DisplayAlert(
                "ERROR",
                "WebSocket не подключён",
                "OK");

            return;
        }

        try
        {
            await _webSocketService.SendMessageAsync(
                content);

            MessageEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "ERROR",
                ex.Message,
                "OK");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        _receiveCancellation?.Cancel();
        _receiveCancellation?.Dispose();
        _receiveCancellation = null;

        await _webSocketService.DisconnectAsync();
    }
}