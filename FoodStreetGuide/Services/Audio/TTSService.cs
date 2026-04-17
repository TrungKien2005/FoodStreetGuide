using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace doanC_.Services.Audio
{
    /// <summary>
    /// Text-to-Speech Service - Phát âm thanh
    /// </summary>
    public class TTSService
    {
        private bool _isSpeaking = false;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Phát text dạng âm thanh
        /// </summary>
        public async Task SpeakAsync(string text, string language = "en-US")
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine("[TTSService] ❌ Text is empty");
                return;
            }

            // Cancel any previous speech (TextToSpeech API uses CancellationToken).
            await CancelAsync();

            _isSpeaking = true;
            _cts = new CancellationTokenSource();

            try
            {
                Debug.WriteLine($"[TTSService] 🔊 Speaking: {text.Substring(0, Math.Min(50, text.Length))}...");
                Debug.WriteLine($"[TTSService] 📢 Language: {language}");

                AdjustVolume();
                await Task.Delay(200);

                if (TextToSpeech.Default == null)
                {
                    Debug.WriteLine("[TTSService] ❌ TextToSpeech.Default is null!");
                    return;
                }

                // Pick best matching locale for the requested language code.
                Locale? locale = null;
                try
                {
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    locale = locales.FirstOrDefault(l => string.Equals(l.Language, language, StringComparison.OrdinalIgnoreCase))
                    ?? locales.FirstOrDefault(l => language.Length >= 2 && string.Equals(l.Language, language[..2], StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TTSService] ⚠️ Cannot get locales: {ex.Message}");
                }

                var speechOptions = new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = locale
                };

                Debug.WriteLine($"[TTSService] 🗣️ Locale: {speechOptions.Locale?.Language ?? "(default)"}");
                Debug.WriteLine("[TTSService] ⏳ Calling TextToSpeech.Default.SpeakAsync...");

                await TextToSpeech.Default.SpeakAsync(text, speechOptions, _cts.Token);

                Debug.WriteLine("[TTSService] ✅ Speech completed successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[TTSService] ⚠️ Speech canceled");
            }
            catch (NotImplementedException ex)
            {
                Debug.WriteLine($"[TTSService] ❌ TextToSpeech not implemented: {ex.Message}");
            }
            catch (PlatformNotSupportedException ex)
            {
                Debug.WriteLine($"[TTSService] ❌ Platform not supported: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTSService] ❌ Error: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[TTSService] ❌ Stack: {ex.StackTrace}");
            }
            finally
            {
                _isSpeaking = false;

                await Task.Delay(300);
            }
        }

        /// <summary>
        /// Phát text dạng âm thanh với tuỳ chọn giọng
        /// </summary>
        public async Task SpeakAsync(string text, string language = "en-US", string voice = "Giọng nữ")
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine("[TTSService] ❌ Text is empty");
                return;
            }

            // Cancel any previous speech (TextToSpeech API uses CancellationToken).
            await CancelAsync();

            _isSpeaking = true;
            _cts = new CancellationTokenSource();

            try
            {
                Debug.WriteLine($"[TTSService] 🔊 Speaking: {text.Substring(0, Math.Min(50, text.Length))}...");
                Debug.WriteLine($"[TTSService] 📢 Language: {language}");
                Debug.WriteLine($"[TTSService] 👤 Voice: {voice}");

                AdjustVolume();
                await Task.Delay(200);

                if (TextToSpeech.Default == null)
                {
                    Debug.WriteLine("[TTSService] ❌ TextToSpeech.Default is null!");
                    return;
                }

                // Pick best matching locale for the requested language code.
                Locale? locale = null;
                try
                {
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    locale = locales.FirstOrDefault(l => string.Equals(l.Language, language, StringComparison.OrdinalIgnoreCase))
                    ?? locales.FirstOrDefault(l => language.Length >= 2 && string.Equals(l.Language, language[..2], StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TTSService] ⚠️ Cannot get locales: {ex.Message}");
                }

                var speechOptions = new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch = GetPitchForVoice(voice),  // ← Điều chỉnh pitch theo giọng
                    Locale = locale
                };

                Debug.WriteLine($"[TTSService] 🗣️ Locale: {speechOptions.Locale?.Language ?? "(default)"}");
                Debug.WriteLine($"[TTSService] 🎵 Pitch: {speechOptions.Pitch}");
                Debug.WriteLine("[TTSService] ⏳ Calling TextToSpeech.Default.SpeakAsync...");

                await TextToSpeech.Default.SpeakAsync(text, speechOptions, _cts.Token);

                Debug.WriteLine("[TTSService] ✅ Speech completed successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[TTSService] ⚠️ Speech canceled");
            }
            catch (NotImplementedException ex)
            {
                Debug.WriteLine($"[TTSService] ❌ TextToSpeech not implemented: {ex.Message}");
            }
            catch (PlatformNotSupportedException ex)
            {
                Debug.WriteLine($"[TTSService] ❌ Platform not supported: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTSService] ❌ Error: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[TTSService] ❌ Stack: {ex.StackTrace}");
            }
            finally
            {
                _isSpeaking = false;

                await Task.Delay(300);
            }
        }

        /// <summary>
        /// Lấy Pitch dựa vào giọng được chọn
        /// </summary>
        private float GetPitchForVoice(string voice)
        {
            return voice switch
            {
                "Giọng nữ" => 1.3f,  // Giọng nữ: cao hơn
                "Giọng nam" => 0.8f, // Giọng nam: thấp hơn
                _ => 1.0f            // Mặc định
            };
        }

        private void AdjustVolume()
        {
#if __ANDROID__
            try
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

                if (context == null)
                {
                    Debug.WriteLine("[TTSService] ⚠️ Android context is null");
                    return;
                }

                var audioManager = context.GetSystemService(Android.Content.Context.AudioService)
                as Android.Media.AudioManager;

                if (audioManager == null)
                {
                    Debug.WriteLine("[TTSService] ⚠️ AudioManager is null");
                    return;
                }

                int maxVolume = audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
                int currentVolume = audioManager.GetStreamVolume(Android.Media.Stream.Music);

                Debug.WriteLine($"[TTSService] 📢 Volume: {currentVolume}/{maxVolume}");

                audioManager.SetStreamVolume(
                Android.Media.Stream.Music,
                maxVolume,
                Android.Media.VolumeNotificationFlags.ShowUi
                );

                Debug.WriteLine($"[TTSService] 🔊 Volume set to MAX: {maxVolume}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTSService] ⚠️ Volume adjustment error: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Dừng phát âm thanh (hủy từ từ)
        /// </summary>
        public Task CancelAsync()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTSService] ⚠️ Error canceling: {ex.Message}");
            }
            finally
            {
                _isSpeaking = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ✅ DỪNG PHÁT NGAY LẬP TỨC - THÊM MỚI
        /// </summary>
        /// <summary>
        /// ✅ DỪNG PHÁT NGAY LẬP TỨC - SỬA LỖI
        /// </summary>
        public async Task StopImmediatelyAsync()
        {
            try
            {
                Debug.WriteLine("[TTSService] 🛑 Stopping immediately...");

                // Hủy token đang chạy (cách duy nhất để dừng TTS trong MAUI)
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                _isSpeaking = false;

                // Trong MAUI, không có TextToSpeech.Default.Cancel()
                // Chỉ cần hủy token là đủ để dừng SpeakAsync đang chạy

                // Đợi một chút để đảm bảo dừng hoàn toàn
                await Task.Delay(50);

                Debug.WriteLine("[TTSService] ✅ Stopped immediately");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTSService] ❌ StopImmediately error: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra TTS có đang phát không
        /// </summary>
        public bool IsSpeaking()
        {
            return _isSpeaking;
        }

        public bool IsSupported()
        {
            try
            {
                bool supported = TextToSpeech.Default != null;
                Debug.WriteLine($"[TTSService] TTS Supported: {supported}");
                return supported;
            }
            catch
            {
                Debug.WriteLine("[TTSService] ❌ Cannot check TTS support");
                return false;
            }
        }
    }
}