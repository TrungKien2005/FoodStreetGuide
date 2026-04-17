using doanC_.Services.Localization;
using doanC_.ViewModels;

namespace doanC_
{
    public class PoiListViewModel : ILanguageRefresh
    {
        public string PoiListTitle        => AppResources.GetString("PoiListTitle");
        public string ExploreSubtitle     => AppResources.GetString("ExploreSubtitle");
        public string SearchPlaceholder   => AppResources.GetString("SearchPlaceholder");
        public string AllCategories       => AppResources.GetString("AllCategories");
        public string Restaurant          => AppResources.GetString("Restaurant");
        public string Location            => AppResources.GetString("Location");
        public string History             => AppResources.GetString("History");

        public PoiListViewModel()
        {
            LanguageChangeManager.Register(this);
        }

        // ???c g?i khi ngôn ng? ??i ? l?n binding ti?p theo s? l?y text m?i
        public void RefreshLanguage()
        {
            // Không c?n code n?u ch? dùng expression property nh? trên
        }
    }
}