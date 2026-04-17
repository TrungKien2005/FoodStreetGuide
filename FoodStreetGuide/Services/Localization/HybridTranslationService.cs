using System.Diagnostics;
using System.Text.RegularExpressions;

namespace doanC_.Services.Localization
{
    /// <summary>
    /// Optimized Google Translate Only Service
    /// Strategy: Google Translate API Only (no fallback needed - 83% success rate is sufficient)
    /// 
    /// Removed: MyMemory (rate limited) + LibreTranslate (network errors)
    /// Reason: Google Translate is most reliable based on production logs
    /// </summary>
    public class HybridTranslationService
    {
     private string _currentLanguage = "en";
 private readonly GoogleTranslateOnlyService _googleService;

        // ? Translation cache to reduce API calls
        private readonly Dictionary<string, string> _translationCache = new();
        private const int MaxCacheSize = 1000;

        public HybridTranslationService()
        {
            _googleService = new GoogleTranslateOnlyService();
   }

        public void SetLanguage(string languageCode)
        {
          _currentLanguage = languageCode;
 _googleService?.SetLanguage(languageCode);
        Debug.WriteLine($"[Translation] ?? Language set to: {languageCode}");
        }

        public void Initialize()
        {
 _googleService?.Initialize();
            Debug.WriteLine("[Translation] ? Initialized with Google Translate API Only");
  }

        /// <summary>
        /// Optimized translation with caching
    /// </summary>
 public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
          if (string.IsNullOrEmpty(text))
            return text;

            // ? OPTIMIZATION 1: Skip translation for time/number formats
            if (!ShouldTranslate(text))
            {
   Debug.WriteLine($"[Translation] ?? Skip translating '{text}' (time/number format)");
   return text;
     }

          // ? OPTIMIZATION 2: Check cache first
         string cacheKey = $"{text}_{targetLanguage}";
            if (_translationCache.TryGetValue(cacheKey, out var cached))
            {
     Debug.WriteLine($"[Translation] ?? Cache hit: '{cached}'");
    return cached;
   }

            try
 {
                Debug.WriteLine($"[Translation] ?? Translating '{text.Substring(0, Math.Min(30, text.Length))}...' to {targetLanguage}");

     // Use Google Translate API (only option now)
    var result = await _googleService.TranslateTextAsync(text, targetLanguage);
            
     if (!string.IsNullOrEmpty(result) && result != text)
          {
          Debug.WriteLine($"[Translation] ? Translated: '{result}'");
         _translationCache[cacheKey] = result;
return result;
     }

      Debug.WriteLine("[Translation] ?? Translation failed, returning original text");
          }
            catch (Exception ex)
 {
       Debug.WriteLine($"[Translation] ? Error: {ex.Message}");
            }

            // Fallback: Return original text
            _translationCache[cacheKey] = text;
          return text;
        }

        /// <summary>
        /// Batch translation for better performance
 /// Translates multiple texts in parallel
    /// </summary>
        public async Task<Dictionary<string, string>> TranslateBatchAsync(
   List<string> texts,
   string targetLanguage)
     {
            var results = new Dictionary<string, string>();

  if (texts == null || texts.Count == 0)
     return results;

            try
        {
          Debug.WriteLine($"[Translation] ?? Batch translating {texts.Count} items...");

                // Filter and prepare texts that need translation
    var textsToTranslate = texts
     .Where(t => !string.IsNullOrEmpty(t) && ShouldTranslate(t))
  .Distinct()
           .ToList();

                if (textsToTranslate.Count == 0)
    {
            // All texts are time/number formats - return as-is
   foreach (var text in texts)
         {
              results[text] = text;
         }
        return results;
            }

                // ? Parallel translation (max 5 concurrent tasks to avoid rate limiting)
    var semaphore = new System.Threading.SemaphoreSlim(5);
       var tasks = textsToTranslate.Select(async text =>
         {
 await semaphore.WaitAsync();
               try
              {
       var translated = await TranslateTextAsync(text, targetLanguage);
        return new { Original = text, Translated = translated };
        }
  finally
           {
                  semaphore.Release();
            }
         });

          var translatedItems = await Task.WhenAll(tasks);

          foreach (var item in translatedItems)
                {
               results[item.Original] = item.Translated;
                }

    // Add non-translated items
                foreach (var text in texts.Where(t => !results.ContainsKey(t)))
        {
       results[text] = text;
                }

    double successCount = translatedItems.Count(x => x.Translated != x.Original);
        double successRate = translatedItems.Length > 0 ? (successCount / translatedItems.Length * 100) : 0;
    Debug.WriteLine($"[Translation] ? Batch translation completed. Success rate: {successRate:F1}%");
          }
       catch (Exception ex)
    {
    Debug.WriteLine($"[Translation] ? Batch translation error: {ex.Message}");

  // Fallback: return original texts
   foreach (var text in texts)
        {
      results[text] = text;
       }
  }

   return results;
        }

     /// <summary>
        /// ? OPTIMIZATION: Determine if text should be translated
        /// Skip:
    /// - Time formats (HH:MM - HH:MM)
        /// - Price formats (999.999 VN?)
        /// - Pure numbers
        /// - Pure special characters
 /// </summary>
        private bool ShouldTranslate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
        return false;

       // Check if text is only time format (13:00 - 00:00)
            if (Regex.IsMatch(text, @"^(\d{1,2}:\d{2}\s*-?\s*\d{1,2}:\d{2})?$"))
          return false;

       // Check if text is only numbers, spaces, dashes, commas, dots, currency
       if (Regex.IsMatch(text, @"^[\d\s\-:.,VN?€$%]*$"))
         return false;

            return true;
        }

        /// <summary>
      /// Clear cache when language changes or manually
        /// </summary>
        public void ClearCache()
        {
            _translationCache.Clear();
       Debug.WriteLine("[Translation] ??? Translation cache cleared");
        }

        /// <summary>
/// Get cache statistics
    /// </summary>
        public void LogCacheStats()
  {
        Debug.WriteLine($"[Translation] ?? Cache Stats: {_translationCache.Count} items stored");
      _googleService.LogStatistics();
     }

        /// <summary>
      /// Reset all statistics
        /// </summary>
 public void ResetStatistics()
  {
   _googleService.ResetStatistics();
       ClearCache();
    Debug.WriteLine("[Translation] ?? All statistics reset");
    }
    }
}
