using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowError("Заполните имя пользователя и пароль");
            return;
        }

        var loginButton = (Button)sender;
        loginButton.IsEnabled = false;

        try
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var result = await _apiService.LoginAsync(request);

            if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ShowError("Неверное имя пользователя или пароль");
                return;
            }

            await SecureStorage.SetAsync(
                "access_token",
                result.AccessToken);

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (HttpRequestException)
        {
            ShowError("Не удалось подключиться к серверу");
        }
        catch (TaskCanceledException)
        {
            ShowError("Сервер не отвечает");
        }
        finally
        {
            loginButton.IsEnabled = true;
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
