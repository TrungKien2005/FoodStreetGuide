using doanC_.Services.Localization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace doanC_.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged, ILanguageRefresh
    {
        private string _selectedVoiceDisplay;
        private string _selectedRadiusDisplay;

        public string Settings                => AppResources.GetString("Settings");
        public string LanguageSettingsSection => AppResources.GetString("LanguageSettingsSection");
        public string LanguageLabel           => AppResources.GetString("LanguageLabel");
        public string VoiceTTSLabel           => AppResources.GetString("VoiceTTSLabel");
        public string GpsGeofenceSection      => AppResources.GetString("GpsGeofenceSection");
        public string RadiusActivationLabel   => AppResources.GetString("RadiusActivationLabel");
        public string BackgroundTrackingLabel => AppResources.GetString("BackgroundTrackingLabel");
        public string BatterySaveLabel        => AppResources.GetString("BatterySaveLabel");
        public string OfflineContentSection   => AppResources.GetString("OfflineContentSection");
        public string DownloadOfflineLabel    => AppResources.GetString("DownloadOfflineLabel");
        public string OfflinePackageInfo      => AppResources.GetString("OfflinePackageInfo");

        public string SelectedVoiceDisplay
        {
            get => GetVoiceDisplay();
            set { if (_selectedVoiceDisplay != value) { _selectedVoiceDisplay = value; OnPropertyChanged(); } }
        }

        public string SelectedRadiusDisplay
        {
            get => GetRadiusDisplay();
            set { OnPropertyChanged(); }
        }

        private string GetVoiceDisplay()
        {
            var selectedVoice = Preferences.Get("SelectedVoice", "Gi?ng n?");
            
            // Ki?m tra chính xác - gi?ng ???c l?u s? là "Gi?ng n?" ho?c "Gi?ng nam" (ti?ng Vi?t)
            if (selectedVoice == "Gi?ng n?")
            {
                return AppResources.GetString("FemaleVoice");
            }
            else if (selectedVoice == "Gi?ng nam")
            {
                return AppResources.GetString("MaleVoice");
            }
            else
            {
                // Fallback n?u có giá tr? khác
                return selectedVoice;
            }
        }

        private string GetRadiusDisplay()
        {
     var selectedRadius = Preferences.Get("GeoFenceRadius", "50 mét");
    return selectedRadius;
   }

        public SettingsViewModel()
        {
            LanguageChangeManager.Register(this);
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            OnPropertyChanged(nameof(SelectedVoiceDisplay));
            OnPropertyChanged(nameof(SelectedRadiusDisplay));
        }

        public void RefreshLanguage()
        {
            // Ép refresh UI khi thay ??i ngôn ng?
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(LanguageSettingsSection));
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(VoiceTTSLabel));
            OnPropertyChanged(nameof(GpsGeofenceSection));
            OnPropertyChanged(nameof(RadiusActivationLabel));
            OnPropertyChanged(nameof(BackgroundTrackingLabel));
            OnPropertyChanged(nameof(BatterySaveLabel));
            OnPropertyChanged(nameof(OfflineContentSection));
            OnPropertyChanged(nameof(DownloadOfflineLabel));
            OnPropertyChanged(nameof(OfflinePackageInfo));
            OnPropertyChanged(nameof(SelectedVoiceDisplay));
            OnPropertyChanged(nameof(SelectedRadiusDisplay));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
