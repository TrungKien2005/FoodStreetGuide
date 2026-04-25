using System.Diagnostics;
using System.Text;
using doanC_.Helpers;
using doanC_.Models;
using doanC_.Services;
using doanC_.Services.Data;
using doanC_.Services.Geo;
using doanC_.Services.Localization;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Media;
using Map = Microsoft.Maui.Controls.Maps.Map;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using doanC_.Services.Api;

namespace doanC_.Views;

public partial class MapPage : ContentPage, INotifyPropertyChanged
{
    private readonly SQLiteService _sqliteService;
    private readonly GeoFenceService _geoFenceService;
    private readonly LocationService _locationService;
    private readonly HybridTranslationService _translationService;
    private List<LocationPoint> _allPoints = new();
    private List<LocationPoint> _originalPoints = new();
    private Dictionary<string, Dictionary<string, string>> _translationCache = new();
    private Location _currentLocation;
    private double _currentRadius = 15;
    private LocationPoint _currentSelectedPoi;
    private bool _isFirstLoad = true;
    private Task _loadPointsTask;
    private CancellationTokenSource _gpsTokenSource;
    private string _currentLanguage = "vi";
    private string _lastLanguage = "vi";
    private bool _isTranslating = false;
    private Circle? _radiusCircle;
    private Dictionary<int, DateTime> _cooldownQueue = new();

    // Binding properties

    // ✅ CÁCH ĐÚNG (CÓ NOTIFY UI)
    private string _searchPlaceholder = "Tìm điểm thuyết minh...";
    public string SearchPlaceholder
    {
        get => _searchPlaceholder;
        set { if (_searchPlaceholder != value) { _searchPlaceholder = value; OnPropertyChanged(); } }
    }

    private string _languageCode = "VI";
    public string LanguageCode
    {
        get => _languageCode;
        set { if (_languageCode != value) { _languageCode = value; OnPropertyChanged(); } }
    }

    private string _radiusDisplayText = "Bán kính kích hoạt: 15m";
    public string RadiusDisplayText
    {
        get => _radiusDisplayText;
        set { if (_radiusDisplayText != value) { _radiusDisplayText = value; OnPropertyChanged(); } }
    }

    private string _nearbyText = "Đang tìm điểm gần bạn...";
    public string NearbyText
    {
        get => _nearbyText;
        set { if (_nearbyText != value) { _nearbyText = value; OnPropertyChanged(); } }
    }

    private string _poiNameText = "Chưa có điểm nào";
    public string PoiNameText
    {
        get => _poiNameText;
        set { if (_poiNameText != value) { _poiNameText = value; OnPropertyChanged(); } }
    }

    private string _listenText = "Nhấn nút bên dưới để nghe thuyết minh";
    public string ListenText
    {
        get => _listenText;
        set { if (_listenText != value) { _listenText = value; OnPropertyChanged(); } }
    }

