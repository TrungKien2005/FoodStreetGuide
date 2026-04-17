using System.Diagnostics;
using doanC_.Helpers;
using doanC_.Models;
using doanC_.Services.Data;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

// alias để tránh conflict
using MapControl = Microsoft.Maui.Controls.Maps.Map;

namespace doanC_.Services;

public class MapService
{
    private readonly SQLiteService _sqlite;
    private Pin? _userPin;

    public MapService()
    {
        _sqlite = ServiceHelper.GetService<SQLiteService>();
    }

    /// <summary>
    /// Focus bản đồ đến một vị trí
    /// </summary>
    public void FocusToLocation(MapControl map, double latitude, double longitude, double zoomInMeters = 500)
    {
        try
        {
            var location = new Location(latitude, longitude);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(zoomInMeters)));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapService] FocusToLocation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Hiển thị vị trí user (tạo pin mới)
    /// </summary>
    public void ShowUserLocation(MapControl map, double latitude, double longitude)
    {
        try
        {
            var location = new Location(latitude, longitude);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var pin = new Pin
                {
                    Label = "Bạn đang ở đây",
                    Location = location,
                    Type = PinType.SavedPin
                };
                map.Pins.Add(pin);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapService] ShowUserLocation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật vị trí user (cập nhật pin cũ hoặc tạo mới)
    /// </summary>
    public void UpdateUserLocation(MapControl map, double latitude, double longitude)
    {
        try
        {
            var location = new Location(latitude, longitude);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_userPin == null)
                {
                    _userPin = new Pin
                    {
                        Label = "Bạn đang ở đây",
                        Location = location,
                        Type = PinType.SavedPin
                    };
                    map.Pins.Add(_userPin);
                }
                else
                {
                    _userPin.Location = location;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapService] UpdateUserLocation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Hiển thị POI từ danh sách
    /// </summary>
    public void AddLocationPoints(MapControl map, List<LocationPoint> points, bool clearExisting = false)
    {
        if (points == null || points.Count == 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (clearExisting)
                map.Pins.Clear();

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
        });
    }

    /// <summary>
    /// Hiển thị POI lấy từ SQLite
    /// </summary>
    public async Task AddLocationPointsFromDbAsync(MapControl map, bool clearExisting = true)
    {
        try
        {
            // Ensure DB/tables exist
            await _sqlite.InitializeAsync();

            var points = await _sqlite.GetAllLocationPointsAsync();

            if (points == null || points.Count == 0)
            {
                Debug.WriteLine("[MapService] No POI found in database");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (clearExisting)
                    map.Pins.Clear();

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
            });

            Debug.WriteLine($"[MapService] Added {points.Count} POI pins to map");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapService] AddLocationPointsFromDbAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Xóa tất cả POI trên bản đồ
    /// </summary>
    public void ClearPOI(MapControl map)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            map.Pins.Clear();
            _userPin = null;
        });
    }

    /// <summary>
    /// Lấy danh sách POI từ database
    /// </summary>
    public async Task<List<LocationPoint>> GetPOIFromDatabaseAsync()
    {
        try
        {
            await _sqlite.InitializeAsync();
            return await _sqlite.GetAllLocationPointsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapService] GetPOIFromDatabaseAsync error: {ex.Message}");
            return new List<LocationPoint>();
        }
    }
}