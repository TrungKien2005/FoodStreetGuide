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
    }
}