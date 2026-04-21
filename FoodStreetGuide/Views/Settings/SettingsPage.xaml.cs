using System.Diagnostics;
using doanC_;
using doanC_.Helpers;
using doanC_.Services;
using doanC_.Services.Geo;
using doanC_.Services.Localization;
using doanC_.ViewModels;

namespace doanC_.Views
{
    public partial class SettingsPage : ContentPage
    {
        // ✅ Lấy LocationService từ ServiceHelper (singleton)
        private LocationService _locationService => ServiceHelper.GetService<LocationService>();

        // ✅ Lấy GeoFenceService instance
        private GeoFenceService _geoFenceService => GeoFenceService.Instance;

        public SettingsPage()
        {
            InitializeComponent();
            this.BindingContext = new SettingsViewModel();

            LoadSettings();
        }

        private void LoadSettings()
        {
            var savedLanguage = Preferences.Get("AppLanguage", "vi");
            var savedVoice = Preferences.Get("SelectedVoice", "Giọng nữ");

            // ✅ Đọc bán kính dạng số (double) từ Preferences
            var savedRadiusValue = Preferences.Get("GeoFenceRadiusValue", 15.0);
            var savedRadiusDisplay = Preferences.Get("GeoFenceRadius", "15 mét");

            var backgroundTracking = Preferences.Get("BackgroundTracking", true);
            var offlinePackage = Preferences.Get("OfflinePackage", "Phố Lê Thánh Tôn · 24MB");

            // Update language label
            UpdateLanguageLabel(savedLanguage);

            // ✅ Cập nhật giọng đọc (text cứng)
            if (VoiceLabel != null) VoiceLabel.Text = savedVoice;

            // ✅ Hiển thị bán kính đã lưu
            if (RadiusLabel != null) RadiusLabel.Text = savedRadiusDisplay;

            // ✅ Đồng bộ radius với GeoFenceService
            _geoFenceService.UpdateRadius(savedRadiusValue);
            Debug.WriteLine($"[SettingsPage] Loaded radius: {savedRadiusValue}m - {savedRadiusDisplay}");

            if (BackgroundTrackingSwitch != null) BackgroundTrackingSwitch.IsToggled = backgroundTracking;
            if (OfflinePackageLabel != null) OfflinePackageLabel.Text = offlinePackage;
        }

        private void UpdateLanguageLabel(string languageCode)
        {
            var languageNames = new Dictionary<string, string>
            {
                { "vi", "🇻🇳 Tiếng Việt" },
                { "en", "🇺🇸 English" },
                { "zh", "🇨🇳 中文" },
                { "fr", "🇫🇷 Français" },
                { "es", "🇪🇸 Español" },
                { "ja", "🇯🇵 日本語" },
                { "ko", "🇰🇷 한국어" }
            };

            if (LanguageLabel != null && languageNames.TryGetValue(languageCode, out var displayName))
            {
                LanguageLabel.Text = displayName;
            }
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            string[] languages = { "🇻🇳 Tiếng Việt", "🇺🇸 English", "🇨🇳 中文", "🇫🇷 Français", "🇪🇸 Español", "🇯🇵 日本語", "🇰🇷 한국어" };

            var result = await DisplayActionSheet("Chọn ngôn ngữ", "Hủy", null, languages);

            if (result != null && result != "Hủy")
            {
                string languageCode = GetLanguageCode(result);

                // Lưu ngôn ngữ
                Preferences.Set("AppLanguage", languageCode);
                AppResources.SetLanguage(languageCode);

                // ✅ GỬI SỰ KIỆN CHO CÁC TRANG KHÁC
                MessagingCenter.Send(this, "LanguageChanged", languageCode);

                // ✅ Cập nhật UI hiện tại (dùng LoadSettings thay vì UpdateUILanguage)
                LoadSettings();

                await DisplayAlert("Thông báo", $"Đã chuyển sang {result}", "OK");

                // Tạo lại AppShell để cập nhật TabBar
                Application.Current.MainPage = new AppShell();
            }
        }

