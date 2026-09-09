using Project_SDOBA.Models;
using Project_SDOBA.Services;

namespace Project_SDOBA.Views;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;

    public MainPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadConversationsAsync();
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            var conversations = await _apiService.GetConversationsAsync();

            if (conversations == null)
            {
                await DisplayAlert(
                    "ERROR",
                    "Не удалось загрузить беседы",
                    "OK");

                return;
            }

            ConversationsList.ItemsSource = conversations;
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "ERROR",
                "Не удалось подключиться к серверу",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert(
                "ERROR",
                "Сервер не отвечает",
                "OK");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");

        await Shell.Current.GoToAsync("//LoginPage");
    }
}
