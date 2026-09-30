namespace Project_SDOBA;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(
            new NavigationPage(new Views.Auth.LoginPage()));

        _ = RestoreSessionAsync(window);

        return window;
    }

    private static async Task RestoreSessionAsync(Window window)
    {
        try
        {
            var token = await SecureStorage.GetAsync("access_token");

            if (!string.IsNullOrWhiteSpace(token))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    window.Page = new NavigationPage(
                        new Views.MainPage());
                });
            }
        }
        catch
        {
            SecureStorage.Remove("access_token");
        }
    }
}