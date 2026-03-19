using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using doanC_.Services;
using doanC_.Models;
using Microsoft.Maui.Maps;

namespace doanC_.Views;

public partial class MapPage : ContentPage
{
    private LocationService locationService = new();
    private LocationPointService pointService = new();

    private Location? lastLocation;

    public MapPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadMap();
    }

    private async Task LoadMap()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                // ✅ FIX: đặt vị trí mặc định (Việt Nam) trước
                var defaultLocation = new Location(10.7726, 106.6980); // TP.HCM

                map.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        defaultLocation,
                        Distance.FromMeters(200)));

                // 🚀 Sau đó mới tracking GPS
                await locationService.StartTrackingAsync(location =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var current = new Location(location.Latitude, location.Longitude);

                        // 🧠 Nếu đã có vị trí trước đó → kiểm tra khoảng cách
                        if (lastLocation != null &&
                            Location.CalculateDistance(lastLocation, current, DistanceUnits.Kilometers) * 1000 < 10)
                        {
                            return;
                        }

                        lastLocation = current;

                        // 🎯 Zoom thông minh
                        double zoom = 100;

                        if (location.Speed.HasValue && location.Speed > 5)
                            zoom = 200;
                        else
                            zoom = 80;

                        await Task.Delay(30);

                        map.MoveToRegion(
                            MapSpan.FromCenterAndRadius(
                                current,
                                Distance.FromMeters(zoom)));
                    });
                });
            }

            // 📌 Add POI
            var pois = pointService.GetLocations();

            foreach (var p in pois)
            {
                map.Pins.Add(new Pin
                {
                    Label = p.Name,
                    Address = p.Description,
                    Location = new Location(p.Latitude, p.Longitude)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        locationService.StopTracking();
    }
}