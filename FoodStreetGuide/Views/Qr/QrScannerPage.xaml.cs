using System.Text.Json;
using doanC_.Models;
using doanC_.Services;
using doanC_.Services.Audio;
using doanC_.Services.Localization;
using doanC_.Services.Data;
using doanC_.ViewModels;
using ZXing.Net.Maui;
using doanC_.Services.Geo;
using System.Diagnostics;
using doanC_.Helpers;
using doanC_.Services.LocationTracking;  // 👈 ĐÃ CÓ

namespace doanC_.Views
{
    public partial class QrScannerPage : ContentPage
    {
        private bool _isScanning = true;
        private QrScannerViewModel _viewModel;
        private TTSService _ttsService;
        private HybridTranslationService? _translationService;
        private SQLiteService? _sqliteService;
        private string _currentLanguage = "vi";
        private bool _isSpeaking = false;
        private DeviceTrackingService? _deviceTrackingService;  // 👈 THÊM DÒNG NÀY

        public QrScannerPage()
        {
            InitializeComponent();
            _viewModel = new QrScannerViewModel();
            this.BindingContext = _viewModel;

            _ttsService = ServiceHelper.GetService<TTSService>() ?? new TTSService();
            _translationService = new HybridTranslationService();
            _sqliteService = ServiceHelper.GetService<SQLiteService>();
            _deviceTrackingService = ServiceHelper.GetService<DeviceTrackingService>();  // 👈 THÊM DÒNG NÀY

            LoadSavedLanguage();
        }

        private void LoadSavedLanguage()
        {
            var savedLanguage = Preferences.Get("AppLanguage", "vi");
            _currentLanguage = savedLanguage;
            Debug.WriteLine($"[QrScannerPage] 🌐 Loaded language: {_currentLanguage}");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Track activity khi mở QR Scanner
            TrackActivity("OpenScanner");  // 👈 THÊM DÒNG NÀY

            _viewModel.LoadLanguage();
            RequestCameraPermissionAndStartScanning();
        }

        // 👈 THÊM METHOD NÀY ĐỂ TRACK ACTIVITY
        private async void TrackActivity(string activityType, int pointId = 0)
        {
            try
            {
                if (_deviceTrackingService != null)
                {
                    await _deviceTrackingService.TrackActivityAsync(activityType, pointId);
                    Debug.WriteLine($"[QrScannerPage] 📊 Tracked activity: {activityType}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QrScannerPage] ⚠️ Track activity error: {ex.Message}");
            }
        }

        private async void RequestCameraPermissionAndStartScanning()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert(AppResources.GetString("Error"), AppResources.GetString("CameraPermissionDenied"), AppResources.GetString("OK"));
                return;
            }

            cameraView.IsDetecting = true;
        }

        private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (!_isScanning) return;

            var result = e.Results.FirstOrDefault();
            if (result == null) return;

            _isScanning = false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                cameraView.IsDetecting = false;

                await HandleQr(result.Value);

