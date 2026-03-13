namespace doanC_.Views;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
     InitializeComponent();
    }

    private async void OnViewPoiListClicked(object sender, EventArgs e)
    {
await Shell.Current.GoToAsync("//PoiListPage");
    }

private async void OnScanQrClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//QrScannerPage");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}
