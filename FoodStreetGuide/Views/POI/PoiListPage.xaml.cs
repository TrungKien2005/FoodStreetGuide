using System.Diagnostics;
using doanC_.Config;
using doanC_.Helpers;
using doanC_.Models;
using doanC_.Services;
using doanC_.Services.Api;
using doanC_.Services.Audio;
using doanC_.Services.Data;
using doanC_.Services.Localization;
using Microsoft.Maui.Devices.Sensors;
using doanC_.Services.LocationTracking;

namespace doanC_.Views
{
    public partial class PoiListPage : ContentPage
    {
        private ApiService _apiService;
        private LocationService _locationService;
        private HybridTranslationService _translationService;
        private TTSService _ttsService;
        private SQLiteService _sqliteService;
        private List<LocationPoint> _allLocationPoints;
        private Location _currentLocation;
        private string _currentCategory = "Tất cả";
        private string _currentSearchText = string.Empty;
        private string _imageBaseUrl;
        private string _currentLanguage = "vi";

        // Binding properties cho đa ngôn ngữ
        public string PageTitle { get; set; } = "Địa điểm";
        public string PageSubtitle { get; set; } = "Khám phá các địa điểm thú vị";
        public string SearchPlaceholder { get; set; } = "Tìm kiếm địa điểm...";
        private DeviceTrackingService? _deviceTrackingService;

        // Dictionary lưu category và icon
        private readonly Dictionary<string, string> _categoryIcons = new()
 {
            { "Tất cả", "📌" },
            { "Ăn vặt", "🍢" },
          { "Đồ uống", "☕" },
            { "Đồ nướng", "🔥" },
     { "Hải sản", "🦐" },
            { "Chợ -Ẩm thực", "🛒" },
         { "Đi bộ", "🚶" },
         { "Di tích", "🏛️" },
            { "Thiên nhiên", "🌿" },
     { "Điểm cao", "🗼" },
         { "Nhà hàng", "🍽️" },
       { "Quán ăn", "🍜" },
            { "Cafe", "☕" },
            { "Bar - Pub", "🍺" }
        };

        public PoiListPage()
        {
            InitializeComponent();
            BindingContext = this;

            _apiService = new ApiService();
            _sqliteService = ServiceHelper.GetService<SQLiteService>();
            _translationService = new HybridTranslationService();
            _ttsService = ServiceHelper.GetService<TTSService>();
            _locationService = new LocationService();
            _deviceTrackingService = ServiceHelper.GetService<DeviceTrackingService>();
            _currentLanguage = Preferences.Get("AppLanguage", "vi");
            _translationService?.SetLanguage(_currentLanguage);
            AppResources.SetLanguage(_currentLanguage);
            UpdateUILanguage();
            Debug.WriteLine($"[PoiListPage] 🌐 Current app language: {_currentLanguage}");

            _imageBaseUrl = ApiConfig.GetBaseUrl().Replace("/api/LocationApi", "/images");
            Debug.WriteLine($"[PoiListPage] 🖼️ Image Base URL: {_imageBaseUrl}");

            // ✅ Lắng nghe sự kiện đổi ngôn ngữ từ Settings
            MessagingCenter.Subscribe<SettingsPage, string>(this, "LanguageChanged", async (sender, languageCode) =>
            {
                Debug.WriteLine($"[PoiListPage] 📢 Language changed to: {languageCode}");
                _currentLanguage = languageCode;
                _translationService?.SetLanguage(_currentLanguage);
                _translationService?.ClearCache();
                AppResources.SetLanguage(_currentLanguage);

                UpdateUILanguage();
                await FilterAndDisplayLocationsAsync();
            });

            LoadData();
            _ = GetCurrentLocationAndCalculateDistance();
        }

        private string GetImageUrl(string? imageName)
        {
            // Nếu không có tên ảnh, trả về ảnh mặc định
            if (string.IsNullOrEmpty(imageName))
            {
                Debug.WriteLine("[PoiListPage] ⚠️ No image name, using default");
                return "poi_default.png";
            }

            // Tạo URL đầy đủ
            var fullUrl = $"{_imageBaseUrl}/{imageName}";
            Debug.WriteLine($"[PoiListPage] 🖼️ Image URL: {fullUrl}");

            // Luôn trả về URL (Fallback sẽ xử lý khi ảnh lỗi)
            return fullUrl;
        }

