namespace doanC_.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load các cài đặt đã lưu
        var savedLanguage = Preferences.Get("SelectedLanguage", "VN Tiếng Việt");
        var savedVoice = Preferences.Get("SelectedVoice", "Giọng nữ miền Nam");
        var savedRadius = Preferences.Get("GeoFenceRadius", "50 mét");
        var backgroundTracking = Preferences.Get("BackgroundTracking", true);
        var offlinePackage = Preferences.Get("OfflinePackage", "Phố Lê Thánh Tôn · 24MB");

        LanguageLabel.Text = savedLanguage;
        VoiceLabel.Text = savedVoice;
        RadiusLabel.Text = savedRadius;
        BackgroundTrackingSwitch.IsToggled = backgroundTracking;
        OfflinePackageLabel.Text = offlinePackage;
    }

    private async void OnLanguageClicked(object sender, EventArgs e)
    {
        // Navigate to language selection page
        await Shell.Current.GoToAsync("//LanguageSelectionPage");
    }

    private async void OnVoiceClicked(object sender, EventArgs e)
    {
        string[] voices = { "Giọng nữ miền Nam", "Giọng nam miền Bắc", "Giọng nữ miền Bắc", "Giọng nam miền Nam" };
        var result = await DisplayActionSheet("Chọn giọng đọc", "Hủy", null, voices);

        if (result != null && result != "Hủy")
        {
            VoiceLabel.Text = result;
            Preferences.Set("SelectedVoice", result);
        }
    }

    private async void OnRadiusClicked(object sender, EventArgs e)
    {
        string[] radii = { "20 mét", "50 mét", "100 mét", "200 mét" };
        var result = await DisplayActionSheet("Chọn bán kính kích hoạt", "Hủy", null, radii);

        if (result != null && result != "Hủy")
        {
            RadiusLabel.Text = result;
            Preferences.Set("GeoFenceRadius", result);
        }
    }

    private void OnBackgroundTrackingToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("BackgroundTracking", e.Value);

        if (e.Value)
        {
            // Enable background tracking
        }
        else
        {
            // Disable background tracking
        }
    }

    private async void OnOfflinePackageClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Tải gói offline", "Tính năng tải gói offline đang được phát triển", "OK");
    }


}
