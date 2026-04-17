using SQLite;
using doanC_.Models;
using doanC_.Services.Api;
using System.Diagnostics;

namespace doanC_.Services.Offline
{
    public class OfflinePoiService
    {
        private SQLiteAsyncConnection _database;
        private readonly ApiService _apiService;
        private bool _isInitialized = false;

        public OfflinePoiService(ApiService apiService)
        {
            _apiService = apiService;
        }

        private async Task InitDatabaseAsync()
        {
            if (_isInitialized) return;

            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "offline_data.db");
                _database = new SQLiteAsyncConnection(dbPath);
                await _database.CreateTableAsync<LocationPoint>();
                await _database.CreateTableAsync<ScanHistory>();
                await _database.CreateTableAsync<TranslatedText>();
                _isInitialized = true;
                Debug.WriteLine("[OfflinePoi] ✅ Database initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OfflinePoi] ❌ Init error: {ex.Message}");
                throw;
            }
        }

        // Lấy POI (offline-first với cache strategy)
        public async Task<(List<LocationPoint> Data, bool IsFromCache, string Message)> GetLocationPointsAsync(bool forceRefresh = false)
        {
            await InitDatabaseAsync();

            // 1. Nếu không force refresh, thử lấy từ API
            if (!forceRefresh)
            {
                try
                {
                    Debug.WriteLine("[OfflinePoi] 📡 Trying API...");
                    var onlineData = await _apiService.GetLocationPointsAsync();

                    if (onlineData != null && onlineData.Any())
                    {
                        // Có Internet + API thành công -> Cập nhật cache
                        await SaveLocationPointsToLocal(onlineData);
                        Debug.WriteLine($"[OfflinePoi] ✅ Got {onlineData.Count} POIs from API");
                        return (onlineData, false, "✅ Dữ liệu mới từ server");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OfflinePoi] ⚠️ API failed: {ex.Message}");
                }
            }

            // 2. API lỗi hoặc force refresh -> Đọc từ cache
            Debug.WriteLine("[OfflinePoi] 📂 Loading from cache...");
            var cachedData = await _database.Table<LocationPoint>().ToListAsync();

            if (cachedData != null && cachedData.Any())
            {
                Debug.WriteLine($"[OfflinePoi] ✅ Loaded {cachedData.Count} POIs from cache");
                var message = forceRefresh
                    ? "📂 Đang ở chế độ offline - Dữ liệu từ cache"
                    : "📂 Chế độ offline - Hiển thị dữ liệu đã lưu";
                return (cachedData, true, message);
            }

            // 3. Không có dữ liệu
            Debug.WriteLine("[OfflinePoi] ❌ No data available");
            return (null, false, "❌ Không có dữ liệu. Vui lòng kết nối Internet để tải lần đầu.");
        }

        private async Task SaveLocationPointsToLocal(List<LocationPoint> points)
        {
            try
            {
                await _database.DeleteAllAsync<LocationPoint>();
                await _database.InsertAllAsync(points);
                Debug.WriteLine($"[OfflinePoi] 💾 Saved {points.Count} POIs to cache");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OfflinePoi] ❌ Save error: {ex.Message}");
            }
        }

        // Kiểm tra cache có dữ liệu không
        public async Task<bool> HasCacheAsync()
        {
            await InitDatabaseAsync();
            var count = await _database.Table<LocationPoint>().CountAsync();
            return count > 0;
        }

        // Xóa cache
        public async Task ClearCacheAsync()
        {
            await InitDatabaseAsync();
            await _database.DeleteAllAsync<LocationPoint>();
            Debug.WriteLine("[OfflinePoi] 🗑️ Cache cleared");
        }

        // Thêm scan history (offline)
        public async Task AddScanHistory(int poiId, string deviceId)
        {
            await InitDatabaseAsync();
            var history = new ScanHistory
            {
                PointId = poiId,
                DeviceId = deviceId,
                ScanTime = DateTime.Now
            };
            await _database.InsertAsync(history);
            Debug.WriteLine($"[OfflinePoi] 📝 Scan history saved for POI {poiId}");
        }

        // Lấy scan history
        public async Task<List<ScanHistory>> GetScanHistoryAsync(int limit = 20)
        {
            await InitDatabaseAsync();
            return await _database.Table<ScanHistory>()
                .OrderByDescending(h => h.ScanTime)
                .Take(limit)
                .ToListAsync();
        }

        // Lấy thống kê cache
        public async Task<int> GetCacheCountAsync()
        {
            await InitDatabaseAsync();
            return await _database.Table<LocationPoint>().CountAsync();
        }
    }

    [Table("ScanHistory")]
    public class ScanHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int PointId { get; set; }
        public string DeviceId { get; set; }
        public DateTime ScanTime { get; set; }
    }

    [Table("TranslatedText")]
    public class TranslatedText
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int PointId { get; set; }
        public string Language { get; set; }
        public string Text { get; set; }
        public DateTime CachedAt { get; set; }
    }
}