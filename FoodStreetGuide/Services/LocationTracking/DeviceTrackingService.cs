using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private const int HEARTBEAT_INTERVAL_SECONDS = 30;

        public DeviceTrackingService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
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
            try
            {
                var deviceInfo = await GetDeviceInfoAsync(latitude, longitude);
                var json = JsonSerializer.Serialize(deviceInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/Track", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[DeviceTracking] ✅ Device tracked successfully");
                }
                else
                {
                    Debug.WriteLine($"[DeviceTracking] ⚠️ Track failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ Track error: {ex.Message}");
            }
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
        /// Gửi heartbeat để giữ session
        /// </summary>
        private async void SendHeartbeat(object state)
        {
            await TrackDeviceAsync();
        }

        /// <summary>
        /// Bắt đầu heartbeat timer
        /// </summary>
        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(SendHeartbeat, null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(HEARTBEAT_INTERVAL_SECONDS));
        }

        /// <summary>
        /// Dừng heartbeat
        /// </summary>
        public void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        /// <summary>
        /// Lấy hoặc tạo Device ID duy nhất
        /// </summary>
        private async Task<string> GetOrCreateDeviceIdAsync()
        {
            try
            {
                // Thử lấy từ SecureStorage
                var savedId = await SecureStorage.GetAsync("device_unique_id");

                if (!string.IsNullOrEmpty(savedId))
                {
                    return savedId;
                }

                // Tạo mới nếu chưa có
                var newId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("device_unique_id", newId);
                Debug.WriteLine($"[DeviceTracking] 🆕 Created new device ID: {newId.Substring(0, 8)}...");

                return newId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceTracking] ❌ GetOrCreateId error: {ex.Message}");
                // Fallback: tạo ID tạm thời
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
    }

    /// <summary>
    /// Model thông tin thiết bị gửi lên server
    /// </summary>
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