        private string GetLanguageCode(string displayName)
        {
            return displayName switch
            {
                "🇻🇳 Tiếng Việt" => "vi",
                "🇺🇸 English" => "en",
                "🇨🇳 中文" => "zh",
                "🇫🇷 Français" => "fr",
                "🇪🇸 Español" => "es",
                "🇯🇵 日本語" => "ja",
                "🇰🇷 한국어" => "ko",
                _ => "vi"
            };
        }

        private async void OnVoiceClicked(object sender, EventArgs e)
        {
            string[] voices = { "Giọng nữ", "Giọng nam" };
            var result = await DisplayActionSheet("Chọn giọng đọc", "Hủy", null, voices);

            if (result != null && result != "Hủy")
            {
                Preferences.Set("SelectedVoice", result);
                if (VoiceLabel != null) VoiceLabel.Text = result;
                await DisplayAlert("Thông báo", $"Đã chọn {result}", "OK");
            }
        }

        private async void OnRadiusClicked(object sender, EventArgs e)
        {
            // ✅ MỞ RỘNG BÁN KÍNH LÊN 500M ĐỂ TEST
            string[] radii = {
                "15 mét", "20 mét", "25 mét", "30 mét",
                "50 mét", "100 mét", "200 mét", "300 mét", "400 mét", "500 mét"
            };

            var result = await DisplayActionSheet("Bán kính kích hoạt", "Hủy", null, radii);

            if (result != null && result != "Hủy")
            {
                // ✅ Map giá trị hiển thị sang giá trị số (mét)
                double radiusValue = result switch
                {
                    "15 mét" => 15.0,
                    "20 mét" => 20.0,
                    "25 mét" => 25.0,
                    "30 mét" => 30.0,
                    "50 mét" => 50.0,
                    "100 mét" => 100.0,
                    "200 mét" => 200.0,
                    "300 mét" => 300.0,
                    "400 mét" => 400.0,
                    "500 mét" => 500.0,
                    _ => 15.0
                };

                // ✅ Lưu cả giá trị số và text hiển thị
                Preferences.Set("GeoFenceRadiusValue", radiusValue);
                Preferences.Set("GeoFenceRadius", result);

                // ✅ Set text cho RadiusLabel
                if (RadiusLabel != null) RadiusLabel.Text = result;

                // ✅ Cập nhật bán kính trong GeoFenceService NGAY LẬP TỨC
                _geoFenceService.UpdateRadius(radiusValue);

                Debug.WriteLine($"[SettingsPage] Radius updated to: {radiusValue}m - Display: {result}");

                // ✅ Hiển thị thông báo xác nhận
                await DisplayAlert("Thành công", $"Đã cập nhật bán kính kích hoạt thành {result}", "OK");
            }
        }

        private void OnBackgroundTrackingToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("BackgroundTracking", e.Value);

            if (e.Value)
            {
                Debug.WriteLine("[SettingsPage] Background Tracking: ON ✅");
                StartBackgroundTracking();
            }
            else
            {
                Debug.WriteLine("[SettingsPage] Background Tracking: OFF ❌");
                StopBackgroundTracking();
            }
        }

        private void StartBackgroundTracking()
        {
            try
            {
                Debug.WriteLine("[SettingsPage] 🔄 Bắt đầu theo dõi nền...");

                _ = _locationService.StartTrackingAsync(location =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Debug.WriteLine($"[SettingsPage] 📍 Location updated: {location.Latitude}, {location.Longitude}");
                        _geoFenceService.CheckLocation(location);
                    });
                });

                Debug.WriteLine("[SettingsPage] ✅ Background Tracking bắt đầu");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsPage] ❌ Lỗi khi bắt đầu tracking: {ex.Message}");
            }
        }

        private void StopBackgroundTracking()
        {
            try
            {
                Debug.WriteLine("[SettingsPage] 🛑 Dừng theo dõi nền...");
                _locationService.StopTracking();
                Debug.WriteLine("[SettingsPage] ✅ Background Tracking dừng");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsPage] ❌ Lỗi khi dừng tracking: {ex.Message}");
            }
        }

        private async void OnOfflinePackageClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Tải gói offline", "Tính năng tải gói offline đang được phát triển", "OK");
        }
    }
}