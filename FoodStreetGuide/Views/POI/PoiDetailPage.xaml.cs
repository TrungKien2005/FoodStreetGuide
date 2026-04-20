using System.Diagnostics;
using doanC_.Config;
using doanC_.Helpers;
using doanC_.Models;
using doanC_.Services;
using doanC_.Services.Api;
using doanC_.Services.Audio;
using doanC_.Services.Data;
using doanC_.Services.Localization;
using doanC_.ViewModels;
using Microsoft.Maui.Devices.Sensors;
using doanC_.Services.LocationTracking;

namespace doanC_.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class PoiDetailPage : ContentPage
{
    private ApiService _apiService;
    private LocationService _locationService;
    private HybridTranslationService _translationService;
    private TTSService _ttsService;
    private SQLiteService _sqliteService;
    private PoiDetailViewModel _viewModel;
    private LocationPoint _currentPoi;
    private Location _userLocation;
    private bool _isAudioPlayerVisible = false;
    private string _currentLanguage = "vi";
    private string _lastLanguage = "vi";
    private bool _isPlaying = false;
    private string _originalDescription = "";
    private string _originalName = "";
    private string _originalAddress = "";
    private string _originalCategory = "";
    private string _originalOpeningHours = "";
    private string _originalPriceRange = "";
    private string _imageBaseUrl;
    private bool _isTranslating = false;
    private DeviceTrackingService? _deviceTrackingService;

    private int _poiId;
    public int PoiId
    {
        get => _poiId;
        set
        {
            _poiId = value;
            _ = LoadPoiDetailsAsync(_poiId);
        }
    }

    public PoiDetailPage()
    {
        InitializeComponent();

        _viewModel = new PoiDetailViewModel();
        BindingContext = _viewModel;

        _apiService = new ApiService();
        _translationService = new HybridTranslationService();
        _ttsService = ServiceHelper.GetService<TTSService>();
        _locationService = new LocationService();
        _sqliteService = ServiceHelper.GetService<SQLiteService>();
        _deviceTrackingService = ServiceHelper.GetService<DeviceTrackingService>();
        _imageBaseUrl = ApiConfig.GetBaseUrl().Replace("/api/LocationApi", "/images");

        LoadSavedLanguage();
        InitializeAudioLanguagePicker();

        // ✅ Lắng nghe sự kiện đổi ngôn ngữ từ Settings
        MessagingCenter.Subscribe<SettingsPage, string>(this, "LanguageChanged", async (sender, languageCode) =>
        {
            Debug.WriteLine($"[PoiDetailPage] 📢 Received language change: {languageCode}");
            _currentLanguage = languageCode;
            _translationService?.SetLanguage(_currentLanguage);
            _translationService?.ClearCache();
            AppResources.SetLanguage(_currentLanguage);

            // ✅ Update all UI text elements
            UpdateAllUIText();

            if (_currentPoi != null)
            {
                if (_currentLanguage == "vi")
                {
                    UpdatePoiUI();
                }
                else
                {
                    UpdatePoiUI();
                    await TranslateAllPoiTextsAsync();
                }
            }

            // Cập nhật Picker
            if (AudioLanguagePicker != null)
            {
                AudioLanguagePicker.SelectedIndex = GetLanguageIndex(_currentLanguage);
            }
        });
    }

    private void LoadSavedLanguage()
    {
        _currentLanguage = Preferences.Get("AppLanguage", "vi");
        _lastLanguage = _currentLanguage;
        Debug.WriteLine($"[PoiDetailPage] Loaded saved language: {_currentLanguage}");

        if (_translationService != null)
        {
            _translationService.SetLanguage(_currentLanguage);
        }
    }

    private void InitializeAudioLanguagePicker()
    {
        if (AudioLanguagePicker != null)
        {
            AudioLanguagePicker.SelectedIndex = GetLanguageIndex(_currentLanguage);
            AudioLanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
            AudioLanguagePicker.SelectedIndexChanged += OnLanguageChanged;
        }
    }

    private int GetLanguageIndex(string languageCode)
    {
        return languageCode switch
        {
            "vi" => 0,
            "en" => 1,
            "zh" => 2,
            "ja" => 3,
            "ko" => 4,
            _ => 0
        };
    }

