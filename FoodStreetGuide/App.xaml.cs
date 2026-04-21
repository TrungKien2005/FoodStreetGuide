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

            _deviceTrackingService = ServiceHelper.GetService<DeviceTrackingService>();

            var appLanguage = Preferences.Get("AppLanguage", null);
            if (!string.IsNullOrEmpty(appLanguage))
            {
                AppResources.SetLanguage(appLanguage);
                Debug.WriteLine($"[App] 🌐 Loaded saved language: {appLanguage}");
            }

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
                var databaseService = ServiceHelper.GetService<SQLiteService>();
                if (databaseService != null)
                {
                    await databaseService.InitializeAsync();
                    Debug.WriteLine("[App] ✅ SQLite database initialized");
                }

                if (_deviceTrackingService != null)
                {
                    await _deviceTrackingService.InitializeAsync();
                    Debug.WriteLine("[App] ✅ DeviceTrackingService initialized");
                }

                var translationService = new HybridTranslationService();
                if (translationService != null)
                {
                    translationService.Initialize();
                    Debug.WriteLine("[App] ✅ HybridTranslationService initialized");
                }

                await LoadDataFromApi();

                if (databaseService != null)
                {
                    var points = await databaseService.GetAllLocationPointsAsync();
                    Debug.WriteLine($"[App] 📍 SQLite has {points?.Count ?? 0} location points");
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

        protected override void OnSleep()
        {
            base.OnSleep();

            try
            {
                // Try to notify server that app/device is offline immediately
                if (_deviceTrackingService != null)
                {
                    // run synchronously to ensure server receives the call before process suspends in many platforms
                    Task.Run(async () => await _deviceTrackingService.UntrackAsync()).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] ❌ Untrack error on sleep: {ex.Message}");
            }
            finally
            {
                _deviceTrackingService?.StopHeartbeat();
                Debug.WriteLine("[App] 💤 App sleeping, stopped heartbeat and requested untrack");
            }
        }

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

                var locations = await apiService.GetLocationPointsAsync();

                if (locations != null && locations.Any())
                {
                    Debug.WriteLine($"[App] ✅ API trả về {locations.Count} địa điểm từ SQL Server");

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