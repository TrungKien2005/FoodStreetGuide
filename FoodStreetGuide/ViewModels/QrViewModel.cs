using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using doanC_.Services.Localization;

namespace doanC_.ViewModels
{
    public class QrScannerViewModel : INotifyPropertyChanged, ILanguageRefresh
    {
        private string _qrScannerTitle;
        private string _qrScannerSubtitle;
        private string _qrInstructions;
        private string _qrManualInput;

        public string QrScannerTitle
        {
            get => _qrScannerTitle;
            set { if (_qrScannerTitle != value) { _qrScannerTitle = value; OnPropertyChanged(); } }
        }

        public string QrScannerSubtitle
        {
            get => _qrScannerSubtitle;
            set { if (_qrScannerSubtitle != value) { _qrScannerSubtitle = value; OnPropertyChanged(); } }
        }

        public string QrInstructions
        {
            get => _qrInstructions;
            set { if (_qrInstructions != value) { _qrInstructions = value; OnPropertyChanged(); } }
        }

        public string QrManualInput
        {
            get => _qrManualInput;
            set { if (_qrManualInput != value) { _qrManualInput = value; OnPropertyChanged(); } }
        }

        public QrScannerViewModel()
        {
            LanguageChangeManager.Register(this);
            LoadLanguage();
        }

        public void LoadLanguage()
        {
            QrScannerTitle   = AppResources.GetString("QrScannerTitle");
            QrScannerSubtitle= AppResources.GetString("QrScannerSubtitle");
            QrInstructions   = AppResources.GetString("QrInstructions");
            QrManualInput    = AppResources.GetString("QrManualInput");
        }

        public void RefreshLanguage() => LoadLanguage();

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
