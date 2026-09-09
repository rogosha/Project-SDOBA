namespace Project_SDOBA;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        count++;

        if (count == 1)
            Btn1.Text = $"Clicked {count} time";
        else
            Btn1.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(Btn1.Text);
    } 
    void nameEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        nameLabel.Text = nameEntry.Text;
    }
    void nameEntry_Completed(object sender, TextChangedEventArgs e)
    {
        nameLabel.Text = nameEntry.Text;
    }

    private void nameEntry_OnCompleted(object? sender, EventArgs e)
    {
        nameLabelFinal.Text = nameEntry.Text;
    }
}