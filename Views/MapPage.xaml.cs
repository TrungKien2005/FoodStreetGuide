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

        LoadMap();
    }

    private async void LoadMap()
    {
        var location = await locationService.GetCurrentLocationAsync();

        if (location != null)
        {
            mapService.FocusToLocation(
                map,
                location.Latitude,
                location.Longitude);

            // hiển thị vị trí user
            mapService.ShowUserLocation(
                map,
                location.Latitude,
                location.Longitude);
        }

        var locations = pointService.GetLocations();

        mapService.AddLocationPoints(map, locations);
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