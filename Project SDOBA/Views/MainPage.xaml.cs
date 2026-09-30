using System.Text;
using System.Text.Json;
using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;

    private uint _currentUserId;
    private User? _foundUser;

    private CancellationTokenSource? _searchCancellation;

    public MainPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await InitializeCurrentUserAsync();
        await LoadConversationsAsync();
    }

    private async Task InitializeCurrentUserAsync()
    {
        try
        {
            var userId =
                await GetCurrentUserIdFromTokenAsync();

            if (userId == 0)
            {
                ShowConversationError(
                    "Не удалось определить текущего пользователя");

                return;
            }

            _currentUserId = userId;

            var currentUser =
                await _apiService.GetUserAsync(
                    _currentUserId);

            if (currentUser != null)
            {
                CurrentUserLabel.Text =
                    $"@{currentUser.Username}";
            }
        }
        catch (Exception ex)
        {
            ShowConversationError(ex.Message);
        }
    }

    private async Task<uint> GetCurrentUserIdFromTokenAsync()
    {
        var token =
            await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            return 0;

        var parts = token.Split('.');

        if (parts.Length != 3)
            return 0;

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
                    return id;
                }

                if (userId.ValueKind ==
                    JsonValueKind.String &&
                    uint.TryParse(
                        userId.GetString(),
                        out id))
                {
                    return id;
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
                    return id;
                }

                if (sub.ValueKind ==
                    JsonValueKind.String &&
                    uint.TryParse(
                        sub.GetString(),
                        out id))
                {
                    return id;
                }
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            var conversations =
                await _apiService.GetConversationsAsync();

            if (conversations == null)
            {
                ShowConversationError(
                    "Не удалось загрузить беседы");

                ConversationsList.ItemsSource =
                    new List<Conversation>();

                return;
            }

            foreach (var conversation in conversations)
            {
                conversation.DisplayName =
                    GetConversationDisplayName(
                        conversation);

                if (string.IsNullOrWhiteSpace(
                        conversation.LastMessage))
                {
                    conversation.LastMessage =
                        await GetLastMessageTextAsync(
                            conversation.Id);
                }
            }

            conversations = conversations
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();

            ConversationsList.ItemsSource =
                conversations;
        }
        catch (HttpRequestException ex)
        {
            ShowConversationError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowConversationError(
                "Сервер не отвечает");
        }
        catch (Exception ex)
        {
            ShowConversationError(ex.Message);
        }
    }

    private async Task<string> GetLastMessageTextAsync(
        uint conversationId)
    {
        try
        {
            var messages =
                await _apiService.GetMessagesAsync(
                    conversationId);

            if (messages == null ||
                messages.Count == 0)
            {
                return string.Empty;
            }

            var lastMessage =
                messages
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

            return lastMessage?.Content
                   ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetConversationDisplayName(
        Conversation conversation)
    {
        if (conversation.Members == null ||
            conversation.Members.Count == 0)
        {
            return string.IsNullOrWhiteSpace(
                conversation.Name)
                ? $"CONVERSATION {conversation.Id}"
                : conversation.Name;
        }

        if (conversation.Members.Count == 2)
        {
            var otherUser =
                conversation.Members
                    .Select(x => x.User)
                    .FirstOrDefault(
                        x => x != null &&
                             x.Id != _currentUserId);

            if (otherUser != null &&
                !string.IsNullOrWhiteSpace(
                    otherUser.Username))
            {
                return otherUser.Username;
            }

            return string.IsNullOrWhiteSpace(
                conversation.Name)
                ? $"CONVERSATION {conversation.Id}"
                : conversation.Name;
        }

        return string.IsNullOrWhiteSpace(
            conversation.Name)
            ? $"CONVERSATION {conversation.Id}"
            : conversation.Name;
    }

    private async void OnUsernameTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        _searchCancellation?.Cancel();

        UserResult.IsVisible = false;
        _foundUser = null;

        HideConversationError();

        var username =
            e.NewTextValue?.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var cancellation =
            new CancellationTokenSource();

        _searchCancellation = cancellation;

        try
        {
            await Task.Delay(
                600,
                cancellation.Token);

            var user =
                await _apiService.SearchUserAsync(
                    username,
                    cancellation.Token);

            if (cancellation.IsCancellationRequested)
                return;

            if (user == null)
            {
                ShowConversationError(
                    "Пользователь не найден");

                return;
            }

            if (user.Id == _currentUserId)
            {
                ShowConversationError(
                    "Нельзя создать беседу с самим собой");

                return;
            }

            _foundUser = user;

            FoundUsernameLabel.Text =
                $"@{user.Username}";

            FoundUserIdLabel.Text =
                $"ID: {user.Id}";

            UserResult.IsVisible = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpRequestException ex)
        {
            if (!cancellation.IsCancellationRequested)
            {
                ShowConversationError(ex.Message);
            }
        }
        catch (Exception ex)
        {
            if (!cancellation.IsCancellationRequested)
            {
                ShowConversationError(ex.Message);
            }
        }
        finally
        {
            if (ReferenceEquals(
                    _searchCancellation,
                    cancellation))
            {
                _searchCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private async void OnFoundUserTapped(
        object sender,
        TappedEventArgs e)
    {
        if (_foundUser == null)
            return;

        try
        {
            var conversation =
                await _apiService.CreateConversationAsync(
                    new List<uint>
                    {
                        _currentUserId,
                        _foundUser.Id
                    });

            if (conversation == null)
            {
                ShowConversationError(
                    "Не удалось создать conversation");

                return;
            }

            await Navigation.PushAsync(
                new ConversationPage
                {
                    ConversationId =
                        conversation.Id.ToString()
                });
        }
        catch (HttpRequestException ex)
        {
            ShowConversationError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowConversationError(
                "Сервер не отвечает");
        }
    }

    private async void OnNewConversationClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new CreateConversationPage());
    }

    private async void OnConversationSelected(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault()
            is not Conversation conversation)
        {
            return;
        }

        ConversationsList.SelectedItem = null;

        await Navigation.PushAsync(
            new ConversationPage
            {
                ConversationId =
                    conversation.Id.ToString()
            });
    }

    private void OnLogoutClicked(
        object sender,
        EventArgs e)
    {
        _searchCancellation?.Cancel();

        SecureStorage.Remove("access_token");

        if (Window is not null)
        {
            Window.Page = new NavigationPage(
                new Auth.LoginPage());
        }
    }

    private void ShowConversationError(
        string message)
    {
        ConversationErrorLabel.Text = message;
        ConversationErrorLabel.IsVisible = true;
    }

    private void HideConversationError()
    {
        ConversationErrorLabel.IsVisible = false;
    }
}