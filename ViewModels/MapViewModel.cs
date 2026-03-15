using doanC_.Models;
using System.Collections.ObjectModel;

namespace doanCSharp.ViewModels
{
    public class MapViewModel
    {
        public ObservableCollection<LocationPoint> Points { get; set; }

        public MapViewModel()
        {
            Points = new ObservableCollection<LocationPoint>()
            {
                new LocationPoint("Bến Thành", "Chợ Bến Thành",10.7725,106.6980),
                new LocationPoint("Nhà thờ Đức Bà","Notre Dame",10.7798,106.6992)
            };
        }
    }
}