    private string _playButtonText = "🔊 NGHE THUYẾT MINH";
    public string PlayButtonText
    {
        get => _playButtonText;
        set { if (_playButtonText != value) { _playButtonText = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MapPage()
    {
        InitializeComponent();
        _sqliteService = ServiceHelper.GetService<SQLiteService>();
        _geoFenceService = GeoFenceService.Instance;
        _locationService = ServiceHelper.GetService<LocationService>();
        _translationService = new HybridTranslationService();

        LoadRadiusFromPreferences();
        LoadSavedLanguage();
        UpdateRadiusDisplay();
        UpdateUILanguage();
        InitializeMap();

        // ✅ THÊM ĐOẠN NÀY - LẮNG NGHE THÔNG BÁO ĐỔI NGÔN NGỮ
        MessagingCenter.Subscribe<SettingsPage, string>(this, "LanguageChanged", async (sender, languageCode) =>
        {
            Debug.WriteLine($"[MapPage] 🔔 Language changed to: {languageCode}");
            _currentLanguage = languageCode;
            _translationService?.SetLanguage(_currentLanguage);
            _translationService?.ClearCache();
            RefreshUITexts();
            await TranslateAndDisplayPoints();
            if (_currentSelectedPoi != null)
            {
                UpdateSelectedPoiTexts(_currentSelectedPoi);
            }
        });
        // ✅ THÊM ĐOẠN NÀY - LẮNG NGHE THÔNG BÁO ĐỔI BÁN KÍNH (TỪ SETTINGS)
        MessagingCenter.Subscribe<SettingsPage, double>(this, "RadiusChanged", (sender, newRadius) =>
        {
            Debug.WriteLine($"[MapPage] 📢 Radius changed to: {newRadius}m");
            _currentRadius = newRadius;
            UpdateRadiusDisplay();
            if (_currentLocation != null)
            {
                DrawRadiusCircle(new Location(_currentLocation.Latitude, _currentLocation.Longitude));
            }
            CheckNearbyPoints();
        });
    }

    private void LoadSavedLanguage()
    {
        _currentLanguage = Preferences.Get("AppLanguage", "vi");
        _lastLanguage = _currentLanguage;
        Debug.WriteLine($"[MapPage] Loaded language: {_currentLanguage}");
        _translationService?.SetLanguage(_currentLanguage);
    }

    private void UpdateUILanguage()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SearchPlaceholder = AppResources.GetString("PoiSearchPlaceholder");
            LanguageCode = _currentLanguage.ToUpper();
            NearbyText = AppResources.GetString("NearbyDistance");
            PoiNameText = AppResources.GetString("SelectPoi");
            ListenText = AppResources.GetString("TapToListen2");
            PlayButtonText = AppResources.GetString("PlayCommentary");

            // ✅ THÊM DÒNG NÀY - CẬP NHẬT RADIUS DISPLAY
            var prefix = _currentLanguage == "vi" ? "Bán kính kích hoạt" : "Activation radius";
            RadiusDisplayText = $"{prefix}: {_currentRadius}m";

            // Gọi OnPropertyChanged cho tất cả
            OnPropertyChanged(nameof(SearchPlaceholder));
            OnPropertyChanged(nameof(LanguageCode));
            OnPropertyChanged(nameof(RadiusDisplayText));
            OnPropertyChanged(nameof(NearbyText));
            OnPropertyChanged(nameof(PoiNameText));
            OnPropertyChanged(nameof(ListenText));
            OnPropertyChanged(nameof(PlayButtonText));

            // Cập nhật trực tiếp các control (dự phòng)
            //if (SearchEntry != null) SearchEntry.Placeholder = SearchPlaceholder;
            //if (LanguageLabel != null) LanguageLabel.Text = LanguageCode;
            if (RadiusDisplayLabel != null) RadiusDisplayLabel.Text = RadiusDisplayText;
            if (PlayButton != null) PlayButton.Text = PlayButtonText;
            if (NearbyLabel != null) NearbyLabel.Text = NearbyText;
            if (PoiNameLabel != null) PoiNameLabel.Text = PoiNameText;
            if (ListenTextLabel != null) ListenTextLabel.Text = ListenText;
        });
    }

