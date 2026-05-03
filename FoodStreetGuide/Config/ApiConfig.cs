using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace doanC_.Config
{
    public static class ApiConfig
    {
        // ========== CẤU HÌNH CHUNG ==========
        private const int Port = 5225;
        private const string ApiPath = "/api/LocationApi";
        // ========== IP THỰC CỦA MÁY TÍNH ==========
        private const string LocalIP = "172.20.10.11";

        // ========== NGROK URL (CẬP NHẬT MỖI KHI CHẠY) ==========
        private const string NgrokUrl = "https://tapeless-nondivergently-eleni.ngrok-free.dev/api/LocationApi";

        // ========== RENDER URL (THÊM VÀO) ==========
        private const string RenderUrl = "https://foodstreet-api-nrym.onrender.com//api/LocationApi";

        // ========== CÁC URL TƯƠNG ỨNG ==========
        private static readonly Dictionary<string, string> Urls = new()
        {
            { "Localhost", $"http://localhost:{Port}{ApiPath}" },
            { "Emulator", $"http://10.0.2.2:{Port}{ApiPath}" },
            { "LocalhostReverse", $"http://localhost:{Port}{ApiPath}" },
            { "Lan", $"http://{LocalIP}:{Port}{ApiPath}" },
            { "Ngrok", NgrokUrl },
            { "Render", RenderUrl }  // ✅ THÊM RENDER
        };

        // ========== CHỌN CHẾ ĐỘ KẾT NỐI ==========
        // "Localhost" - Chạy trên Windows (cùng máy với doanC_Admin)
        // "Emulator" - Android Emulator (dùng 10.0.2.2)
        // "Lan" - Dùng IP thực trong mạng LAN (chung WiFi)
        // "LocalhostReverse" - Sau khi chạy adb reverse tcp:5225 tcp:5225
        // "Ngrok" - Dùng ngrok (demo từ xa)
        // "Render" - Dùng Render.com (deploy cloud)
        private const string CurrentMode = "Lan";

        public static string GetBaseUrl()
        {
            if (Urls.TryGetValue(CurrentMode, out var url))
            {
                Debug.WriteLine($"[ApiConfig] ✅ Mode: {CurrentMode}");
                Debug.WriteLine($"[ApiConfig] 🔗 URL: {url}");
                return url;
            }

            // Fallback an toàn
            Debug.WriteLine($"[ApiConfig] ⚠️ Mode {CurrentMode} not found, using Localhost");
            return Urls["Localhost"];
        }

        /// <summary>
        /// Lấy IP động của máy (dùng khi IP thay đổi)
        /// </summary>
        public static string GetDynamicIpUrl()
        {
            var dynamicIp = GetLocalIPAddress();
            return $"http://{dynamicIp}:{Port}{ApiPath}";
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);

                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        Debug.WriteLine($"[ApiConfig] 📡 Found IP: {ip}");
                        return ip.ToString();
                    }
                }
                return LocalIP;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiConfig] ❌ Get IP error: {ex.Message}");
                return LocalIP;
            }
        }

        /// <summary>
        /// Kiểm tra kết nối API (bất đồng bộ)
        /// </summary>
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync(GetBaseUrl());
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiConfig] ❌ Connection test failed: {ex.Message}");
                return false;
            }
        }

        public static void TestConnection()
        {
            Debug.WriteLine($"[ApiConfig] ========== CONFIG INFO ==========");
            Debug.WriteLine($"[ApiConfig] 📱 Mode: {CurrentMode}");
            Debug.WriteLine($"[ApiConfig] 🔗 URL: {GetBaseUrl()}");
            Debug.WriteLine($"[ApiConfig] 🔌 Port: {Port}");
            Debug.WriteLine($"[ApiConfig] 📁 Path: {ApiPath}");
            Debug.WriteLine($"[ApiConfig] 💻 Local IP: {LocalIP}");
            Debug.WriteLine($"[ApiConfig] 🌐 Dynamic IP: {GetLocalIPAddress()}");
            Debug.WriteLine($"[ApiConfig] ==================================");
        }

        public static string GetCurrentModeInfo()
        {
            return $"""
                    ==================================
                    📱 FoodStreetGuide API Configuration
                    ==================================
                    Mode        : {CurrentMode}
                    URL         : {GetBaseUrl()}
                    Port        : {Port}
                    Path        : {ApiPath}
                    Local IP    : {LocalIP}
                    Dynamic IP  : {GetLocalIPAddress()}
                    ==================================
                    """;
        }
    }
}