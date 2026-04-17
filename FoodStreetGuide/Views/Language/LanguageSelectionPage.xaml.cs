using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using doanC_.Views;
using doanC_.Services.Localization;
using doanC_.Helpers;
using System.Diagnostics;

namespace doanC_.Views.Language
{
    public partial class LanguageSelectionPage : ContentPage
    {
        private HybridTranslationService _translationService;

        public LanguageSelectionPage()
        {
            InitializeComponent();
            _translationService = new HybridTranslationService();
        }

        private async void OnSelectVietnamese(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("vi");
        }

        private async void OnSelectEnglish(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("en");
        }

        private async void OnSelectChinese(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("zh");
        }

        private async void OnSelectFrench(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("fr");
        }

        private async void OnSelectSpanish(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("es");
        }

        private async void OnSelectJapanese(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("ja");
        }

        private async void OnSelectKorean(object sender, EventArgs e)
        {
            SetLanguageAndNavigate("ko");
        }

        private void SetLanguageAndNavigate(string languageCode)
        {
            // ?? C?p nh?t AppResources
            AppResources.SetLanguage(languageCode);

            _translationService?.SetLanguage(languageCode);
            _translationService?.Initialize();

            Preferences.Set("AppLanguage", languageCode);

            Debug.WriteLine($"[LanguageSelection] ?? Language set to: {languageCode}");

            // T?o AppShell m?i v?i ngôn ng? v?a set
            var newShell = new AppShell();
            Application.Current.MainPage = newShell;
        }
    }
}
