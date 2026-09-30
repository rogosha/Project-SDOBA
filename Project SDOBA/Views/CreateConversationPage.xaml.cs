using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views;

public partial class CreateConversationPage : ContentPage
{
    private readonly ApiService _apiService;

    private readonly ObservableCollection<User> _members =
        new();

    private CancellationTokenSource? _searchCancellation;

    private uint _currentUserId;
    private User? _foundUser;

    public CreateConversationPage()
    {
        InitializeComponent();

        _apiService = new ApiService();

        MembersList.ItemsSource = _members;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_currentUserId != 0)
            return;

        _currentUserId =
            await GetCurrentUserIdFromTokenAsync();

        UpdateMembersCount();
    }

    private async Task<uint> GetCurrentUserIdFromTokenAsync()
    {
        var token =
            await SecureStorage.GetAsync(
                "access_token");

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

    private async void OnUsernameTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        _searchCancellation?.Cancel();

        UserResult.IsVisible = false;
        _foundUser = null;

        HideError();

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
                ShowError(
                    "Пользователь не найден");

                return;
            }

            if (user.Id == _currentUserId)
            {
                ShowError(
                    "Нельзя добавить самого себя");

                return;
            }

            if (_members.Any(
                    x => x.Id == user.Id))
            {
                ShowError(
                    "Пользователь уже добавлен");

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
                ShowError(ex.Message);
            }
        }
        catch (Exception ex)
        {
            if (!cancellation.IsCancellationRequested)
            {
                ShowError(ex.Message);
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

    private void OnFoundUserTapped(
        object sender,
        TappedEventArgs e)
    {
        if (_foundUser == null)
            return;

        if (_members.Any(
                x => x.Id == _foundUser.Id))
        {
            ShowError(
                "Пользователь уже добавлен");

            return;
        }

        _members.Add(_foundUser);

        UsernameEntry.Text =
            string.Empty;

        UserResult.IsVisible = false;
        _foundUser = null;

        HideError();

        UpdateMembersCount();
    }

    private void OnRemoveMemberClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not User user)
        {
            return;
        }

        _members.Remove(user);

        UpdateMembersCount();
    }

    private async void OnCreateClicked(
        object sender,
        EventArgs e)
    {
        HideError();

        if (_currentUserId == 0)
        {
            ShowError(
                "Не удалось определить текущего пользователя");

            return;
        }

        if (_members.Count == 0)
        {
            ShowError(
                "Добавьте хотя бы одного пользователя");

            return;
        }

        var userIds =
            new List<uint>
            {
                _currentUserId
            };

        userIds.AddRange(
            _members.Select(x => x.Id));

        var button = (Button)sender;
        button.IsEnabled = false;

        try
        {
            var conversation =
                await _apiService.CreateConversationAsync(
                    userIds);

            if (conversation == null)
            {
                ShowError(
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
            ShowError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowError(
                "Сервер не отвечает");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void UpdateMembersCount()
    {
        MembersCountLabel.Text =
            $"MEMBERS: {_members.Count + 1}";
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.IsVisible = false;
    }
}