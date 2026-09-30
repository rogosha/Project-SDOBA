using System.Collections.ObjectModel;
using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views;

public partial class ConversationSettingsPage : ContentPage
{
    private readonly ApiService _apiService;

    private readonly ObservableCollection<ConversationMember> _members =
        new();

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

    public ConversationSettingsPage()
    {
        InitializeComponent();

        _apiService = new ApiService();

        MembersList.ItemsSource = _members;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_conversationId == 0)
            return;

        await InitializeCurrentUserAsync();
        await LoadConversationAsync();
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
                System.Text.Encoding.UTF8.GetString(
                    jsonBytes);

            using var document =
                System.Text.Json.JsonDocument.Parse(
                    payloadJson);

            var root = document.RootElement;

            if (root.TryGetProperty(
                    "user_id",
                    out var userId))
            {
                if (userId.ValueKind ==
                        System.Text.Json.JsonValueKind.Number &&
                    userId.TryGetUInt32(out var id))
                {
                    _currentUserId = id;
                    return;
                }

                if (userId.ValueKind ==
                        System.Text.Json.JsonValueKind.String &&
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
                        System.Text.Json.JsonValueKind.Number &&
                    sub.TryGetUInt32(out var id))
                {
                    _currentUserId = id;
                    return;
                }

                if (sub.ValueKind ==
                        System.Text.Json.JsonValueKind.String &&
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

            UpdateConversationData();
        }
        catch (HttpRequestException ex)
        {
            ShowManagementError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowManagementError(
                "Сервер не отвечает");
        }
        catch (Exception ex)
        {
            ShowManagementError(ex.Message);
        }
    }

    private void UpdateConversationData()
    {
        if (_conversation == null)
            return;

        ConversationNameEntry.Text =
            _conversation.Name;

        _members.Clear();

        foreach (var member in _conversation.Members)
        {
            _members.Add(member);
        }

        MembersCountLabel.Text =
            $"MEMBERS: {_members.Count}";
    }

    private async void OnRenameConversationClicked(
        object sender,
        EventArgs e)
    {
        if (_conversation == null)
            return;

        var name =
            ConversationNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowManagementError(
                "Введите название беседы");

            return;
        }

        if (_conversation.Members.Count < 3)
        {
            ShowManagementError(
                "Название беседы из двух человек менять нельзя");

            return;
        }

        var button = (Button)sender;
        button.IsEnabled = false;

        try
        {
            var conversation =
                await _apiService.RenameConversationAsync(
                    _conversationId,
                    name);

            if (conversation != null)
            {
                _conversation = conversation;
            }
            else
            {
                _conversation.Name = name;
            }

            UpdateConversationData();
            HideManagementError();
        }
        catch (HttpRequestException ex)
        {
            ShowManagementError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowManagementError(
                "Сервер не отвечает");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async void OnAddMemberClicked(
        object sender,
        EventArgs e)
    {
        if (_conversation == null)
            return;

        var username =
            AddMemberUsernameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowManagementError(
                "Введите username");

            return;
        }

        try
        {
            var user =
                await _apiService.SearchUserAsync(
                    username);

            if (user == null)
            {
                ShowManagementError(
                    "Пользователь не найден");

                return;
            }

            if (user.Id == _currentUserId)
            {
                ShowManagementError(
                    "Пользователь уже является участником");

                return;
            }

            if (_conversation.Members.Any(
                    member => member.UserId == user.Id))
            {
                ShowManagementError(
                    "Пользователь уже является участником");

                return;
            }

            await _apiService.AddConversationMemberAsync(
                _conversationId,
                user.Id);

            AddMemberUsernameEntry.Text =
                string.Empty;

            await LoadConversationAsync();

            HideManagementError();
        }
        catch (HttpRequestException ex)
        {
            ShowManagementError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowManagementError(
                "Сервер не отвечает");
        }
    }

    private async void OnRemoveMemberClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter
                is not ConversationMember member)
        {
            return;
        }

        if (member.UserId == _currentUserId)
        {
            ShowManagementError(
                "Нельзя удалить самого себя");

            return;
        }

        try
        {
            await _apiService.RemoveConversationMemberAsync(
                _conversationId,
                member.UserId);

            await LoadConversationAsync();

            HideManagementError();
        }
        catch (HttpRequestException ex)
        {
            ShowManagementError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowManagementError(
                "Сервер не отвечает");
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void ShowManagementError(
        string message)
    {
        ManagementErrorLabel.Text = message;
        ManagementErrorLabel.IsVisible = true;
    }

    private void HideManagementError()
    {
        ManagementErrorLabel.IsVisible = false;
    }
}