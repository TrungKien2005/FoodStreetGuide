using doanC_.Models;
using doanC_.Services.Api;
using doanC_.Services.Data;
using System.Text;

namespace doanC_.Views
{
    public partial class SimpleTestPage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly SQLiteService _sqliteService;

        public SimpleTestPage(ApiService apiService, SQLiteService sqliteService)
        {
            InitializeComponent();
            _apiService = apiService;
            _sqliteService = sqliteService;
        }

        private async void OnLoadDataClicked(object sender, EventArgs e)
        {
            try
            {
                ResultLabel.Text = "Đang tải dữ liệu...";

                var locations = await _apiService.GetLocationPointsAsync();

                var sb = new StringBuilder();

                if (locations != null && locations.Any())
                {
                    sb.AppendLine($"✅ Thành công! {locations.Count} địa điểm:\n");
                    sb.AppendLine(new string('=', 50));

                    foreach (var loc in locations)
                    {
                        sb.AppendLine($"📍 {loc.Name}");
                        sb.AppendLine($"   ID: {loc.PointId}");
                        sb.AppendLine($"   Địa chỉ: {loc.Address ?? "N/A"}");
                        sb.AppendLine($"   Category: {loc.Category ?? "N/A"}");

                        // 👉 SỬA LỖI: Rating là float (không nullable), dùng trực tiếp
                        sb.AppendLine($"   Rating: {loc.Rating}⭐");
                        sb.AppendLine($"   Tọa độ: {loc.Latitude}, {loc.Longitude}");
                        sb.AppendLine();
                    }

                    // Lưu vào SQLite
                    await _sqliteService.SaveLocationPointsAsync(locations);
                    sb.AppendLine($"💾 Đã lưu {locations.Count} địa điểm vào SQLite");
                }
                else
                {
                    sb.AppendLine("❌ Không có dữ liệu từ API");
                    sb.AppendLine("\nKiểm tra:");
                    sb.AppendLine("1. doanC_Admin đang chạy?");
                    sb.AppendLine("2. Cổng 5225 đúng?");
                    sb.AppendLine("3. Database có dữ liệu?");
                }

                ResultLabel.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"❌ Lỗi: {ex.Message}\n\n{ex.StackTrace}";
            }
        }

        private async void OnClearCacheClicked(object sender, EventArgs e)
        {
            var existing = await _sqliteService.GetAllLocationPointsAsync();
            foreach (var loc in existing)
            {
                await _sqliteService.DeleteLocationPointAsync(loc.PointId);
            }
            ResultLabel.Text = $"🗑️ Đã xóa {existing.Count} địa điểm khỏi cache";
        }
    }
}