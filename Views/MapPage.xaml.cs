using doanC_.Services;
using doanC_.Models;

namespace doanC_.Views;

public partial class MapPage : ContentPage
{
    private LocationService locationService = new();
    private MapService mapService = new();
    private LocationPointService pointService = new();

    public MapPage()
    {
        InitializeComponent();

        mapService.InitializeMap(map);
        
        // Gọi async method đúng cách khi page loaded
        Loaded += async (s, e) => await LoadMap();
    }

    private async Task LoadMap()
    {
        try
        {
            // Yêu cầu permission GPS trước
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                // Nếu không có permission, hiển thị map TP.HCM mặc định
                mapService.FocusToLocation(map, 10.7726, 106.6980);
        
                var poiLocations = pointService.GetLocations();
                mapService.AddLocationPoints(map, poiLocations);
                return;
            }

            var location = await locationService.GetCurrentLocationAsync();

            if (location != null)
            {
                // Focus vào vị trí hiện tại
                mapService.FocusToLocation(
                    map,
                    location.Latitude,
                    location.Longitude);

                // Hiển thị vị trí user (điểm xanh)
                mapService.ShowUserLocation(
                    map,
                    location.Latitude,
                    location.Longitude);
            }
            else
            {
                // Nếu lấy location thất bại, hiển thị map TP.HCM
                mapService.FocusToLocation(map, 10.7726, 106.6980);
            }

            // Hiển thị các địa điểm quan tâm (điểm đỏ)
            var locations = pointService.GetLocations();
            mapService.AddLocationPoints(map, locations);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Map error: {ex.Message}");
        }
    }

    private async void OnViewPoiListClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//PoiListPage");
    }

    private async void OnScanQrClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//QrScannerPage");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}