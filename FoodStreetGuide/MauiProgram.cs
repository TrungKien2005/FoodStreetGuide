using doanC_.Services.Api;
using doanC_.Services.Audio;
using doanC_.Services.Data;
using doanC_.Services.Localization;
using doanC_.Services.LocationTracking;
using doanC_.Services.Offline;
using doanC_.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;
using doanC_.Services.LocationTracking;

namespace doanC_
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            MauiAppBuilder mauiAppBuilder = builder
                .UseMauiApp<App>()
                .UseMauiMaps();
            mauiAppBuilder
                .UseSkiaSharp()                       
                .UseBarcodeReader()               
                .ConfigureFonts(fonts =>            
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-solid-900.ttf", "FontAwesome");
                });

            // 3. ĐĂNG KÝ SERVICES (Dependency Injection)

            // 3.1. DATA SERVICES
            builder.Services.AddSingleton<SQLiteService>();        // Database local SQLite
                                                                   // builder.Services.AddSingleton<SeedDataService>();   // Tạo dữ liệu mẫu (tạm comment)

            // 3.2. LOCALIZATION SERVICES
            builder.Services.AddSingleton<HybridTranslationService>(); 
            builder.Services.AddSingleton<TranslationCacheService>(); 

            // 3.3. AUDIO SERVICES
            builder.Services.AddSingleton<TTSService>();            

            // 3.4. API & OFFLINE SERVICES
            builder.Services.AddSingleton<ApiService>();           
            builder.Services.AddSingleton<OfflinePoiService>();     
            builder.Services.AddSingleton<OfflineSyncService>();    

            // 3.5. PAGES (UI)
            builder.Services.AddSingleton<MainPage>();            
            builder.Services.AddTransient<ApiDebugPage>();
            builder.Services.AddTransient<SimpleTestPage>();
            // 3.6.LOCATION SERVICES
            //builder.Services.AddSingleton<LocationService>();
            //builder.Services.AddSingleton<GeoFenceService>();
            builder.Services.AddSingleton<DeviceTrackingService>();

            // 3.7. MAP SERVICES
            //builder.Services.AddSingleton<MapService>(); 

            // 3.8. QR SERVICES
            //builder.Services.AddSingleton<QrService>();     

            // 3.9. AUDIO CACHE
            builder.Services.AddSingleton<AudioCacheService>();

            // 4. CẤU HÌNH LOGGING (DEBUG MODE)
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // 5. BUILD VÀ TRẢ VỀ APP
            return builder.Build();
        }
    }
}