        private async void LoadData()
        {
            try
            {
                loadingIndicator.IsVisible = true;
                loadingIndicator.IsRunning = true;

                var dbPoints = await _sqliteService.GetAllLocationPointsAsync();
                _allLocationPoints = dbPoints?.Where(p => p.IsApproved).ToList() ?? new List<LocationPoint>();

                if (_allLocationPoints != null && _allLocationPoints.Any())
                {
                    Debug.WriteLine($"[PoiListPage] ✅ Loaded {_allLocationPoints.Count} POI items from SQLite");

                    foreach (var poi in _allLocationPoints)
                    {
                        Debug.WriteLine($"[PoiListPage] 📍 POI: {poi.Name}, Image: {poi.Image ?? "NULL"}");
                    }

                    LoadCategoryFilters();
                    await FilterAndDisplayLocationsAsync();
                }
                else
                {
                    Debug.WriteLine("[PoiListPage] ⚠️ No data in SQLite, trying API...");
                    await LoadDataFromApiOnline();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] ❌ Error loading data: {ex.Message}");
            }
            finally
            {
                loadingIndicator.IsVisible = false;
                loadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadDataFromApiOnline()
        {
            try
            {
                var onlineData = await _apiService.GetLocationPointsAsync();
                if (onlineData != null && onlineData.Any())
                {
                    var approvedData = onlineData.Where(p => p.IsApproved).ToList();
                    
                    foreach (var poi in approvedData)
                    {
                        await _sqliteService.AddLocationPointAsync(poi);
                    }
                    _allLocationPoints = approvedData;

                    LoadCategoryFilters();
                    await FilterAndDisplayLocationsAsync();
                    Debug.WriteLine($"[PoiListPage] ✅ Loaded {onlineData.Count} POI items from API");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] ❌ API error: {ex.Message}");
            }
        }

        private void LoadCategoryFilters()
        {
            try
            {
                if (_allLocationPoints == null) return;

                var categories = _allLocationPoints
              .Where(p => !string.IsNullOrEmpty(p.Category))
              .Select(p => p.Category!)
       .Distinct()
                .OrderBy(c => c)
                     .ToList();

                var allCategories = new List<string> { "Tất cả" };
                allCategories.AddRange(categories);
                UpdateCategoryButtons(allCategories);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] Load categories error: {ex.Message}");
            }
        }

        private void UpdateCategoryButtons(List<string> categories)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var stackLayout = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Padding = new Thickness(0)
                };

                foreach (var category in categories)
                {
                    // Lấy icon từ dictionary
                    var icon = _categoryIcons.ContainsKey(category) ? _categoryIcons[category] : "📍";

                    // ✅ LẤY TÊN HIỂN THỊ TỪ AppResources (hỗ trợ đa ngôn ngữ)
                    string displayCategory = GetCategoryDisplayName(category);
                    var displayText = $"{icon} {displayCategory}";

                    var frame = new Frame
                    {
                        Padding = new Thickness(12, 6),
                        CornerRadius = 20,
                        HasShadow = false,
                        BackgroundColor = category == _currentCategory ? Color.FromArgb("#C85A3F") : Color.FromArgb("#E0D5CC"),
                        BorderColor = Colors.Transparent
                    };

                    var label = new Label
                    {
                        Text = displayText,
                        FontSize = 12,
                        FontAttributes = category == _currentCategory ? FontAttributes.Bold : FontAttributes.None,
                        TextColor = category == _currentCategory ? Colors.White : Color.FromArgb("#2C1810")
                    };

                    frame.Content = label;

                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => OnCategoryFilterClicked(category);
                    frame.GestureRecognizers.Add(tapGesture);

                    stackLayout.Children.Add(frame);
                }

