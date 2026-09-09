using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views.Auth;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowError("Заполните все поля");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Пароли не совпадают");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Пароль должен содержать минимум 6 символов");
            return;
        }

        RegisterButton.IsEnabled = false;

        try
        {
            var request = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password
            };

            var response = await _apiService.RegisterAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ShowError("Не удалось зарегистрировать пользователя");
                return;
            }

            await Shell.Current.GoToAsync("//LoginPage");
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
            RegisterButton.IsEnabled = true;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
