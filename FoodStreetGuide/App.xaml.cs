using doanC_.Helpers;
using doanC_.Models;
using doanC_.Services.Api;
using doanC_.Services.Data;
using doanC_.Services.Localization;
using doanC_.Services.LocationTracking;  // 👈 THÊM DÒNG NÀY
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace doanC_
{
    public partial class App : Application
    {
        private DeviceTrackingService _deviceTrackingService;  // 👈 THÊM DÒNG NÀY

        public App()
        {
            InitializeComponent();

            // 👈 KHỞI TẠO DEVICE TRACKING SERVICE
            _deviceTrackingService = ServiceHelper.GetService<DeviceTrackingService>();

            // Tải ngôn ngữ đã lưu
            var appLanguage = Preferences.Get("AppLanguage", null);
            if (!string.IsNullOrEmpty(appLanguage))
            {
                AppResources.SetLanguage(appLanguage);
                Debug.WriteLine($"[App] 🌐 Loaded saved language: {appLanguage}");
            }

            // Kiểm tra nếu đã chọn ngôn ngữ, hiển thị AppShell; nếu chưa, hiển thị LanguageSelectionPage
            if (string.IsNullOrEmpty(appLanguage))
            {
                MainPage = new NavigationPage(new Views.Language.LanguageSelectionPage());
            }
            else
            {
                MainPage = new AppShell();
            }
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                // ✅ KHỞI TẠO SQLITE
                var databaseService = ServiceHelper.GetService<SQLiteService>();
                if (databaseService != null)
                {
                    await databaseService.InitializeAsync();
                    Debug.WriteLine("[App] ✅ SQLite database initialized");
                }

                // ✅ KHỞI TẠO DEVICE TRACKING (THÊM VÀO ĐÂY)
                if (_deviceTrackingService != null)
                {
                    await _deviceTrackingService.InitializeAsync();
                    Debug.WriteLine("[App] ✅ DeviceTrackingService initialized");
                }

                // ✅ KHỞI TẠO TRANSLATION SERVICE
                var translationService = new HybridTranslationService();
                if (translationService != null)
                {
                    translationService.Initialize();
                    Debug.WriteLine("[App] ✅ HybridTranslationService initialized");
                }

                // ✅ GỌI API ĐỂ LẤY DỮ LIỆU TỪ SQL SERVER
                await LoadDataFromApi();

                // ✅ KIỂM TRA DỮ LIỆU TRONG SQLITE
                if (databaseService != null)
                {
                    var points = await databaseService.GetAllLocationPointsAsync();
                    Debug.WriteLine($"[App] 📍 SQLite has {points?.Count ?? 0} location points");

                    if (points != null && points.Any())
                    {
                        foreach (var p in points.Take(5))
                        {
                            Debug.WriteLine($"[App]    - {p.Name}: ({p.Latitude}, {p.Longitude})");
                        }
                        if (points.Count > 5)
                        {
                            Debug.WriteLine($"[App]    ... và {points.Count - 5} điểm khác");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[App] ⚠️ WARNING: No location points in SQLite! Geofence will not work.");
                    }
                }

                Debug.WriteLine("[App] ✅ App started successfully");
                Debug.WriteLine("[App] 💡 GPS and Geofence are ready");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] ❌ Error: {ex.Message}");
                Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");
            }
        }

        // 👈 THÊM METHOD NÀY ĐỂ XỬ LÝ KHI APP TẮT
        protected override void OnSleep()
        {
            base.OnSleep();
            _deviceTrackingService?.StopHeartbeat();
            Debug.WriteLine("[App] 💤 App sleeping, stopped heartbeat");
        }

        // 👈 THÊM METHOD NÀY ĐỂ XỬ LÝ KHI APP MỞ LẠI
        protected override void OnResume()
        {
            base.OnResume();
            Task.Run(async () =>
            {
                if (_deviceTrackingService != null)
                {
                    await _deviceTrackingService.InitializeAsync();
                    Debug.WriteLine("[App] 🔄 App resumed, restarted tracking");
                }
            });
        }

        /// <summary>
        /// Tải dữ liệu từ API và lưu vào SQLite
        /// </summary>
        private async Task LoadDataFromApi()
        {
            try
            {
                Debug.WriteLine("[App] ===== BẮT ĐẦU TẢI DỮ LIỆU TỪ API =====");

                var apiService = ServiceHelper.GetService<ApiService>();
                var databaseService = ServiceHelper.GetService<SQLiteService>();

                if (apiService == null)
                {
                    Debug.WriteLine("[App] ❌ ApiService not available");
                    return;
                }

                if (databaseService == null)
                {
                    Debug.WriteLine("[App] ❌ DatabaseService not available");
                    return;
                }

                // Gọi API lấy dữ liệu
                var locations = await apiService.GetLocationPointsAsync();

                if (locations != null && locations.Any())
                {
                    Debug.WriteLine($"[App] ✅ API trả về {locations.Count} địa điểm từ SQL Server");

                    // Lưu vào SQLite (xóa cũ, thêm mới)
                    var existingPoints = await databaseService.GetAllLocationPointsAsync();
                    foreach (var point in existingPoints)
                    {
                        await databaseService.DeleteLocationPointAsync(point.PointId);
                    }

                    foreach (var loc in locations)
                    {
                        await databaseService.AddLocationPointAsync(loc);
                    }

                    Debug.WriteLine($"[App] 💾 Đã lưu {locations.Count} địa điểm vào SQLite cache");
                }
                else
                {
                    Debug.WriteLine("[App] ⚠️ API không trả về dữ liệu, kiểm tra kết nối!");

                    // Kiểm tra kết nối API
                    var isConnected = await apiService.TestConnectionAsync();
                    Debug.WriteLine($"[App] 📡 API Connection test: {(isConnected ? "OK" : "FAILED")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] ❌ Lỗi khi tải dữ liệu từ API: {ex.Message}");
                Debug.WriteLine($"[App] 📂 App sẽ dùng dữ liệu cache nếu có");
            }
        }
    }
}