                CategoryScrollView.Content = stackLayout;
            });
        }

        /// <summary>
        /// Chuyển đổi tên category sang ngôn ngữ hiện tại
        /// </summary>
        private string GetCategoryDisplayName(string category)
        {
            var categoryKey = category switch
            {
                "Tất cả" => "Category_All",
                "Ăn vặt" => "Category_Snack",
                "Đồ uống" => "Category_Drink",
                "Đồ nướng" => "Category_Grill",
                "Hải sản" => "Category_Seafood",
                "Chợ - Ẩm thực" => "Category_Market",
                "Đi bộ" => "Category_Walking",
                "Di tích" => "Category_Historical",
                "Thiên nhiên" => "Category_Nature",
                "Điểm cao" => "Category_HighPoint",
                "Nhà hàng" => "Category_Restaurant",
                "Quán ăn" => "Category_Eatery",
                "Cafe" => "Category_Cafe",
                "Bar - Pub" => "Category_Bar",
                "Ẩm thực" => "Category_Food",
                _ => category
            };

            var translated = AppResources.GetString(categoryKey);
            return translated == categoryKey ? category : translated;
        }

        private async void OnCategoryFilterClicked(string category)
        {
            _currentCategory = category;
            await FilterAndDisplayLocationsAsync();
            LoadCategoryFilters();
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = e.NewTextValue ?? string.Empty;
            await FilterAndDisplayLocationsAsync();
        }

        private async Task FilterAndDisplayLocationsAsync()
        {
            if (_allLocationPoints == null) return;

            var filtered = _allLocationPoints.AsEnumerable();

            if (_currentCategory != "Tất cả")
            {
                filtered = filtered.Where(p => p.Category == _currentCategory);
            }

            if (!string.IsNullOrWhiteSpace(_currentSearchText))
            {
                filtered = filtered.Where(p =>
            (p.Name?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                 (p.Description?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Address?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) == true));
            }

            var resultList = filtered.ToList();
            var poiItems = new List<PoiItem>();

            // ✅ OPTIMIZATION: Batch translate all text at once instead of one-by-one
            if (_currentLanguage != "vi" && _translationService != null)
            {
                var textsToTranslate = new List<string>();
                foreach (var location in resultList)
                {
                    if (!string.IsNullOrWhiteSpace(location.Name))
                        textsToTranslate.Add(location.Name);
                    if (!string.IsNullOrWhiteSpace(location.Description))
                        textsToTranslate.Add(location.Description);
                    if (!string.IsNullOrWhiteSpace(location.Address))
                        textsToTranslate.Add(location.Address);
                }

                // Batch translate
                var translations = await _translationService.TranslateBatchAsync(textsToTranslate, _currentLanguage);
                Debug.WriteLine($"[PoiListPage] ✅ Batch translated {translations.Count} items");

                foreach (var location in resultList)
                {
                    var originalName = location.Name ?? string.Empty;
                    var originalDescription = location.Description ?? string.Empty;
                    var originalAddress = location.Address ?? string.Empty;
                    var originalCategory = location.Category ?? string.Empty;

                    var displayName = translations.TryGetValue(originalName, out var transName) ? transName : originalName;
                    var displayDescription = translations.TryGetValue(originalDescription, out var transDesc) ? transDesc : originalDescription;
                    var displayAddress = translations.TryGetValue(originalAddress, out var transAddr) ? transAddr : originalAddress;

                    poiItems.Add(new PoiItem
                    {
                        PointId = location.PointId,
                        Name = displayName,
                        Description = displayDescription,
                        Distance = _currentLocation != null
                           ? (int)CalculateDistance(
                          _currentLocation.Latitude,
                 _currentLocation.Longitude,
                   location.Latitude,
               location.Longitude)
               : 0,
                        ImageUrl = GetImageUrl(location.Image),
                        Category = originalCategory,
                        Rating = location.Rating ?? 0,
                        ReviewCount = location.ReviewCount ?? 0,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    });
                }
            }
            else
            {
                // Language is Vietnamese or no translation service - use original text
                foreach (var location in resultList)
                {
                    poiItems.Add(new PoiItem
                    {
                        PointId = location.PointId,
                        Name = location.Name ?? string.Empty,
                        Description = location.Description ?? string.Empty,
                        Distance = _currentLocation != null
                              ? (int)CalculateDistance(
                 _currentLocation.Latitude,
                     _currentLocation.Longitude,
                       location.Latitude,
                location.Longitude)
                      : 0,
                        ImageUrl = GetImageUrl(location.Image),
                        Category = location.Category ?? string.Empty,
                        Rating = location.Rating ?? 0,
                        ReviewCount = location.ReviewCount ?? 0,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    });
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PoiCollection.ItemsSource = poiItems;
                Debug.WriteLine($"[PoiListPage] 📋 Displaying {poiItems.Count} items");

                if (poiItems.Any())
                {
                    Debug.WriteLine($"[PoiListPage] 🖼️ First item image URL: {poiItems.First().ImageUrl}");
                }
            });
        }

        private async Task GetCurrentLocationAndCalculateDistance()
        {
            try
            {
                _currentLocation = await _locationService.GetCurrentLocationAsync();
                if (_currentLocation != null)
                {
                    await FilterAndDisplayLocationsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] Error getting location: {ex.Message}");
            }
        }

        private async void OnPoiTapped(object sender, TappedEventArgs e)
        {
            try
            {
                var frame = sender as Frame;
                if (frame?.BindingContext is PoiItem selectedPoi)
                {
                    // 👈 TRACK KHI CHỌN POI
                    TrackActivity("SelectPOI", selectedPoi.PointId);

                    await Shell.Current.GoToAsync($"///PoiDetailPage?poiId={selectedPoi.PointId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] Error: {ex.Message}");
            }
        }

        private async void OnPlayButtonTapped(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null) return;

                var frame = button.Parent?.Parent?.Parent as Frame;
                if (frame?.BindingContext is PoiItem selectedPoi)
                {
                    await HandlePlayButton(selectedPoi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] Play button error: {ex.Message}");
            }
        }

        // Sửa method HandlePlayButton - Thêm gọi API ghi nhận TTS

        // Trong HandlePlayButton của PoiListPage
        private async Task HandlePlayButton(PoiItem selectedPoi)
        {
            try
            {
                var locationPoint = _allLocationPoints?.FirstOrDefault(l => l.PointId == selectedPoi.PointId);
                if (locationPoint == null) return;

                TrackActivity("TTSListen", selectedPoi.PointId);

                DateTime playStartTime = DateTime.Now;

                var savedLanguage = Preferences.Get("AppLanguage", "vi");
                var selectedVoice = Preferences.Get("SelectedVoice", AppResources.GetString("FemaleVoice"));

                var originalText = locationPoint.Description ?? locationPoint.Name;
                var textToSpeak = originalText;

                if (savedLanguage != "vi" && _translationService != null && !string.IsNullOrWhiteSpace(originalText))
                {
                    try
                    {
                        textToSpeak = await _translationService.TranslateTextAsync(originalText, savedLanguage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PoiListPage] TTS translate error: {ex.Message}");
                        textToSpeak = originalText;
                    }
                }

                try
                {
                    await _ttsService.SpeakAsync(textToSpeak, GetLanguageCodeForTTS(savedLanguage), selectedVoice);

                    var duration = (int)(DateTime.Now - playStartTime).TotalSeconds;

                    // ✅ THÊM LOG TRƯỚC KHI GỌI API
                    Debug.WriteLine($"[PoiListPage] 🚀 Calling RecordTTSListenAsync for POI: {selectedPoi.PointId}, duration: {duration}s");

                    string deviceId = Microsoft.Maui.Devices.DeviceInfo.Current.Name ?? "Unknown";
                    int languageId = GetLanguageId(savedLanguage);
                    bool recorded = await _apiService.RecordTTSListenAsync(selectedPoi.PointId, languageId, duration, deviceId);

                    Debug.WriteLine($"[PoiListPage] 📊 TTS Listen recorded: {recorded}");

                    if (!recorded)
                    {
                        Debug.WriteLine($"[PoiListPage] ⚠️ Failed to record TTS listen - check API connection");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PoiListPage] ❌ SpeakAsync error: {ex.Message}");
                    var audioCommentaryTitle = AppResources.GetString("AudioPlayerTitle");
                    await DisplayAlert(audioCommentaryTitle, textToSpeak, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] HandlePlayButton error: {ex.Message}");
            }
        }

        // Thêm helper method GetLanguageId
        private int GetLanguageId(string languageCode)
        {
            return languageCode switch
            {
                "vi" => 1,
                "en" => 2,
                "zh" => 3,
                "ja" => 4,
                "ko" => 5,
                _ => 1
            };
        }

        private string GetLanguageCodeForTTS(string languageCode)
        {
            return languageCode switch
            {
                "en" => "en-US",
                "vi" => "vi-VN",
                "fr" => "fr-FR",
                "es" => "es-ES",
                "zh" => "zh-CN",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
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

        private async void TrackActivity(string activityType, int pointId = 0)
        {
            try
            {
                if (_deviceTrackingService != null)
                {
                    await _deviceTrackingService.TrackActivityAsync(activityType, pointId);
                    Debug.WriteLine($"[PoiListPage] 📊 Tracked activity: {activityType} for POI: {pointId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiListPage] ⚠️ Track error: {ex.Message}");
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            TrackActivity("ViewList");
            // Kiểm tra ngôn ngữ thay đổi khi quay lại trang
            var currentLanguage = Preferences.Get("AppLanguage", "vi");
            if (currentLanguage != _currentLanguage)
            {
                _currentLanguage = currentLanguage;
                _translationService?.SetLanguage(_currentLanguage);
                _translationService?.ClearCache();
                await FilterAndDisplayLocationsAsync();
            }

            // Cập nhật khoảng cách nếu vị trí thay đổi
            await GetCurrentLocationAndCalculateDistance();
        }

        private void UpdateUILanguage()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PageTitle = AppResources.GetString("PoiListTitle");
                PageSubtitle = AppResources.GetString("PoiListSubtitle");
                SearchPlaceholder = AppResources.GetString("PoiSearchPlaceholder");

                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
                OnPropertyChanged(nameof(SearchPlaceholder));
            });
        }
    }

    public class PoiItem
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Distance { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}