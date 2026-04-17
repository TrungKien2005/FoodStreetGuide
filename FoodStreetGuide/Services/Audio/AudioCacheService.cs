using System;
using System.Diagnostics; // ← THÊM DÒNG NÀY
using System.IO;
using System.Threading.Tasks;
using doanC_.Models;
using doanC_.Services.Api;

namespace doanC_.Services.Audio
{
    public class AudioCacheService
    {
        private static AudioCacheService? _instance;
        public static AudioCacheService Instance => _instance ??= new AudioCacheService();

        private readonly string _cacheDir;
        private readonly ApiService _apiService;

        private AudioCacheService()
        {
            _apiService = new ApiService();
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "audio_cache");

            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }

        /// <summary>
        /// Lấy audio từ cache hoặc tải về
        /// </summary>
        public async Task<string?> GetAsync(string text, string language)
        {
            try
            {
                var fileName = $"{GetHash(text)}_{language}.mp3";
                var filePath = Path.Combine(_cacheDir, fileName);

                // Nếu có trong cache thì trả về
                if (File.Exists(filePath))
                {
                    Debug.WriteLine($"[AudioCache] Found in cache: {fileName}");
                    return filePath;
                }

                Debug.WriteLine($"[AudioCache] Not found in cache: {fileName}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioCache] Get error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lưu audio vào cache
        /// </summary>
        public async Task<bool> SaveAsync(string text, string language, byte[] audioData)
        {
            try
            {
                var fileName = $"{GetHash(text)}_{language}.mp3";
                var filePath = Path.Combine(_cacheDir, fileName);

                await File.WriteAllBytesAsync(filePath, audioData);
                Debug.WriteLine($"[AudioCache] Saved: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioCache] Save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa cache cũ
        /// </summary>
        public void ClearOldCache(int daysToKeep = 7)
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-daysToKeep);
                var files = Directory.GetFiles(_cacheDir);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastAccessTime < cutoff)
                    {
                        File.Delete(file);
                        Debug.WriteLine($"[AudioCache] Deleted old cache: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioCache] ClearOldCache error: {ex.Message}");
            }
        }

        private string GetHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 16);
        }

        /// <summary>
        /// Lấy kích thước cache (MB)
        /// </summary>
        public double GetCacheSizeMB()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDir);
                long totalBytes = 0;

                foreach (var file in files)
                {
                    totalBytes += new FileInfo(file).Length;
                }

                return Math.Round(totalBytes / (1024.0 * 1024.0), 2);
            }
            catch
            {
                return 0;
            }
        }
    }
}