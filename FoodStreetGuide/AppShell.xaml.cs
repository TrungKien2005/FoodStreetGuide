using doanC_.Views;
using doanC_.Views.Language;
using doanC_.Services.Localization;
using System.Diagnostics;

namespace doanC_
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            // Ẩn thanh navigation bar của Shell
            Shell.SetNavBarIsVisible(this, false);

            this.InitializeComponent();

            // Đăng ký routes cho navigation
            Routing.RegisterRoute("MapPage", typeof(MapPage));
            Routing.RegisterRoute("PoiListPage", typeof(PoiListPage));
            Routing.RegisterRoute("PoiDetailPage", typeof(PoiDetailPage));
            Routing.RegisterRoute("QrScannerPage", typeof(QrScannerPage));
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("ApiDebugPage", typeof(ApiDebugPage));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                UpdateTabBarTitles();
            });
        }

        private void UpdateTabBarTitles()
        {
            try
            {
                var currentLang = AppResources.GetCurrentLanguage();
                Debug.WriteLine($"[AppShell] 🌐 Current language: {currentLang}");

                if (this.Items?.Count > 0 && this.Items[0] is TabBar tabBar)
                {
                    string[] keys = { "TabMap", "TabPoi", "TabQr", "TabSettings" };
                    for (int i = 0; i < tabBar.Items.Count && i < keys.Length; i++)
                    {
                        if (tabBar.Items[i] is ShellSection section)
                        {
                            section.Title = AppResources.GetString(keys[i]);
                            Debug.WriteLine($"[AppShell] Set item {i} title to: {section.Title}");
                        }
                    }
                }

                Debug.WriteLine("[AppShell] ✅ UpdateTabBarTitles completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] ⚠️ Error: {ex.Message}");
            }
        }
    }
}