using doanC_.Models;
using doanC_.Services.Api;
using doanC_.Services.Data;
using System.Diagnostics;

namespace doanC_.Services.Offline
{
    public class OfflineSyncService
    {
        private readonly OfflinePoiService _offlinePoiService;

        public OfflineSyncService(OfflinePoiService offlinePoiService)
        {
            _offlinePoiService = offlinePoiService;
        }

        /// <summary>
        /// Lấy dữ liệu POI - Wrapper cho OfflinePoiService
        /// </summary>
        public async Task<List<LocationPoint>> GetLocationPointsOfflineFirstAsync()
        {
            Debug.WriteLine("[OfflineSync] ===== BẮT ĐẦU LẤY DỮ LIỆU =====");

            var (data, isFromCache, message) = await _offlinePoiService.GetLocationPointsAsync();

            if (data != null && data.Any())
            {
                Debug.WriteLine($"[OfflineSync] ✅ {message} - {data.Count} POIs");
                if (isFromCache)
                {
                    // Có thể hiển thị thông báo cho user
                    Debug.WriteLine($"[OfflineSync] 📌 {message}");
                }
                return data;
            }

            Debug.WriteLine("[OfflineSync] ❌ No data available");
            return new List<LocationPoint>();
        }

        // Kiểm tra cache
        public async Task<bool> HasCacheAsync()
        {
            return await _offlinePoiService.HasCacheAsync();
        }

        // Xóa cache
        public async Task ClearCacheAsync()
        {
            await _offlinePoiService.ClearCacheAsync();
        }

        // Thống kê
        public async Task<int> GetCacheCountAsync()
        {
            return await _offlinePoiService.GetCacheCountAsync();
        }
    }
}