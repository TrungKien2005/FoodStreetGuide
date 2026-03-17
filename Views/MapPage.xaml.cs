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

    private Pin? userPin;
    private bool isFirst = true;

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

            Location center = new Location(10.7726, 106.6980); // fallback

            if (status == PermissionStatus.Granted)
            {
                // 🚀 Bắt đầu tracking liên tục
                await locationService.StartTrackingAsync(location =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var current = new Location(location.Latitude, location.Longitude);

                        // 📍 Tạo hoặc update pin user
                        if (userPin == null)
                        {
                            userPin = new Pin
                            {
                                Label = "Bạn đang ở đây",
                                Location = current
                            };

                            map.Pins.Add(userPin);
                        }
                        else
                        {
                            userPin.Location = current;
                        }

                        // 🎯 Focus chỉ lần đầu
                        if (isFirst)
                        {
                            map.MoveToRegion(
                                MapSpan.FromCenterAndRadius(
                                    current,
                                    Distance.FromMeters(500)));

                            isFirst = false;
                        }
                    });

                    // 👉 Nếu có GeoFence thì gọi ở đây
                    // geoFenceService.CheckLocation(location);
                });
            }

            // 📌 Add POI (chỉ 1 lần)
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

        // ⛔ Dừng GPS khi rời màn hình
        locationService.StopTracking();
    }
}