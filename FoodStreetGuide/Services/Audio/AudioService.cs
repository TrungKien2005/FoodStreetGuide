using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace doanC_.Services.Audio
{
    public class AudioService
    {
        private static AudioService? _instance;
        public static AudioService Instance => _instance ??= new AudioService();

        private IAudioPlayer? _player;
        private readonly IAudioManager _audioManager;

        private AudioService()
        {
            _audioManager = AudioManager.Current;
        }

        /// <summary>
        /// Phát audio từ file path
        /// </summary>
        public async Task<bool> PlayAsync(string filePath)
        {
            try
            {
                Stop();

                if (string.IsNullOrEmpty(filePath))
                    return false;

                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[AudioService] File not found: {filePath}");
                    return false;
                }

                _player = _audioManager.CreatePlayer(filePath);
                _player.Play();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Play error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Phát audio từ stream
        /// </summary>
        public async Task<bool> PlayAsync(Stream audioStream)
        {
            try
            {
                Stop();

                if (audioStream == null)
                    return false;

                _player = _audioManager.CreatePlayer(audioStream);
                _player.Play();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Play stream error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dừng audio
        /// </summary>
        public void Stop()
        {
            try
            {
                if (_player != null)
                {
                    if (_player.IsPlaying)
                        _player.Stop();

                    _player.Dispose();
                    _player = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Stop error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạm dừng
        /// </summary>
        public void Pause()
        {
            try
            {
                if (_player != null && _player.IsPlaying)
                    _player.Pause();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Pause error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tiếp tục phát
        /// </summary>
        public void Resume()
        {
            try
            {
                if (_player != null && !_player.IsPlaying)
                    _player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Resume error: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra đang phát không
        /// </summary>
        public bool IsPlaying => _player != null && _player.IsPlaying;

        /// <summary>
        /// Lấy thời lượng audio (giây)
        /// </summary>
        public double Duration => _player?.Duration ?? 0;

        /// <summary>
        /// Lấy vị trí hiện tại (giây)
        /// </summary>
        public double CurrentPosition => _player?.CurrentPosition ?? 0;

        /// <summary>
        /// Tự động dừng sau khi phát xong
        /// </summary>
        public async Task PlayAndWaitAsync(string filePath)
        {
            await PlayAsync(filePath);

            // Chờ đến khi phát xong
            while (IsPlaying)
            {
                await Task.Delay(100);
            }
        }
    }
}