    private async Task ReloadWithNewLanguage()
    {
        if (_isTranslating || _currentPoi == null)
        {
            Debug.WriteLine("[PoiDetailPage] Skipping reload - already translating or no POI");
            return;
        }

        Debug.WriteLine($"[PoiDetailPage] 🔄 Reloading with language: {_currentLanguage}");
        _isTranslating = true;

        try
        {
            // Cập nhật Picker
            if (AudioLanguagePicker != null)
            {
                AudioLanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
                AudioLanguagePicker.SelectedIndex = GetLanguageIndex(_currentLanguage);
                AudioLanguagePicker.SelectedIndexChanged += OnLanguageChanged;
            }

            // ✅ Cập nhật UI buttons text
            UpdateUIButtonsLanguage();

            // ✅ Cập nhật tất cả UI text
            UpdateAllUIText();

            // Dịch nội dung
            if (_currentLanguage == "vi")
            {
                UpdatePoiUI();
            }
            else
            {
                await TranslateAllPoiTextsAsync();
            }

            _lastLanguage = _currentLanguage;
            Debug.WriteLine($"[PoiDetailPage] ✅ Reload completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ❌ Reload error: {ex.Message}");
        }
        finally
        {
            _isTranslating = false;
        }
    }

    private void UpdateUIButtonsLanguage()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (AudioButton != null)
            {
                AudioButton.Text = AppResources.GetString("AudioButton");
            }

