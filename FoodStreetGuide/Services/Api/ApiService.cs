using System.Text;
using System.Text.Json;
using System.Diagnostics;
using doanC_.Models;
using doanC_.Config;
using System.Net.Http.Headers;

namespace doanC_.Services.Api
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl;
        private readonly string _baseUrlRoot;
        public ApiService()
        {
#if DEBUG
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
#else
            _httpClient = new HttpClient();
#endif

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // 👉 CẤU HÌNH JSON DESERIALIZATION
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // Cho phép "pointId" khớp với "PointId"
                PropertyNamingPolicy = null,         // Giữ nguyên tên property
                WriteIndented = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            _baseUrl = ApiConfig.GetBaseUrl();
            _baseUrlRoot = _baseUrl.Replace("/api/LocationApi", "");    
            ApiConfig.TestConnection();
            Debug.WriteLine($"[ApiService] ✅ Initialized with URL: {_baseUrl}");
        }

        public async Task<List<LocationPoint>> GetLocationPointsAsync()
        {
            try
            {
                Debug.WriteLine($"[ApiService] ===== GET ALL LOCATIONS =====");
                Debug.WriteLine($"[ApiService] 📡 URL: {_baseUrl}");

                var response = await _httpClient.GetAsync(_baseUrl);

                Debug.WriteLine($"[ApiService] 📊 Status: {(int)response.StatusCode} - {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiService] 📄 JSON Length: {json.Length} chars");
                    Debug.WriteLine($"[ApiService] 📄 First 300 chars: {(json.Length > 300 ? json.Substring(0, 300) : json)}");

                    // Deserialize với options
                    var locations = JsonSerializer.Deserialize<List<LocationPoint>>(json, _jsonOptions);

                    if (locations != null && locations.Any())
                    {
                        Debug.WriteLine($"[ApiService] ✅ Success! Deserialized {locations.Count} locations");

                        // Log chi tiết từng location
                        foreach (var loc in locations)
                        {
                            Debug.WriteLine($"[ApiService] 📍 ID:{loc.PointId} | {loc.Name} | ({loc.Latitude}, {loc.Longitude})");
                        }

                        return locations;
                    }
                    else
                    {
                        Debug.WriteLine($"[ApiService] ⚠️ Deserialization returned null or empty");

                        // Thử debug bằng cách deserialize raw
                        try
                        {
                            var rawList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, _jsonOptions);
                            if (rawList != null && rawList.Any())
                            {
                                Debug.WriteLine($"[ApiService] 🔍 Raw data first item keys: {string.Join(", ", rawList.First().Keys)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ApiService] 🔍 Raw deserialize error: {ex.Message}");
                        }

                        return new List<LocationPoint>();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiService] ❌ HTTP Error: {response.StatusCode}");
                    Debug.WriteLine($"[ApiService] ❌ Error Body: {error}");
                    return new List<LocationPoint>();
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[ApiService] ❌ JSON Error: {ex.Message}");
                Debug.WriteLine($"[ApiService] Path: {ex.Path}, Line: {ex.LineNumber}");
                return new List<LocationPoint>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ Error: {ex.Message}");
                Debug.WriteLine($"[ApiService] Stack: {ex.StackTrace}");
                return new List<LocationPoint>();
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Debug.WriteLine($"[ApiService] 🔌 Testing connection to {_baseUrl}");
                var response = await _httpClient.GetAsync(_baseUrl);
                bool connected = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ApiService] 📡 Connection test: {(connected ? "OK" : "FAILED")}");
                return connected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ Connection test failed: {ex.Message}");
                return false;
            }
        }
        // Thêm vào cuối class ApiService

        /// <summary>
        /// Ghi nhận lượt quét QR từ MAUI App
        /// </summary>
        public async Task<bool> RecordQRScanAsync(int pointId, string deviceId, double? latitude = null, double? longitude = null)
        {
            try
            {
                var request = new
                {
                    pointId = pointId,
                    deviceId = deviceId,
                    latitude = latitude,
                    longitude = longitude
                };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ SỬA: Dùng _baseUrlRoot thay vì _baseUrl
                var url = $"{_baseUrlRoot}/api/RealTime/RecordQRScan";

                Debug.WriteLine($"[ApiService] 📤 URL: {url}");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ RecordQRScan error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ghi nhận lượt nghe TTS từ MAUI App
        /// </summary>
        public async Task<bool> RecordTTSListenAsync(int pointId, int languageId, int durationSeconds, string deviceId)
        {
            try
            {
                var request = new
                {
                    pointId = pointId,
                    languageId = languageId,
                    durationSeconds = durationSeconds,
                    deviceId = deviceId
                };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ SỬA: Dùng _baseUrlRoot thay vì _baseUrl
                var url = $"{_baseUrlRoot}/api/RealTime/RecordTTSListen";

                Debug.WriteLine($"[ApiService] 📤 URL: {url}");
                Debug.WriteLine($"[ApiService] 📤 JSON: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[ApiService] 📥 Response: {response.StatusCode} - {responseContent}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ RecordTTSListen error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy top địa điểm được nghe nhiều nhất
        /// </summary>
        public async Task<List<TopLocationDto>> GetTopListenedLocationsAsync(int take = 5)
        {
            try
            {
                Debug.WriteLine($"[ApiService] 📊 Getting top {take} listened locations");
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/RealTime/top-locations?metric=listens&take={take}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<TopLocationDto>>>(json, _jsonOptions);
                    Debug.WriteLine($"[ApiService] ✅ Got {result?.Data?.Count ?? 0} top listened locations");
                    return result?.Data ?? new List<TopLocationDto>();
                }

                Debug.WriteLine($"[ApiService] ❌ Failed to get top listened: {response.StatusCode}");
                return new List<TopLocationDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ GetTopListenedLocations error: {ex.Message}");
                return new List<TopLocationDto>();
            }
        }

        /// <summary>
        /// Lấy top địa điểm có thời gian nghe trung bình cao nhất
        /// </summary>
        public async Task<List<TopAvgTimeDto>> GetTopAvgTimeLocationsAsync(int take = 5)
        {
            try
            {
                Debug.WriteLine($"[ApiService] ⏱️ Getting top {take} avg time locations");
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/RealTime/top-locations?metric=avgtime&take={take}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<List<TopAvgTimeDto>>>(json, _jsonOptions);
                    Debug.WriteLine($"[ApiService] ✅ Got {result?.Data?.Count ?? 0} top avg time locations");
                    return result?.Data ?? new List<TopAvgTimeDto>();
                }

                Debug.WriteLine($"[ApiService] ❌ Failed to get top avg time: {response.StatusCode}");   
                return new List<TopAvgTimeDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiService] ❌ GetTopAvgTimeLocations error: {ex.Message}");
                return new List<TopAvgTimeDto>();
            }
        }
        // Thêm vào cuối file ApiService.cs (trong cùng namespace)
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
            public string? Metric { get; set; }
        }

        public class TopLocationDto
        {
            public int PointId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class TopAvgTimeDto
        {
            public int PointId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public double AvgTime { get; set; }
        }
    }
}