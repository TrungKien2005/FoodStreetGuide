namespace doanC_.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        NavigateToLanguageSelection();
    }

    private async void NavigateToLanguageSelection()
    {
        await Task.Delay(3000); // Hi?n th? splash 3 giây
        await Shell.Current.GoToAsync("//LanguageSelectionPage");
    }
}
