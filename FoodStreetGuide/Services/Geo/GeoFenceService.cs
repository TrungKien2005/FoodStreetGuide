using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using doanC_.Models;
using doanC_.Services.Data;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using doanC_.Helpers;

namespace doanC_.Services.Geo
{
    public class GeoFenceService
    {
        public static GeoFenceService Instance { get; } = new GeoFenceService();

        private List<LocationPoint> _points = new List<LocationPoint>();
        private Dictionary<string, bool> _insideStates = new();
        private Dictionary<string, DateTime> _lastTriggerTimes = new();

        // ✅ Thay hardcode bằng biến có thể thay đổi
        private double _radius = 15; // Mặc định 15 mét (khớp với Settings)

        private DateTime _lastCheckTime = DateTime.MinValue;
        private DateTime _lastHeartbeatTime = DateTime.MinValue;

        private readonly TimeSpan DebounceTime = TimeSpan.FromSeconds(3);
        private readonly TimeSpan CooldownTime = TimeSpan.FromMinutes(5);
        private readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

        private readonly SQLiteService _sqliteService;

        // ✅ Property để lấy bán kính hiện tại
        public double Radius
        {
            get => _radius;
            private set => _radius = value;
        }

        public GeoFenceService()
        {
            _sqliteService = ServiceHelper.GetService<SQLiteService>();
            LoadPointsFromDatabase();

            // ✅ Đọc bán kính đã lưu từ Preferences
            LoadRadiusFromPreferences();
        }

        // ✅ Method để đọc bán kính từ Preferences
        private void LoadRadiusFromPreferences()
        {
            try
            {
                var savedRadius = Preferences.Get("GeoFenceRadiusValue", 15.0);
                _radius = savedRadius;
                Debug.WriteLine($"[Geo] Loaded radius from preferences: {_radius}m");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Geo] Failed to load radius: {ex.Message}");
            }
        }

        // ✅ Method để cập nhật bán kính (được gọi từ SettingsPage)
        public void UpdateRadius(double newRadius)
        {
            _radius = newRadius;
            Debug.WriteLine($"[Geo] Radius updated to: {_radius}m");

            // Reset trạng thái inside để áp dụng bán kính mới
            _insideStates.Clear();
            _lastTriggerTimes.Clear();
        }

        private async void LoadPointsFromDatabase()
        {
            try
            {
                if (_sqliteService == null)
                {
                    Debug.WriteLine("[Geo] SQLiteService is null");
                    return;
                }

                var points = await _sqliteService.GetAllLocationPointsAsync();

                if (points != null && points.Any())
                {
                    _points = points;
                    Debug.WriteLine($"[Geo] Loaded {_points.Count} points from SQLite");
                    Debug.WriteLine($"[Geo] Current geofence radius: {_radius}m");

                    foreach (var p in _points)
                    {
                        Debug.WriteLine($"[Geo] POI: {p.Name} ({p.Latitude}, {p.Longitude})");
                    }
                }
                else
                {
                    Debug.WriteLine("[Geo] No points found in SQLite");
                    _points = new List<LocationPoint>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Geo] Failed to load points: {ex.Message}");
            }
        }

        public void AddPoint(LocationPoint point)
        {
            _points.Add(point);
            Debug.WriteLine($"[Geo] Added: {point.Name}");
        }

        public async void CheckLocation(Location location)
        {
            if (_points == null || _points.Count == 0)
            {
                Debug.WriteLine("[Geo] No points to check, loading...");
                LoadPointsFromDatabase();
                return;
            }

            var now = DateTime.Now;

            if (now - _lastCheckTime < DebounceTime)
                return;

            _lastCheckTime = now;

            Debug.WriteLine($"[Geo] Checking location: {location.Latitude}, {location.Longitude} with radius: {_radius}m");

            foreach (var point in _points)
            {
                double distance = CalculateDistance(
                    location.Latitude,
                    location.Longitude,
                    point.Latitude,
                    point.Longitude);

                bool isInsideNow = distance <= _radius;

                string key = point.Name ?? string.Empty;

                if (!_insideStates.ContainsKey(key))
                    _insideStates[key] = false;

                if (!_lastTriggerTimes.ContainsKey(key))
                    _lastTriggerTimes[key] = DateTime.MinValue;

                bool wasInside = _insideStates[key];

                // Log khi gần
                if (distance < 200)
                {
                    Debug.WriteLine($"[Geo] {point.Name}: distance={distance:F2}m, radius={_radius}m, inside={isInsideNow}");
                }

                if (!wasInside && isInsideNow)
                {
                    if (now - _lastTriggerTimes[key] >= CooldownTime)
                    {
                        Debug.WriteLine($">>> ENTER {point.Name} - Distance: {distance:F2}m (Radius: {_radius}m)");
                        _lastTriggerTimes[key] = now;
                        await SpeakAsync($"Bạn đã đến {point.Name}. Khoảng cách {distance:F0} mét.");
                    }
                    else
                    {
                        Debug.WriteLine($"[Cooldown] {point.Name} - Chưa hết thời gian chờ");
                    }
                }

                if (wasInside && !isInsideNow)
                {
                    Debug.WriteLine($"<<< EXIT {point.Name}");
                }

                _insideStates[key] = isInsideNow;
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRad(double val) => val * Math.PI / 180;

        private async Task SpeakAsync(string text)
        {
            try
            {
                await TextToSpeech.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TTS] Error: {ex.Message}");
            }
        }

        public async Task<List<LocationPoint>> GetNearbyPoints(Location location, double radiusInMeters = 500)
        {
            if (_points == null || _points.Count == 0)
            {
                await Task.Run(() => LoadPointsFromDatabase());
            }

            var nearby = new List<(LocationPoint Point, double Distance)>();

            foreach (var point in _points)
            {
                double distance = CalculateDistance(
                    location.Latitude,
                    location.Longitude,
                    point.Latitude,
                    point.Longitude);

                if (distance <= radiusInMeters)
                {
                    nearby.Add((point, distance));
                }
            }

            return nearby
                .OrderBy(p => p.Distance)
                .Take(10)
                .Select(p => p.Point)
                .ToList();
        }

        // ✅ Method để lấy bán kính hiện tại
        public double GetCurrentRadius()
        {
            return _radius;
        }
    }
}