using doanC_.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

// alias để tránh conflict
using MapControl = Microsoft.Maui.Controls.Maps.Map;

namespace doanC_.Services;

public class MapService
{
    // Focus bản đồ
    public void FocusToLocation(MapControl map, double latitude, double longitude)
    {
        var location = new Location(latitude, longitude);

        map.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                location,
                Distance.FromMeters(500)));
    }

    // Hiển thị vị trí user
    public void ShowUserLocation(MapControl map, double latitude, double longitude)
    {
        var location = new Location(latitude, longitude);

        var pin = new Pin
        {
            Label = "Bạn đang ở đây",
            Location = location,
            Type = PinType.SavedPin
        };

        map.Pins.Add(pin);
    }

    // Cập nhật vị trí user
    private Pin? userPin;

    public void UpdateUserLocation(MapControl map, double latitude, double longitude)
    {
        var location = new Location(latitude, longitude);

        if (userPin == null)
        {
            userPin = new Pin
            {
                Label = "Bạn đang ở đây",
                Location = location,
                Type = PinType.SavedPin
            };

            map.Pins.Add(userPin);
        }
        else
        {
            userPin.Location = location;
        }
    }

    // Hiển thị POI
    public void AddLocationPoints(MapControl map, List<LocationPoint> points)
    {
        foreach (var p in points)
        {
            var pin = new Pin
            {
                Label = p.Name,
                Address = p.Description,
                Location = new Location(p.Latitude, p.Longitude),
                Type = PinType.Place
            };

            map.Pins.Add(pin);
        }
    }

    // Xóa POI
    public void ClearPOI(MapControl map)
    {
        map.Pins.Clear();
    }
}