    private void UpdateRadiusDisplay()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var prefix = _currentLanguage == "vi" ? "Bán kính kích hoạt" : "Activation radius";
            RadiusDisplayText = $"{prefix}: {_currentRadius}m";
            OnPropertyChanged(nameof(RadiusDisplayText));
            if (RadiusDisplayLabel != null) RadiusDisplayLabel.Text = RadiusDisplayText;
        });
    }

    private void LoadRadiusFromPreferences()
    {
        _currentRadius = Preferences.Get("GeoFenceRadiusValue", 15.0);
    }

    private async void InitializeMap()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    _currentLocation = location;

                    // ✅ THÊM DÒNG NÀY - VẼ VÒNG TRÒN LẦN ĐẦU
                    DrawRadiusCircle(new Location(location.Latitude, location.Longitude));

                    var mapSpan = MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),
                             Distance.FromMeters(500));
                    map.MoveToRegion(mapSpan);
                    await LoadPoints();
                    CheckNearbyPoints();
                }
                StartContinuousGpsTracking();
            }
            else
            {
                var errorMsg = _currentLanguage == "vi" ? "Không thể truy cập vị trí của bạn" : "Cannot access your location";
                await DisplayAlert(_currentLanguage == "vi" ? "Lỗi" : "Error", errorMsg, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapPage] InitializeMap error: {ex.Message}");
        }
    }

    private void StartContinuousGpsTracking()
    {
        try
        {
            _gpsTokenSource?.Cancel();
            _gpsTokenSource = new CancellationTokenSource();

            _locationService.StartTrackingAsync(location =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentLocation = location;

                    // ✅ THÊM DÒNG NÀY - VẼ VÒNG TRÒN KHI VỊ TRÍ THAY ĐỔI
                    DrawRadiusCircle(new Location(location.Latitude, location.Longitude));

                    if (_isFirstLoad)
                    {
                        _isFirstLoad = false;
                        var mapSpan = MapSpan.FromCenterAndRadius(
                             new Location(location.Latitude, location.Longitude),
                                Distance.FromMeters(500));
                        map.MoveToRegion(mapSpan);
                    }
                    CheckNearbyPoints();
                });
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapPage] StartContinuousGpsTracking error: {ex.Message}");
        }
    }

    private Task LoadPoints()
    {
        if (_loadPointsTask == null || _loadPointsTask.IsCompleted)
        {
            _loadPointsTask = LoadPointsInternal();
        }
        return _loadPointsTask;
    }

    private async Task LoadPointsInternal()
    {
        try
        {
            var allDbPoints = await _sqliteService.GetAllLocationPointsAsync();
            _originalPoints = allDbPoints?.Where(p => p.IsApproved).ToList() ?? new List<LocationPoint>();

            if (_originalPoints != null && _originalPoints.Any())
            {
                await TranslateAndDisplayPoints();
            }
            else
            {
                var msg = _currentLanguage == "vi" ? "Không có điểm thuyết minh nào" : "No commentary points";
                await DisplayAlert("Thông báo", msg, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapPage] LoadPoints error: {ex.Message}");
        }
    }

    private async Task TranslateAndDisplayPoints()
    {
        if (_originalPoints == null || !_originalPoints.Any()) return;

        var newPoints = new List<LocationPoint>();

        foreach (var original in _originalPoints)
        {
            var translated = new LocationPoint
            {
                PointId = original.PointId,
                Latitude = original.Latitude,
                Longitude = original.Longitude,
                Address = original.Address,
                Category = original.Category,
                Image = original.Image,
                Rating = original.Rating,
                ReviewCount = original.ReviewCount,
                OpeningHours = original.OpeningHours,
                PriceRange = original.PriceRange
            };

            if (_currentLanguage == "vi")
            {
                translated.Name = original.Name;
                translated.Description = original.Description;
            }
            else
            {
                try
                {
                    translated.Name = !string.IsNullOrEmpty(original.Name)
                            ? await TranslateText(original.Name, _currentLanguage)
                        : original.Name;
                    translated.Description = !string.IsNullOrEmpty(original.Description)
                                   ? await TranslateText(original.Description, _currentLanguage)
                       : original.Description;
                }
                catch
                {
                    translated.Name = original.Name;
                    translated.Description = original.Description;
                }
            }
            newPoints.Add(translated);
        }

        _allPoints = newPoints;
        UpdatePinsOnMap();
    }

    private async Task<string> TranslateText(string text, string targetLanguage)
    {
        if (string.IsNullOrEmpty(text) || targetLanguage == "vi") return text;

        if (_translationCache.ContainsKey(text) && _translationCache[text].ContainsKey(targetLanguage))
            return _translationCache[text][targetLanguage];

        if (_translationService == null) return text;

        try
        {
            var translated = await _translationService.TranslateTextAsync(text, targetLanguage);
            if (!_translationCache.ContainsKey(text))
                _translationCache[text] = new Dictionary<string, string>();
            _translationCache[text][targetLanguage] = translated;
            return translated;
        }
        catch
        {
            return text;
        }
    }

    private void UpdatePinsOnMap()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            map.Pins.Clear();
            foreach (var point in _allPoints)
            {
                var pin = new Pin
                {
                    Label = point.Name,
                    Address = point.Description ?? (_currentLanguage == "vi" ? "Địa điểm thuyết minh" : "Commentary point"),
                    Location = new Location(point.Latitude, point.Longitude),
                    Type = PinType.Place
                };
                pin.MarkerClicked += (s, e) => OnPinClicked(point);
                map.Pins.Add(pin);
            }
        });
    }

    private async void OnPinClicked(LocationPoint point)
    {
        _currentSelectedPoi = point;
        var translatedName = point.Name;

        var listenText = _currentLanguage == "vi" ? "🔊 Nghe thuyết minh" : "🔊 Listen";
        var detailText = _currentLanguage == "vi" ? "📖 Xem chi tiết" : "📖 View details";
        var cancelText = _currentLanguage == "vi" ? "Hủy" : "Cancel";

        string action = await DisplayActionSheet($"📍 {translatedName}", cancelText, null, listenText, detailText);

        if (action == listenText)
        {
            var textToSpeak = $"{translatedName}. {point.Description ?? ""}";
            
            // Đưa POI vào hàng đợi cooldown 2 phút
            _cooldownQueue[point.PointId] = DateTime.Now.AddMinutes(2);
            
            DateTime playStartTime = DateTime.Now;

            await TextToSpeech.Default.SpeakAsync(textToSpeak);
            
            var duration = (int)(DateTime.Now - playStartTime).TotalSeconds;
            string deviceId = Microsoft.Maui.Devices.DeviceInfo.Current.Name ?? "Unknown";
            int languageId = GetLanguageId(_currentLanguage);

            var apiService = ServiceHelper.GetService<ApiService>();
            if (apiService != null)
            {
                await apiService.RecordTTSListenAsync(point.PointId, languageId, duration, deviceId);
            }

            // Cập nhật lại UI để chuyển sang POI tiếp theo
            CheckNearbyPoints();
        }
        else if (action == detailText)
        {
            await Navigation.PushAsync(new PoiDetailPage { PoiId = point.PointId });
        }
    }

    private void CheckNearbyPoints()
    {
        // Dọn dẹp hàng đợi các POI đã hết thời gian cooldown (2 phút)
        var now = DateTime.Now;
        var expiredKeys = _cooldownQueue.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
        foreach (var key in expiredKeys) _cooldownQueue.Remove(key);

        if (_currentLocation == null || _allPoints == null) return;

        // 1. Lấy TẤT CẢ POI trong bán kính
        var allNearbyPoints = _allPoints
            .Select(p => new {
                Point = p,
                Distance = CalculateDistance(_currentLocation.Latitude, _currentLocation.Longitude, p.Latitude, p.Longitude)
            })
            .Where(x => x.Distance <= _currentRadius)
            .ToList();

        // 2. Lọc những POI CHƯA bị cooldown
        var activePoints = allNearbyPoints
            .Where(x => !_cooldownQueue.ContainsKey(x.Point.PointId))
            .ToList();

        // Log để debug
        Debug.WriteLine($"[MapPage] Found {allNearbyPoints.Count} total points, {activePoints.Count} active points within {_currentRadius}m");

        // 3. Quyết định chọn POI
        var nearest = activePoints
            .OrderBy(x => x.Distance)                   // Ưu tiên 1: Khoảng cách
            .ThenBy(x => x.Point.PointId)               // Ưu tiên 2: PointId nhỏ nhất
            .FirstOrDefault();

        // Nếu TẤT CẢ các điểm đều đang trong hàng đợi (cooldown) nhưng vẫn có POI quanh đây
        // => Bắt đầu vòng lặp lại bằng cách chọn POI có thời gian vào hàng đợi SỚM NHẤT (cổ nhất / sắp hết hạn nhất)
        if (nearest == null && allNearbyPoints.Any())
        {
            nearest = allNearbyPoints
                .OrderBy(x => _cooldownQueue[x.Point.PointId]) // Lấy POI có thời gian hết hạn nhỏ nhất
                .FirstOrDefault();

            Debug.WriteLine($"[MapPage] All points in cooldown. Looping back to oldest POI: {nearest?.Point.Name}");
        }

        // FIX: Define nearbyPoints as allNearbyPoints.Select(x => x.Point).ToList()
        var nearbyPoints = allNearbyPoints.Select(x => x.Point).ToList();

        if (nearest != null)
        {
            _currentSelectedPoi = nearest.Point;
            var translatedName = nearest.Point.Name;
            var translatedDescription = nearest.Point.Description ?? "";

            var nearbyCount = nearbyPoints.Count;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PoiNameLabel.Text = translatedName;
                ListenTextLabel.Text = string.IsNullOrEmpty(translatedDescription)
                    ? AppResources.GetString("TapToListen2")
                    : translatedDescription;

                var distanceText = string.Format(AppResources.GetString("NearbyDistance"), (int)nearest.Distance);
                NearbyLabel.Text = distanceText;
                RadiusDisplayLabel.Text = $"📍 {translatedName} - {(int)nearest.Distance}m";

                if (nearbyCount > 1)
                {
                    var queueText = _currentLanguage == "vi"
                        ? $"📋 Còn {nearbyCount - 1} điểm khác trong khu vực"
                        : $"📋 {nearbyCount - 1} other points in this area";

                    ListenTextLabel.Text = $"{ListenTextLabel.Text}\n\n{queueText}";
                }

                // Log POI được chọn
                Debug.WriteLine($"[MapPage] ✅ Selected POI: {nearest.Point.Name} (Rating: {nearest.Point.Rating})");
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NearbyLabel.Text = AppResources.GetString("NearbyDistance");
                PoiNameLabel.Text = AppResources.GetString("SelectPoi");
                ListenTextLabel.Text = AppResources.GetString("TapToListen2");
            });
        }
    }

    private void RefreshUITexts()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            //if (SearchEntry != null)
            //    SearchEntry.Placeholder = AppResources.GetString("PoiSearchPlaceholder");

            //if (LanguageLabel != null)
            //    LanguageLabel.Text = _currentLanguage.ToUpper();

            var prefix = _currentLanguage == "vi" ? "Bán kính kích hoạt" : "Activation radius";
            if (RadiusDisplayLabel != null)
                RadiusDisplayLabel.Text = $"{prefix}: {_currentRadius}m";

            if (PlayButton != null)
                PlayButton.Text = AppResources.GetString("PlayCommentary");

            if (_currentSelectedPoi != null)
                UpdateSelectedPoiTexts(_currentSelectedPoi);
            else
            {
                if (NearbyLabel != null)
                    NearbyLabel.Text = AppResources.GetString("NearbyDistance");
                if (PoiNameLabel != null)
                    PoiNameLabel.Text = AppResources.GetString("SelectPoi");
                if (ListenTextLabel != null)
                    ListenTextLabel.Text = AppResources.GetString("TapToListen2");
            }
        });
    }

    private void UpdateSelectedPoiTexts(LocationPoint point)
    {
        var translatedName = point.Name;
        var translatedDescription = point.Description ?? "";
        var distance = CalculateDistance(_currentLocation.Latitude, _currentLocation.Longitude, point.Latitude, point.Longitude);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (PoiNameLabel != null) PoiNameLabel.Text = translatedName;
            if (ListenTextLabel != null)
                ListenTextLabel.Text = string.IsNullOrEmpty(translatedDescription)
                    ? AppResources.GetString("TapToListen2")
                    : translatedDescription;

            if (NearbyLabel != null)
            {
                var distanceText = string.Format(AppResources.GetString("NearbyDistance"), (int)distance);
                NearbyLabel.Text = distanceText;
            }
            if (RadiusDisplayLabel != null)
                RadiusDisplayLabel.Text = $"📍 {translatedName} - {(int)distance}m";
        });
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async void OnGpsButtonTapped(object sender, EventArgs e)
    {
        if (_currentLocation != null)
        {
            var mapSpan = MapSpan.FromCenterAndRadius(new Location(_currentLocation.Latitude, _currentLocation.Longitude), Distance.FromMeters(500));
            map.MoveToRegion(mapSpan);
        }
        else
        {
            await DisplayAlert("Thông báo", "Đang lấy vị trí...", "OK");
            InitializeMap();
        }
    }

    private async void OnPlayButtonClicked(object sender, EventArgs e)
    {
        if (_currentSelectedPoi != null)
        {
            var translatedName = _currentSelectedPoi.Name;
            var translatedDescription = _currentSelectedPoi.Description ?? "";
            
            // Đưa POI vào hàng đợi cooldown 2 phút
            _cooldownQueue[_currentSelectedPoi.PointId] = DateTime.Now.AddMinutes(2);
            
            DateTime playStartTime = DateTime.Now;

            await TextToSpeech.Default.SpeakAsync($"{translatedName}. {translatedDescription}");
            
            var duration = (int)(DateTime.Now - playStartTime).TotalSeconds;
            string deviceId = Microsoft.Maui.Devices.DeviceInfo.Current.Name ?? "Unknown";
            int languageId = GetLanguageId(_currentLanguage);

            var apiService = ServiceHelper.GetService<ApiService>();
            if (apiService != null)
            {
                await apiService.RecordTTSListenAsync(_currentSelectedPoi.PointId, languageId, duration, deviceId);
            }

            // Cập nhật lại UI để nhảy sang POI tiếp theo ngay lập tức
            CheckNearbyPoints();
        }
    }

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var currentLanguage = Preferences.Get("AppLanguage", "vi");
        if (currentLanguage != _lastLanguage && !_isTranslating)
        {
            _currentLanguage = currentLanguage;
            await TranslateAndDisplayPoints();
            RefreshUITexts();
        }

        var newRadius = Preferences.Get("GeoFenceRadiusValue", 15.0);
        if (Math.Abs(newRadius - _currentRadius) > 0.01)
        {
            _currentRadius = newRadius;
            UpdateRadiusDisplay();

            // ✅ THÊM DÒNG NÀY - VẼ LẠI VÒNG TRÒN KHI ĐỔI BÁN KÍNH
            if (_currentLocation != null)
            {
                DrawRadiusCircle(new Location(_currentLocation.Latitude, _currentLocation.Longitude));
            }

            CheckNearbyPoints();
        }

        if (_allPoints == null || !_allPoints.Any())
            await LoadPoints();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gpsTokenSource?.Cancel();
    }
    // ========== VẼ VÒNG TRÒN BÁN KÍNH TRÊN BẢN ĐỒ ==========
    /// <summary>
    /// Vẽ vòng tròn bán kính xung quanh vị trí hiện tại
    /// </summary>
    /// <param name="center">Tâm vòng tròn (vị trí GPS hiện tại)</param>
    private void DrawRadiusCircle(Location center)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Xóa circle cũ nếu có
            if (_radiusCircle != null)
            {
                map.MapElements.Remove(_radiusCircle);
            }

            // Tạo circle mới với bán kính hiện tại
            _radiusCircle = new Circle
            {
                Center = center,
                Radius = Distance.FromMeters(_currentRadius),
                StrokeColor = Color.FromArgb("#2196F3"),
                StrokeWidth = 3,
                FillColor = Color.FromArgb("#402196F3")
            };

            map.MapElements.Add(_radiusCircle);
            Debug.WriteLine($"[MapPage] 🟠 Drew radius circle: {_currentRadius}m");
        });
    }
}