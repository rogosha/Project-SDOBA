namespace Project_SDOBA;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
        
        // MainPage = new ContentPage
        // {
        //     Content = new Label
        //     {
        //         Text = "SDOBA работает",
        //         HorizontalOptions = LayoutOptions.Center,
        //         VerticalOptions = LayoutOptions.Center
        //     }
        // };
    }
}
