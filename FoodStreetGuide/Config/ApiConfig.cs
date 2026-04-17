using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace doanC_.Config
{
    public static class ApiConfig
    {
        // ========== CẤU HÌNH CHUNG ==========
        private const int Port = 5225;           // ✅ THÊM DÒNG NÀY
        private const string ApiPath = "/api/LocationApi";

        // ========== NGROK URL (CẬP NHẬT MỖI KHI CHẠY) ==========
        // Sau khi chạy `ngrok http 5225`, copy URL mới vào đây
        private const string NgrokUrl = "https://tapeless-nondivergently-eleni.ngrok-free.dev/api/LocationApi";

        // ========== URL CHO CÁC CHẾ ĐỘ ==========
        // 👉 IP THỰC CỦA MÁY TÍNH (từ ipconfig)
        private const string LocalIP = "192.168.13.238";

        // Các URL tương ứng
        private static readonly Dictionary<string, string> Urls = new()
        {
            { "Localhost", $"http://localhost:{Port}{ApiPath}" },
            { "Emulator", $"http://10.0.2.2:{Port}{ApiPath}" },
            { "LocalhostReverse", $"http://localhost:{Port}{ApiPath}" },
            { "Lan", $"http://{LocalIP}:{Port}{ApiPath}" },
            { "Ngrok", NgrokUrl }  // ✅ THÊM CHẾ ĐỘ NGROK
        };

        // 🔧 CHỌN CHẾ ĐỘ KẾT NỐI:
        // "Localhost" - Chạy trên Windows (cùng máy với doanC_Admin)
        // "Emulator" - Android Emulator (dùng 10.0.2.2)
        // "Lan" - Dùng IP thực trong mạng LAN
        // "LocalhostReverse" - Sau khi chạy adb reverse tcp:5225 tcp:5225
        // "Ngrok" - Dùng ngrok (demo từ xa)
        private const string CurrentMode = "Ngrok";  // 👈 CHỌN CHẾ ĐỘ NÀY KHI DEMO TỪ XA

        public static string GetBaseUrl()
        {
            if (Urls.TryGetValue(CurrentMode, out var url))
            {
                Debug.WriteLine($"[ApiConfig] 🌐 Mode: {CurrentMode}, URL: {url}");
                return url;
            }

            // Fallback
            Debug.WriteLine($"[ApiConfig] ⚠️ Mode {CurrentMode} not found, using Ngrok");
            return NgrokUrl;
        }

        public static string GetDynamicLanUrl()
        {
            return $"http://{LocalIP}:{Port}{ApiPath}";
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

        public static void TestConnection()
        {
            Debug.WriteLine($"[ApiConfig] ===== CONFIG INFO =====");
            Debug.WriteLine($"[ApiConfig] Mode: {CurrentMode}");
            Debug.WriteLine($"[ApiConfig] URL: {GetBaseUrl()}");
            Debug.WriteLine($"[ApiConfig] Port: {Port}");
            Debug.WriteLine($"[ApiConfig] Path: {ApiPath}");
            Debug.WriteLine($"[ApiConfig] Local IP: {LocalIP}");
        }

        public static string GetCurrentModeInfo()
        {
            return $"Mode: {CurrentMode}\nURL: {GetBaseUrl()}\nPort: {Port}\nIP: {LocalIP}";
        }
    }
}