namespace Project_SDOBA;

public partial class AppShell : Shell
{
    private bool _authChecked;

    public AppShell()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_authChecked)
            return;

        _authChecked = true;

        var accessToken = await SecureStorage.GetAsync("access_token");

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await GoToAsync("//MainPage");
        }
    }
}
