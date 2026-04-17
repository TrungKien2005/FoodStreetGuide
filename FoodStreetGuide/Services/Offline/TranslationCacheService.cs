using System.Diagnostics;
using SQLite;
using doanC_.Models;

namespace doanC_.Services.Offline
{
    public class TranslationCacheService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _dbPath;

        public TranslationCacheService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "translation_cache.db");
            InitDatabase();
        }

        private async void InitDatabase()
        {
            try
            {
                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<CachedTranslation>();
                Debug.WriteLine("[TranslationCache] Database initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TranslationCache] Init error: {ex.Message}");
            }
        }

        private async Task<SQLiteAsyncConnection> GetDatabase()
        {
            if (_database == null)
            {
                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<CachedTranslation>();
            }
            return _database;
        }

        /// <summary>
        /// Lấy bản dịch từ cache hoặc gọi dịch mới
        /// </summary>
        public async Task<string> GetOrTranslateAsync(int poiId, string originalText, string targetLang, Func<string, string, Task<string>> translateFunc)
        {
            try
            {
                if (string.IsNullOrEmpty(originalText))
                    return originalText;

                // Nếu là tiếng Việt, không cần dịch
                if (targetLang == "vi")
                    return originalText;

                var db = await GetDatabase();

                // Kiểm tra cache
                var cached = await db.Table<CachedTranslation>()
                    .Where(t => t.PoiId == poiId && t.TargetLang == targetLang)
                    .FirstOrDefaultAsync();

                if (cached != null && !string.IsNullOrEmpty(cached.TranslatedText))
                {
                    Debug.WriteLine($"[TranslationCache] ✅ Using cached translation for POI {poiId} ({targetLang})");
                    return cached.TranslatedText;
                }

                // Chưa có cache, gọi dịch
                Debug.WriteLine($"[TranslationCache] 🌐 Calling translate for POI {poiId} ({targetLang})");
                var translated = await translateFunc(originalText, targetLang);

                // Lưu cache
                if (!string.IsNullOrEmpty(translated) && translated != originalText)
                {
                    await db.InsertAsync(new CachedTranslation
                    {
                        PoiId = poiId,
                        OriginalText = originalText,
                        TranslatedText = translated,
                        TargetLang = targetLang,
                        CachedAt = DateTime.Now
                    });
                    Debug.WriteLine($"[TranslationCache] 💾 Saved translation for POI {poiId} ({targetLang})");
                }

                return translated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TranslationCache] ❌ Error: {ex.Message}");
                return originalText;
            }
        }

        /// <summary>
        /// Xóa cache cũ
        /// </summary>
        public async Task ClearOldCacheAsync(int daysToKeep = 30)
        {
            try
            {
                var db = await GetDatabase();
                var cutoff = DateTime.Now.AddDays(-daysToKeep);
                var oldEntries = await db.Table<CachedTranslation>()
                    .Where(t => t.CachedAt < cutoff)
                    .ToListAsync();

                foreach (var entry in oldEntries)
                {
                    await db.DeleteAsync(entry);
                }

                Debug.WriteLine($"[TranslationCache] 🗑️ Deleted {oldEntries.Count} old cache entries");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TranslationCache] Clear cache error: {ex.Message}");
            }
        }
    }

    [Table("CachedTranslation")]
    public class CachedTranslation
    {
        [PrimaryKey, AutoIncrement] 
        public int Id { get; set; }

        [Indexed]
        public int PoiId { get; set; }

        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;

        [Indexed]
        public string TargetLang { get; set; } = string.Empty;

        public DateTime CachedAt { get; set; } = DateTime.Now;
    }
}