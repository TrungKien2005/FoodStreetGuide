using System;
using Microsoft.Maui.Controls;

namespace doanC_.Views
{
    public partial class LanguageSelectionPage : ContentPage
    {
        public LanguageSelectionPage()
        {
            InitializeComponent();
        }

        private async void OnVietnameseSelected(object sender, EventArgs e)
        {
            await SelectLanguage("vi");
        }

        private async void OnEnglishSelected(object sender, EventArgs e)
        {
            await SelectLanguage("en");
        }

        private async void OnChineseSelected(object sender, EventArgs e)
        {
            await SelectLanguage("zh");
        }

        private async void OnJapaneseSelected(object sender, EventArgs e)
        {
            await SelectLanguage("ja");
        }

        private async void OnKoreanSelected(object sender, EventArgs e)
        {
            await SelectLanguage("ko");
        }

        private async Task SelectLanguage(string languageCode)
        {
            // L?u ngôn ng? ?ã ch?n
            Preferences.Set("SelectedLanguage", languageCode);

            // Chuyen trang b?n ??
            await Shell.Current.GoToAsync("//MapPage");
        }

        // Add this event handler for the Continue button
        private void OnContinueClicked(object sender, EventArgs e)
        {
            // TODO: Add your logic for the continue button here
        }
    }
}
