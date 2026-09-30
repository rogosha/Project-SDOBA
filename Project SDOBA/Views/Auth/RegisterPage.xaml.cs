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

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Введите имя пользователя");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Введите email");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введите пароль");
            return;
        }

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowError("Подтвердите пароль");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Пароли не совпадают");
            return;
        }

        var registerButton = (Button)sender;
        registerButton.IsEnabled = false;

        try
        {
            var request = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password
            };

            var response = await _apiService.RegisterAsync(request);

            if (response.IsSuccessStatusCode)
            {
                await Navigation.PopAsync();
                return;
            }

            var error = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(error))
            {
                ShowError(
                    $"Ошибка регистрации: {(int)response.StatusCode} {response.StatusCode}");
            }
            else
            {
                ShowError(error);
            }
        }
        catch (HttpRequestException ex)
        {
            ShowError(ex.Message);
        }
        catch (TaskCanceledException)
        {
            ShowError("Сервер не отвечает");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            registerButton.IsEnabled = true;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}