using doanC_.Config;
using doanC_.Models;
using doanC_.Services.Api;
using doanC_.Services.Data;
using System.Diagnostics;
using System.Text;

namespace doanC_.Views
{
    public partial class ApiDebugPage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly SQLiteService _sqliteService;

        public ApiDebugPage(ApiService apiService, SQLiteService sqliteService)
        {
            InitializeComponent();
            _apiService = apiService;
            _sqliteService = sqliteService;

            LoadConfigInfo();
        }

        private void LoadConfigInfo()
        {
            var info = ApiConfig.GetCurrentModeInfo();
            ConfigInfoLabel.Text = info;
            Debug.WriteLine($"[ApiDebug] Config: {info}");
        }

        private async void OnTestApiClicked(object sender, EventArgs e)
        {
            try
            {
                TestApiBtn.IsEnabled = false;
                TestApiBtn.Text = "Đang gọi...";
                StatusLabel.Text = "⏳ Đang gọi API...";
                StatusLabel.TextColor = Colors.Orange;

                var sb = new StringBuilder();
                sb.AppendLine("=== API DEBUG INFO ===\n");
                sb.AppendLine($"📡 URL: {ApiConfig.GetBaseUrl()}\n");
                sb.AppendLine($"🕐 Thời gian: {DateTime.Now:HH:mm:ss}\n");
                sb.AppendLine("--- Đang gọi API ---\n");

                ResultLabel.Text = sb.ToString();

                var locations = await _apiService.GetLocationPointsAsync();

                if (locations != null && locations.Any())
                {
                    sb.AppendLine($"✅ THÀNH CÔNG!\n");
                    sb.AppendLine($"📊 Nhận được {locations.Count} địa điểm từ SQL Server\n");
                    sb.AppendLine("=== DANH SÁCH ĐỊA ĐIỂM ===\n");

                    for (int i = 0; i < locations.Count; i++)
                    {
                        var loc = locations[i];
                        sb.AppendLine($"{i + 1}. 📍 {loc.Name}");
                        sb.AppendLine($"   ID: {loc.PointId}");
                        sb.AppendLine($"   Địa chỉ: {loc.Address ?? "Chưa cập nhật"}");
                        sb.AppendLine($"   Category: {loc.Category ?? "Chưa phân loại"}");
                        sb.AppendLine($"   Rating: {loc.Rating} ⭐");
                        sb.AppendLine($"   Tọa độ: ({loc.Latitude}, {loc.Longitude})");
                        sb.AppendLine("");
                    }

                    StatusLabel.Text = $"✅ Thành công - {locations.Count} địa điểm";
                    StatusLabel.TextColor = Colors.Green;

                    await SaveToSqlite(locations);
                    sb.AppendLine("\n--- 💾 Đã lưu vào SQLite cache ---");
                }
                else
                {
                    sb.AppendLine("❌ API trả về dữ liệu RỖNG hoặc NULL\n");
                    sb.AppendLine("Nguyên nhân có thể:");
                    sb.AppendLine("1. Không có dữ liệu trong bảng LocationPoints");
                    sb.AppendLine("2. API endpoint sai");

                    StatusLabel.Text = "⚠️ API trả về dữ liệu rỗng";
                    StatusLabel.TextColor = Colors.Orange;
                }

                ResultLabel.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("❌ LỖI KHI GỌI API\n");
                sb.AppendLine($"Lỗi: {ex.Message}\n");
                sb.AppendLine($"Chi tiết: {ex}");

                ResultLabel.Text = sb.ToString();
                StatusLabel.Text = $"❌ Lỗi: {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                TestApiBtn.IsEnabled = true;
                TestApiBtn.Text = "📡 Gọi API";
            }
        }

        private async Task SaveToSqlite(List<LocationPoint> locations)
        {
            try
            {
                var existing = await _sqliteService.GetAllLocationPointsAsync();
                foreach (var loc in existing)
                {
                    await _sqliteService.DeleteLocationPointAsync(loc.PointId);
                }

                foreach (var loc in locations)
                {
                    await _sqliteService.AddLocationPointAsync(loc);
                }

                Debug.WriteLine($"[ApiDebug] 💾 Saved {locations.Count} locations to SQLite");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiDebug] ❌ Save error: {ex.Message}");
            }
        }

        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            try
            {
                TestConnectionBtn.IsEnabled = false;
                TestConnectionBtn.Text = "Đang test...";

                var isConnected = await _apiService.TestConnectionAsync();
                var url = ApiConfig.GetBaseUrl();

                if (isConnected)
                {
                    StatusLabel.Text = $"✅ Kết nối thành công đến {url}";
                    StatusLabel.TextColor = Colors.Green;
                    await DisplayAlert("Thành công", $"API tại {url} đang hoạt động!", "OK");
                }
                else
                {
                    StatusLabel.Text = $"❌ Không thể kết nối đến {url}";
                    StatusLabel.TextColor = Colors.Red;
                    await DisplayAlert("Thất bại", $"Không thể kết nối đến API tại:\n{url}", "OK");
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Lỗi: {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                TestConnectionBtn.IsEnabled = true;
                TestConnectionBtn.Text = "🔌 Test Connection";
            }
        }

        private async void OnClearCacheClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Xác nhận", "Xóa toàn bộ dữ liệu cache?", "Đồng ý", "Hủy");

            if (confirm)
            {
                var existing = await _sqliteService.GetAllLocationPointsAsync();
                foreach (var loc in existing)
                {
                    await _sqliteService.DeleteLocationPointAsync(loc.PointId);
                }

                StatusLabel.Text = "🗑️ Đã xóa cache";
                StatusLabel.TextColor = Colors.Orange;
                await DisplayAlert("Thành công", $"Đã xóa {existing.Count} địa điểm", "OK");
            }
        }
    }
}