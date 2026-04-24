using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using doanC_.Config;
using doanC_.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace doanC_.Services.LocationTracking
{
    /// <summary>
    /// Service theo dõi thiết bị MAUI và gửi thông tin lên Server Admin
    /// </summary>
    public class DeviceTrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private string _deviceUniqueId;
        private Timer _heartbeatTimer;

        // ✅ CHUYÊN NGHIỆP: 5s là chuẩn realtime, có thể config
        private const int HEARTBEAT_INTERVAL_SECONDS = 5;  // 5 giây cho đồ án chuyên nghiệp
        private const int BACKGROUND_INTERVAL_SECONDS = 30;
        private bool _isInBackground = false;
        // ✅ Thêm: Retry policy
        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 1000;

        public DeviceTrackingService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Timeout 5s cho heartbeat
            _apiBaseUrl = ApiConfig.GetBaseUrl().Replace("/api/LocationApi", "/api/DeviceTracking");
        }

        /// <summary>
        /// Khởi tạo tracking và bắt đầu gửi heartbeat
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("[DeviceTracking] 🚀 Initializing device tracking...");
                Debug.WriteLine($"[DeviceTracking] 📡 API URL: {_apiBaseUrl}");
                Debug.WriteLine($"[DeviceTracking] ⏱️ Heartbeat interval: {HEARTBEAT_INTERVAL_SECONDS}s");

                // Lấy hoặc tạo Device ID duy nhất
                _deviceUniqueId = await GetOrCreateDeviceIdAsync();

                // Gửi thông tin thiết bị lần đầu
                await TrackDeviceAsync();

                // Bắt đầu heartbeat timer
                StartHeartbeat();

                Debug.WriteLine($"[DeviceTracking] ✅ Initialized with ID: {_deviceUniqueId.Substring(0, 8)}...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ Init error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gửi thông tin thiết bị lên server
        /// </summary>
        public async Task TrackDeviceAsync(double? latitude = null, double? longitude = null)
        {
            await RetryPolicy(async () =>
            {
                var deviceInfo = await GetDeviceInfoAsync(latitude, longitude);
                var json = JsonSerializer.Serialize(deviceInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/Track", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[DeviceTracking] ✅ Heartbeat sent successfully");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[DeviceTracking] ⚠️ Heartbeat failed: {response.StatusCode}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Ghi nhận hoạt động của người dùng
        /// </summary>
        public async Task TrackActivityAsync(string activityType, int pointId = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(_deviceUniqueId))
                {
                    _deviceUniqueId = await GetOrCreateDeviceIdAsync();
                }

                var activityData = new
                {
                    deviceUniqueId = _deviceUniqueId,
                    activityType = activityType,
                    pointId = pointId,
                    timestamp = DateTime.Now
                };

                var json = JsonSerializer.Serialize(activityData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await _httpClient.PostAsync($"{_apiBaseUrl}/TrackActivity", content);
                Debug.WriteLine($"[DeviceTracking] 📊 Activity tracked: {activityType}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ Activity error: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ CHUYÊN NGHIỆP: Gửi heartbeat riêng biệt (tối ưu hơn Track)
        /// </summary>
        private async Task SendHeartbeatAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_deviceUniqueId))
                {
                    _deviceUniqueId = await GetOrCreateDeviceIdAsync();
                }

                var heartbeatData = new
                {
                    deviceUniqueId = _deviceUniqueId,
                    timestamp = DateTime.Now
                };

                var json = JsonSerializer.Serialize(heartbeatData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Heartbeat timeout 3s
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/UpdateHeartbeat", content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[DeviceTracking] 💓 Heartbeat sent at {DateTime.Now:HH:mm:ss}");
                }
                else
                {
                    Debug.WriteLine($"[DeviceTracking] ⚠️ Heartbeat failed: {response.StatusCode}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[DeviceTracking] ⏱️ Heartbeat timeout");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ Heartbeat error: {ex.Message}");
            }
        }

        /// <summary>
        /// Bắt đầu heartbeat timer
        /// </summary>
        private void StartHeartbeat()
        {
            var interval = _isInBackground ? BACKGROUND_INTERVAL_SECONDS : HEARTBEAT_INTERVAL_SECONDS;

            _heartbeatTimer = new Timer(async _ =>
            {
                await SendHeartbeatAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(interval));

            Debug.WriteLine($"[DeviceTracking] 💓 Heartbeat started: {interval}s (Foreground mode: {!_isInBackground})");
        }

        /// <summary>
        /// Dừng heartbeat
        /// </summary>
        public void StopHeartbeat()
        {
            try
            {
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;
                Debug.WriteLine("[DeviceTracking] ⏹️ Heartbeat stopped");

                // Notify server offline
                Task.Run(async () => await SetOfflineAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ StopHeartbeat error: {ex.Message}");
            }
        }
        /// <summary>
        /// Khởi động lại heartbeat với interval mới
        /// </summary>
        private void RestartHeartbeat(int intervalSeconds)
        {
            StopHeartbeat();

            // Tạo timer mới với interval mới
            _heartbeatTimer = new Timer(async _ =>
            {
                await SendHeartbeatAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));

            Debug.WriteLine($"[DeviceTracking] 🔄 Heartbeat restarted: {intervalSeconds}s (Background mode: {_isInBackground})");
        }

        /// <summary>
        /// Gọi API để đánh dấu thiết bị offline
        /// </summary>
        public async Task SetOfflineAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_deviceUniqueId))
                {
                    _deviceUniqueId = await GetOrCreateDeviceIdAsync();
                }

                var payload = new { deviceUniqueId = _deviceUniqueId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/Untrack", content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[DeviceTracking] ✅ Server notified: device offline");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ SetOffline error: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Retry policy cho network operations
        /// </summary>
        private async Task<bool> RetryPolicy(Func<Task<bool>> action)
        {
            for (int attempt = 1; attempt <= MAX_RETRY_COUNT; attempt++)
            {
                try
                {
                    if (await action())
                        return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DeviceTracking] Attempt {attempt}/{MAX_RETRY_COUNT} failed: {ex.Message}");
                }

                if (attempt < MAX_RETRY_COUNT)
                {
                    await Task.Delay(RETRY_DELAY_MS * attempt);
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy hoặc tạo Device ID duy nhất
        /// </summary>
        private async Task<string> GetOrCreateDeviceIdAsync()
        {
            try
            {
                var savedId = await SecureStorage.GetAsync("device_unique_id");
                if (!string.IsNullOrEmpty(savedId))
                {
                    return savedId;
                }

                var newId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("device_unique_id", newId);
                Debug.WriteLine($"[DeviceTracking] 🆕 Created new device ID: {newId.Substring(0, 8)}...");
                return newId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ GetOrCreateId error: {ex.Message}");
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Lấy thông tin thiết bị
        /// </summary>
        private async Task<DeviceInfoModel> GetDeviceInfoAsync(double? latitude = null, double? longitude = null)
        {
            return new DeviceInfoModel
            {
                DeviceUniqueId = await GetOrCreateDeviceIdAsync(),
                DeviceName = DeviceInfo.Name ?? "Unknown Device",
                Platform = DeviceInfo.Platform.ToString(),
                OsVersion = DeviceInfo.VersionString,
                AppVersion = AppInfo.Current.VersionString,
                LastLocationLat = latitude,
                LastLocationLng = longitude
            };
        }

        public async Task UntrackAsync()
        {
            await SetOfflineAsync();
        }

        /// <summary>
        /// Gọi khi app chuyển vào Background
        /// </summary>
        public void OnAppBackground()
        {
            if (_isInBackground) return;

            _isInBackground = true;
            Debug.WriteLine("[DeviceTracking] 📱 App went to BACKGROUND - Reducing heartbeat frequency");
            RestartHeartbeat(BACKGROUND_INTERVAL_SECONDS);
        }

        /// <summary>
        /// Gọi khi app chuyển lên Foreground
        /// </summary>
        public void OnAppForeground()
        {
            if (!_isInBackground) return;

            _isInBackground = false;
            Debug.WriteLine("[DeviceTracking] 📱 App returned to FOREGROUND - Increasing heartbeat frequency");
            RestartHeartbeat(HEARTBEAT_INTERVAL_SECONDS);
        }
    }

    public class DeviceInfoModel
    {
        public string DeviceUniqueId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public double? LastLocationLat { get; set; }
        public double? LastLocationLng { get; set; }
    }
}