            if (DirectionsButton != null)
            {
                DirectionsButton.Text = AppResources.GetString("DirectionsButton");
            }
        });
    }
    // ✅ NEW: Update all UI text elements based on language
    private void UpdateAllUIText()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update Audio Language Picker Title
            if (AudioLanguagePicker != null)
            {
                AudioLanguagePicker.Title = AppResources.GetString("AudioLanguagePickerTitle");
            }

            // Update Play/Pause button text (if not playing)
            if (PlayPauseButton != null && !_isPlaying)
            {
                PlayPauseButton.Text = AppResources.GetString("PlayButton");
            }

            // Update Open Status button text
            if (OpenStatusButton != null)
            {
                OpenStatusButton.Text = AppResources.GetString("OpenStatusButtonText");
            }

            // Update Audio and Directions button texts
            UpdateUIButtonsLanguage();

            Debug.WriteLine($"[PoiDetailPage] ✅ Updated all UI text to language: {_currentLanguage}");
        });
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // ✅ Kiểm tra ngôn ngữ thay đổi khi quay lại trang
        var currentLanguage = Preferences.Get("AppLanguage", "vi");
        if (currentLanguage != _lastLanguage && _currentPoi != null)
        {
            _currentLanguage = currentLanguage;
            AppResources.SetLanguage(_currentLanguage);
            _viewModel?.RefreshLanguage();
            await ReloadWithNewLanguage();
        }

        await GetCurrentLocationAndCalculateDistance();
    }

    private async Task LoadPoiDetailsAsync(int poiId)
    {
        try
        {
            if (poiId == 0)
            {
                Debug.WriteLine("[PoiDetailPage] Invalid POI ID: 0");
                await DisplayAlert("Lỗi", "ID địa điểm không hợp lệ", "OK");
                await GoBackAsync();
                return;
            }

            Debug.WriteLine($"[PoiDetailPage] Loading POI with ID: {poiId}");

            if (_sqliteService == null)
            {
                Debug.WriteLine("[PoiDetailPage] SQLiteService is null");
                await DisplayAlert("Lỗi", "Không thể kết nối cơ sở dữ liệu", "OK");
                await GoBackAsync();
                return;
            }

            _currentPoi = await _sqliteService.GetLocationPointByIdAsync(poiId);

            if (_currentPoi == null)
            {
                Debug.WriteLine($"[PoiDetailPage] POI not found with ID: {poiId}");
                await DisplayAlert("Thông báo", "Không tìm thấy địa điểm", "OK");
                await GoBackAsync();
                return;
            }

            UpdatePoiUI();
            // ✅ Update all UI text on initial load
            UpdateAllUIText();

            if (_currentLanguage != "vi")
            {
                await TranslateAllPoiTextsAsync();
            }

            await GetCurrentLocationAndCalculateDistance();

            Debug.WriteLine($"[PoiDetailPage] Successfully loaded POI: {_currentPoi.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error loading POI: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể tải dữ liệu: {ex.Message}", "OK");
            await GoBackAsync();
        }
    }

    private async Task TranslateAllPoiTextsAsync()
    {
        try
        {
            if (_translationService == null)
            {
                Debug.WriteLine("[PoiDetailPage] Translation service is null");
                return;
            }

            if (_currentLanguage == "vi")
                return;

            Debug.WriteLine($"[PoiDetailPage] Translating to {_currentLanguage}");

            // ✅ OPTIMIZATION: Batch translate all texts at once
            var textsToTranslate = new List<string>
         {
      _originalName,
   _originalDescription,
    _originalAddress,
   _originalCategory,
     _originalOpeningHours,
_originalPriceRange
    };

            var translations = await _translationService.TranslateBatchAsync(textsToTranslate, _currentLanguage);
            Debug.WriteLine($"[PoiDetailPage] ✅ Batch translated {translations.Count} items");

            var displayName = translations.TryGetValue(_originalName, out var transName) ? transName : _originalName;
            var displayDescription = translations.TryGetValue(_originalDescription, out var transDesc) ? transDesc : _originalDescription;
            var displayAddress = translations.TryGetValue(_originalAddress, out var transAddr) ? transAddr : _originalAddress;
            var displayCategory = translations.TryGetValue(_originalCategory, out var transCat) ? transCat : _originalCategory;
            var displayOpeningHours = translations.TryGetValue(_originalOpeningHours, out var transHours) ? transHours : _originalOpeningHours;
            var displayPrice = translations.TryGetValue(_originalPriceRange, out var transPrice) ? transPrice : _originalPriceRange;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (PoiNameLabel != null)
                    PoiNameLabel.Text = displayName;

                if (DescriptionLabel != null)
                    DescriptionLabel.Text = displayDescription;

                if (AddressLabel != null)
                    AddressLabel.Text = displayAddress;

                if (CategoryLabel != null)
                    CategoryLabel.Text = displayCategory.ToUpperInvariant();

                if (OpeningHoursLabel != null)
                    OpeningHoursLabel.Text = string.IsNullOrEmpty(displayOpeningHours) ? "Đang cập nhật" : displayOpeningHours;

                if (PriceLabel != null)
                    PriceLabel.Text = string.IsNullOrEmpty(displayPrice) ? GetPriceRangeDisplay(_originalPriceRange) : displayPrice;
            });

            Debug.WriteLine($"[PoiDetailPage] ✅ Translation completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ❌ Translation error: {ex.Message}");
        }
    }

    private void UpdatePoiUI()
    {
        Debug.WriteLine("[PoiDetailPage] ✅ UpdatePoiUI called");

        if (_currentPoi == null)
        {
            Debug.WriteLine("[PoiDetailPage] _currentPoi is null in UpdatePoiUI");
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _originalName = _currentPoi.Name ?? string.Empty;
                _originalDescription = _currentPoi.Description ?? "Chưa có mô tả";
                _originalAddress = _currentPoi.Address ?? "Đang cập nhật";
                _originalCategory = _currentPoi.Category ?? "Địa điểm";
                _originalOpeningHours = _currentPoi.OpeningHours ?? string.Empty;
                _originalPriceRange = _currentPoi.PriceRange ?? string.Empty;

                Title = _originalName;

                if (MainImage != null)
                {
                    if (!string.IsNullOrEmpty(_currentPoi.Image))
                    {
                        var imageUrl = $"{_imageBaseUrl}/{_currentPoi.Image}";
                        MainImage.Source = imageUrl;
                    }
                    else
                    {
                        MainImage.Source = "poi_default.png";
                    }
                }

                if (PoiNameLabel != null)
                    PoiNameLabel.Text = _originalName;

                if (RatingLabel != null)
                {
                    var rating = _currentPoi.Rating ?? 0;
                    var reviewCount = _currentPoi.ReviewCount ?? 0;
                    RatingLabel.Text = rating > 0 ? $"★ {rating:F1} ({reviewCount} đánh giá)" : "★ Chưa có đánh giá";
                }

                if (CategoryLabel != null)
                    CategoryLabel.Text = _originalCategory.ToUpperInvariant();

                if (AddressLabel != null)
                    AddressLabel.Text = _originalAddress;

                if (DescriptionLabel != null)
                    DescriptionLabel.Text = _originalDescription;

                if (OpeningHoursLabel != null)
                {
                    OpeningHoursLabel.Text = string.IsNullOrEmpty(_originalOpeningHours)
                ? "Đang cập nhật"
                 : _originalOpeningHours;
                }

                if (PriceLabel != null)
                {
                    PriceLabel.Text = GetPriceRangeDisplay(_originalPriceRange);
                    PriceLabel.IsVisible = true;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ❌ Error in UpdatePoiUI: {ex.Message}");
        }
    }

    private string GetPriceRangeDisplay(string priceRange)
    {
        if (string.IsNullOrEmpty(priceRange))
            return "💰 Liên hệ để biết giá";

        return priceRange.ToLower() switch
        {
            "rẻ" => "💰 10.000 – 50.000đ",
            "trung bình" => "💰 70.000 – 150.000đ",
            "cao" => "💰 150.000 – 300.000đ",
            _ => $"💰 {priceRange}"
        };
    }

    private void UpdateDistance()
    {
        if (_userLocation == null || _currentPoi == null)
            return;

        try
        {
            double distance = CalculateDistance(
     _userLocation.Latitude,
          _userLocation.Longitude,
             _currentPoi.Latitude,
          _currentPoi.Longitude
    );
            Debug.WriteLine($"[PoiDetailPage] Distance: {(int)distance}m");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error calculating distance: {ex.Message}");
        }
    }

    private async Task GetCurrentLocationAndCalculateDistance()
    {
        try
        {
            if (_locationService != null)
            {
                _userLocation = await _locationService.GetCurrentLocationAsync();
                if (_userLocation != null)
                {
                    UpdateDistance();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error getting location: {ex.Message}");
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("[PoiDetailPage] Back button clicked");
            await GoBackAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error going back: {ex.Message}");
        }
    }

    private async Task GoBackAsync()
    {
        try
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("///PoiListPage");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] GoBackAsync error: {ex.Message}");
            await Navigation.PopAsync();
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        try
        {
            await DisplayAlert("Yêu thích", $"Đã thêm {_currentPoi?.Name} vào danh sách yêu thích", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error: {ex.Message}");
        }
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentPoi != null)
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = $"📍 {_originalName}\n📝 {_originalDescription}\n📌 {_originalAddress}",
                    Title = _originalName
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error sharing: {ex.Message}");
        }
    }

    private async void OnOpenStatusClicked(object sender, EventArgs e)
    {
        var status = _currentLanguage == "vi" ? "Địa điểm đang mở cửa phục vụ" : "The place is open for service";
        await DisplayAlert("Thông tin", status, "OK");
    }

    private void OnPlayAudioClicked(object sender, EventArgs e)
    {
        try
        {
            _isAudioPlayerVisible = !_isAudioPlayerVisible;
            if (AudioPlayerFrame != null)
                AudioPlayerFrame.IsVisible = _isAudioPlayerVisible;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error: {ex.Message}");
        }
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentPoi == null)
                return;

            var mapUrl = $"https://www.google.com/maps/search/?api=1&query={_currentPoi.Latitude},{_currentPoi.Longitude}";
            await Launcher.Default.OpenAsync(mapUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] Error opening maps: {ex.Message}");
            var msg = _currentLanguage == "vi" ? "Không thể mở bản đồ" : "Cannot open map";
            await DisplayAlert("Lỗi", msg, "OK");
        }
    }

    private async void OnLanguageChanged(object sender, EventArgs e)
    {
        try
        {
            if (AudioLanguagePicker?.SelectedIndex < 0 || _currentPoi == null)
                return;

            var selectedLanguage = AudioLanguagePicker.Items[AudioLanguagePicker.SelectedIndex];
            var newLanguage = GetLanguageCode(selectedLanguage);

            if (newLanguage == _currentLanguage)
                return;

            _currentLanguage = newLanguage;
            AppResources.SetLanguage(_currentLanguage);

            Debug.WriteLine($"[PoiDetailPage] Language changed to: {selectedLanguage} (Code: {_currentLanguage})");

            Preferences.Set("AppLanguage", _currentLanguage);
            _translationService?.SetLanguage(_currentLanguage);
            _translationService?.ClearCache();

            // ✅ Cập nhật ViewModel
            _viewModel?.RefreshLanguage();

            await ReloadWithNewLanguage();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ❌ Error in OnLanguageChanged: {ex.Message}");
        }
    }
    private async void TrackActivity(string activityType, int pointId = 0)
    {
        try
        {
            if (_deviceTrackingService != null)
            {
                await _deviceTrackingService.TrackActivityAsync(activityType, pointId);
                Debug.WriteLine($"[PoiDetailPage] 📊 Tracked activity: {activityType} for POI: {pointId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ⚠️ Track activity error: {ex.Message}");
        }
    }
    private async void OnPlayPauseClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentPoi == null || PlayPauseButton == null)
            {
                await DisplayAlert("Thông báo", "Chưa có dữ liệu để phát", "OK");
                return;
            }

            if (_isPlaying)
            {
                Debug.WriteLine("[PoiDetailPage] ⏹️ Stopping playback immediately");
                await _ttsService.StopImmediatelyAsync();
                _isPlaying = false;
                PlayPauseButton.Text = "▶";
                return;
            }

            _isPlaying = true;
            PlayPauseButton.Text = "⏸";

            // 👈 TRACK KHI NGƯỜI DÙNG PHÁT AUDIO
            TrackActivity("TTSListen", _currentPoi.PointId);

            var sourceText = _originalDescription ?? _originalName;

            if (string.IsNullOrEmpty(sourceText))
            {
                Debug.WriteLine("[PoiDetailPage] ❌ No text to speak");
                _isPlaying = false;
                PlayPauseButton.Text = "▶";
                return;
            }

            string finalText = sourceText;
            if (_translationService != null && _currentLanguage != "vi")
            {
                try
                {
                    finalText = await _translationService.TranslateTextAsync(sourceText, _currentLanguage);
                }
                catch (Exception transEx)
                {
                    Debug.WriteLine($"[PoiDetailPage] ⚠️ Translation failed: {transEx.Message}");
                    finalText = sourceText;
                }
            }

            await _ttsService.SpeakAsync(finalText, GetLanguageCodeForTTS(_currentLanguage));

            _isPlaying = false;
            PlayPauseButton.Text = "▶";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiDetailPage] ❌ Error: {ex.Message}");
            if (PlayPauseButton != null)
                PlayPauseButton.Text = "▶";
            _isPlaying = false;
            await DisplayAlert("Lỗi", $"Không thể phát âm thanh: {ex.Message}", "OK");
        }
    }
    // 👈 THÊM TRACK KHI MỞ TRANG DETAIL
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Kiểm tra ngôn ngữ thay đổi khi quay lại trang
        var currentLanguage = Preferences.Get("AppLanguage", "vi");
        if (currentLanguage != _lastLanguage && _currentPoi != null)
        {
            _currentLanguage = currentLanguage;
            AppResources.SetLanguage(_currentLanguage);
            _viewModel?.RefreshLanguage();
            _ = ReloadWithNewLanguage();
        }

        // 👈 THÊM DÒNG NÀY - TRACK KHI XEM CHI TIẾT POI
        if (_currentPoi != null)
        {
            TrackActivity("ViewDetail", _currentPoi.PointId);
            Debug.WriteLine($"[PoiDetailPage] 📊 Tracked ViewDetail for POI: {_currentPoi.PointId}");
        }

        _ = GetCurrentLocationAndCalculateDistance();
    }

    private string GetLanguageCode(string languageName)
    {
        if (string.IsNullOrEmpty(languageName))
            return "vi";

        if (languageName.Contains("Tiếng Việt") || languageName.Contains("Việt")) return "vi";
        if (languageName.Contains("English")) return "en";
        if (languageName.Contains("中文")) return "zh";
        if (languageName.Contains("日本語")) return "ja";
        if (languageName.Contains("한국어")) return "ko";

        return "vi";
    }

    private string GetLanguageCodeForTTS(string languageCode)
    {
        return languageCode switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            "ja" => "ja-JP",
            "ko" => "ko-KR",
            "vi" => "vi-VN",
            _ => "vi-VN"
        };
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000;
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
           Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRad(double val) => val * Math.PI / 180;
}