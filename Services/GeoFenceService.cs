using System;
using System.Text.Json;
using doanC_.Models;
using Microsoft.Maui.Media;

namespace doanC_.Services
{
    public class GeoFenceService
    {
        public static GeoFenceService Instance { get; } = new GeoFenceService();

        private List<LocationPoint> _points = new List<LocationPoint>
        {
            new LocationPoint("Trường", "ĐH", 10.7715226, 106.6923968),
            new LocationPoint("Nhà", "Nhà riêng", 10.772, 106.693)
        };

        private Dictionary<string, bool> _insideStates = new();
        private Dictionary<string, DateTime> _lastTriggerTimes = new();

        private const double Radius = 100;

        private DateTime _lastCheckTime = DateTime.MinValue;
        private DateTime _lastHeartbeatTime = DateTime.MinValue;

        private readonly TimeSpan DebounceTime = TimeSpan.FromSeconds(3);
        private readonly TimeSpan CooldownTime = TimeSpan.FromMinutes(5);
        private readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

        public void AddPoint(LocationPoint point)
        {
            _points.Add(point);
            Console.WriteLine($"[Geo] Added: {point.Name}");
        }

        public void CheckLocation(Location location)
        {
            var now = DateTime.Now;

            // 🟡 Debounce
            if (now - _lastCheckTime < DebounceTime)
                return;

            _lastCheckTime = now;

            foreach (var point in _points)
            {
                double distance = CalculateDistance(
                    location.Latitude,
                    location.Longitude,
                    point.Latitude,
                    point.Longitude);

                bool isInsideNow = distance <= Radius;

                // init state
                if (!_insideStates.ContainsKey(point.Name))
                    _insideStates[point.Name] = false;

                if (!_lastTriggerTimes.ContainsKey(point.Name))
                    _lastTriggerTimes[point.Name] = DateTime.MinValue;

                bool wasInside = _insideStates[point.Name];

                // 🟢 HEARTBEAT
                if (now - _lastHeartbeatTime >= HeartbeatInterval)
                {
                    Console.WriteLine($"[Heartbeat] {point.Name} | Distance: {distance:F2}m | Inside: {isInsideNow}");
                }

                // 🟢 ENTER
                if (!wasInside && isInsideNow)
                {
                    if (now - _lastTriggerTimes[point.Name] >= CooldownTime)
                    {
                        Console.WriteLine($">>> ENTER {point.Name}");

                        _lastTriggerTimes[point.Name] = now;

                        _ = SpeakAsync($"Bạn đã đến {point.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"{point.Name} đang cooldown");
                    }
                }

                // 🔵 EXIT
                if (wasInside && !isInsideNow)
                {
                    Console.WriteLine($"<<< EXIT {point.Name}");
                }

                _insideStates[point.Name] = isInsideNow;
            }

            // cập nhật heartbeat sau vòng lặp
            if (now - _lastHeartbeatTime >= HeartbeatInterval)
            {
                _lastHeartbeatTime = now;
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
            await TextToSpeech.SpeakAsync(text);
        }
        private GeoFenceService _geoFenceService = new GeoFenceService();
    }
}