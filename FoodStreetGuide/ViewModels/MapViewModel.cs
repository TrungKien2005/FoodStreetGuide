using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using doanC_.Models;
using doanC_.Services.Localization;
using Microsoft.Maui.Devices.Sensors;

namespace doanC_.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged, ILanguageRefresh
    {
        public ObservableCollection<LocationPoint> Points { get; set; }

        private string _searchPlaceholder;
        private string _nearbyText;
        private string _poiName;
        private string _listenText;
        private string _playButtonText;
        private LocationPoint _nearestPoi;

        public string SearchPlaceholder
        {
            get => _searchPlaceholder;
            set { if (_searchPlaceholder != value) { _searchPlaceholder = value; OnPropertyChanged(); } }
        }

        public string NearbyText
        {
            get => _nearbyText;
            set { if (_nearbyText != value) { _nearbyText = value; OnPropertyChanged(); } }
        }

        public string PoiName
        {
            get => _poiName;
            set { if (_poiName != value) { _poiName = value; OnPropertyChanged(); } }
        }

        public string ListenText
        {
            get => _listenText;
            set { if (_listenText != value) { _listenText = value; OnPropertyChanged(); } }
        }

        public string PlayButtonText
        {
            get => _playButtonText;
            set { if (_playButtonText != value) { _playButtonText = value; OnPropertyChanged(); } }
        }

        public LocationPoint NearestPoi
        {
            get => _nearestPoi;
            set { if (_nearestPoi != value) { _nearestPoi = value; OnPropertyChanged(); } }
        }

        public MapViewModel()
        {
            Points = new ObservableCollection<LocationPoint>();
            LoadLanguage();
            LanguageChangeManager.Register(this);
        }

        private void LoadLanguage()
        {
            SearchPlaceholder = AppResources.GetString("FindPoiPlaceholder");
            ListenText        = AppResources.GetString("TapToListen2");
            PlayButtonText    = AppResources.GetString("PlayCommentary");
            
            // ✅ Lấy từ AppResources
          NearbyText = AppResources.GetString("NearbyDistance");
            
        // ✅ Giá trị mặc định
     if (string.IsNullOrEmpty(PoiName))
    PoiName = AppResources.GetString("SelectPoi") ?? "Chọn địa điểm";
        }

/// <summary>
        /// Tính POI gần nhất và cập nhật UI
        /// </summary>
        public void UpdateNearestPoi(Location userLocation)
        {
   if (userLocation == null || Points == null || Points.Count == 0)
    {
     System.Diagnostics.Debug.WriteLine($"[MapViewModel] UpdateNearestPoi - Invalid input. Location: {userLocation != null}, Points count: {Points?.Count ?? 0}");
           return;
            }

        try
            {
        System.Diagnostics.Debug.WriteLine($"[MapViewModel] Calculating nearest POI from {Points.Count} points...");

     // Tìm POI gần nhất
                LocationPoint nearest = null;
       double minDistance = double.MaxValue;

   foreach (var poi in Points)
        {
     double distance = CalculateDistance(
      userLocation.Latitude,
           userLocation.Longitude,
        poi.Latitude,
    poi.Longitude
       );

    System.Diagnostics.Debug.WriteLine($"[MapViewModel] POI: {poi.Name} - Distance: {distance:F2}m");

        if (distance < minDistance)
        {
      minDistance = distance;
       nearest = poi;
           }
   }

      if (nearest != null)
{
                    NearestPoi = nearest;
     PoiName = nearest.Name;
       NearbyText = string.Format(AppResources.GetString("NearbyDistance"), (int)minDistance);
      PlayButtonText = AppResources.GetString("PlayCommentary");
    
           System.Diagnostics.Debug.WriteLine($"[MapViewModel] ✅ Updated - Nearest: {PoiName} ({(int)minDistance}m)");
  System.Diagnostics.Debug.WriteLine($"[MapViewModel] PoiName = {PoiName}");
   System.Diagnostics.Debug.WriteLine($"[MapViewModel] NearbyText = {NearbyText}");
 }
      else
   {
          System.Diagnostics.Debug.WriteLine($"[MapViewModel] ❌ No nearest POI found");
  }
            }
 catch (Exception ex)
            {
      System.Diagnostics.Debug.WriteLine($"[MapViewModel] ❌ Error updating nearest POI: {ex.Message}");
  System.Diagnostics.Debug.WriteLine($"[MapViewModel] ❌ Stack: {ex.StackTrace}");
         }
 }
        /// <summary>
        /// Tính khoảng cách giữa 2 tọa độ (Haversine formula)
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
  double R = 6371000; // Bán kính Trái Đất (meter)
     double dLat = ToRad(lat2 - lat1);
         double dLon = ToRad(lon2 - lon1);

  double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
         Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
          Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

       double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
  return R * c;
        }

        private double ToRad(double val) => val * Math.PI / 180;

        public void RefreshLanguage() => LoadLanguage();

   public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null)
   => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}