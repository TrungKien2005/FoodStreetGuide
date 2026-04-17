using doanC_.Services.Localization;

namespace doanC_.ViewModels
{
    public class PoiDetailViewModel : ILanguageRefresh
    {
        public string PoiDetailTitle          => AppResources.GetString("PoiDetailTitle");
        public string OpenStatus              => AppResources.GetString("OpenStatus");
        public string DirectionsButton        => AppResources.GetString("DirectionsButton");
        public string AudioButton             => AppResources.GetString("AudioButton");
        public string AudioPlayerTitle        => AppResources.GetString("AudioPlayerTitle");
        public string PlayButton              => AppResources.GetString("PlayButton");
        public string AudioLanguagePickerTitle=> AppResources.GetString("AudioLanguagePickerTitle");

        public PoiDetailViewModel()
        {
            LanguageChangeManager.Register(this);
        }

        public void RefreshLanguage()
        {
        }
    }
}