                _isScanning = true;
                cameraView.IsDetecting = true;
            });
        }

        private async Task HandleQr(string qrText)
        {
            try
            {
                Debug.WriteLine($"[QrScannerPage] 📱 QR Text: {qrText}");

                LocationPoint? point = null;
                int scannedPointId = 0;

                if (qrText.StartsWith("poi:", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[QrScannerPage] 📍 Detected POI ID format");
                    point = await HandlePoiIdQrAsync(qrText);

                    // Lấy ID từ QR text
                    var parts = qrText.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int poiId))
                    {
                        scannedPointId = poiId;
                    }
                }
                else
                {
                    Debug.WriteLine($"[QrScannerPage] 📋 Detected JSON format");
                    point = JsonSerializer.Deserialize<LocationPoint>(qrText);
                    if (point != null)
                    {
                        scannedPointId = point.PointId;
                    }
                }

                if (point != null)
                {
                    // 👈 TRACK QR SCAN THÀNH CÔNG
                    TrackActivity("QRScan", scannedPointId);

                    await DisplayAlert(AppResources.GetString("OK"), $"{AppResources.GetString("AddedToFavorite")}: {point.Name}", AppResources.GetString("OK"));

                    GeoFenceService.Instance.AddPoint(point);

                    await ShowLanguageSelectionAndSpeakAsync(point);
                }
                else
                {
                    // 👈 TRACK QR SCAN THẤT BẠI
                    TrackActivity("QRScanFailed", 0);

                    await DisplayAlert("QR", qrText, AppResources.GetString("OK"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QrScannerPage] ❌ Error: {ex.Message}");
                await DisplayAlert(AppResources.GetString("Error"), AppResources.GetString("InvalidQrFormat"), AppResources.GetString("OK"));
            }
        }

        private async Task<LocationPoint?> HandlePoiIdQrAsync(string qrText)
        {
            try
            {
                var parts = qrText.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[1], out int poiId))
                {
                    Debug.WriteLine($"[QrScannerPage] ❌ Invalid POI format: {qrText}");
                    return null;
                }

                Debug.WriteLine($"[QrScannerPage] 🔍 Searching for POI ID: {poiId}");

                if (_sqliteService == null)
                {
                    Debug.WriteLine($"[QrScannerPage] ⚠️ SQLiteService is null");
                    return null;
                }

                var point = await _sqliteService.GetLocationPointByIdAsync(poiId);

                if (point != null)
                {
                    Debug.WriteLine($"[QrScannerPage] ✅ Found POI: {point.Name}");
                    return point;
                }
                else
                {
                    Debug.WriteLine($"[QrScannerPage] ❌ POI not found with ID: {poiId}");
                    await DisplayAlert(AppResources.GetString("Error"), $"Không tìm thấy địa điểm với ID: {poiId}", AppResources.GetString("OK"));
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QrScannerPage] ❌ Error parsing POI ID: {ex.Message}");
                return null;
            }
        }

        private async Task ShowLanguageSelectionAndSpeakAsync(LocationPoint point)
        {
            try
            {
                Debug.WriteLine($"[QrScannerPage] 📢 Showing language selection dialog");

                var languages = new[]
                {
                    "Tiếng Việt (VI)",
                    "English (EN)",
                    "中文 (ZH)",
                    "Français (FR)",
                    "Español (ES)",
                    "日本語 (JA)",
                    "한국어 (KO)"
                };
                var languageCodes = new[] { "vi", "en", "zh", "fr", "es", "ja", "ko" };

                var selectedLanguageIndex = await DisplayActionSheet(
                    "Chọn ngôn ngữ để nghe thuyết minh",
                    AppResources.GetString("Cancel"),
                    null,
                    languages);

                if (selectedLanguageIndex == AppResources.GetString("Cancel") || selectedLanguageIndex == null)
                {
                    Debug.WriteLine($"[QrScannerPage] ⏭️ User cancelled language selection");
                    return;
                }

                var selectedIndex = Array.IndexOf(languages, selectedLanguageIndex);
                if (selectedIndex < 0)
                {
                    Debug.WriteLine($"[QrScannerPage] ⚠️ Invalid language selection");
                    return;
                }

                string selectedLanguage = languageCodes[selectedIndex];
                Debug.WriteLine($"[QrScannerPage] 🌍 Selected language: {selectedLanguage}");

                // 👈 TRACK LẦN NGHE AUDIO
                TrackActivity("TTSListen", point.PointId);

                await SpeakRestaurantDescriptionAsync(point, selectedLanguage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QrScannerPage] ❌ Error showing language selection: {ex.Message}");
                await DisplayAlert(AppResources.GetString("Error"), $"Lỗi chọn ngôn ngữ: {ex.Message}", AppResources.GetString("OK"));
            }
        }

        private async Task SpeakRestaurantDescriptionAsync(LocationPoint point, string? selectedLanguage = null)
        {
            try
            {
                if (point == null)
                {
                    Debug.WriteLine("[QrScannerPage] ❌ LocationPoint is null");
                    return;
                }

                if (_isSpeaking)
                {
                    Debug.WriteLine("[QrScannerPage] ⏭️ Already speaking, skipping...");
                    return;
                }

                string languageToUse = selectedLanguage ?? _currentLanguage;

                Debug.WriteLine($"[QrScannerPage] 📄 Processing description for: {point.Name}");
                Debug.WriteLine($"[QrScannerPage] Using language: {languageToUse}");

                if (string.IsNullOrEmpty(point.Description))
                {
                    Debug.WriteLine($"[QrScannerPage] ⚠️ No description for: {point.Name}");
                    return;
                }

                _isSpeaking = true;

                string textToSpeak = point.Description;

                if (languageToUse != "vi")
                {
                    try
                    {
                        Debug.WriteLine($"[QrScannerPage] 🔄 Translating to {languageToUse}...");

                        if (_translationService != null)
                        {
                            textToSpeak = await _translationService.TranslateTextAsync(point.Description, languageToUse) ?? point.Description;
                            Debug.WriteLine($"[QrScannerPage] ✅ Translated text: {textToSpeak}");
                        }
                    }
                    catch (Exception transEx)
                    {
                        Debug.WriteLine($"[QrScannerPage] ⚠️ Translation failed, using original: {transEx.Message}");
                        textToSpeak = point.Description;
                    }
                }

                Debug.WriteLine($"[QrScannerPage] 🔊 Speaking: {textToSpeak.Substring(0, Math.Min(50, textToSpeak.Length))}...");

                await _ttsService.SpeakAsync(textToSpeak, GetLanguageCodeForTTS(languageToUse));

                Debug.WriteLine($"[QrScannerPage] ✅ Finished speaking for: {point.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[QrScannerPage] ❌ Error speaking description: {ex.Message}");
                await DisplayAlert(AppResources.GetString("Error"), $"{AppResources.GetString("AudioError")}: {ex.Message}", AppResources.GetString("OK"));
            }
            finally
            {
                _isSpeaking = false;
            }
        }

        private string GetLanguageCodeForTTS(string languageCode)
        {
            return languageCode switch
            {
                "en" => "en-US",
                "fr" => "fr-FR",
                "es" => "es-ES",
                "zh" => "zh-CN",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
                "vi" => "vi-VN",
                _ => "vi-VN"
            };
        }
    }
}