using doanC_.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Diagnostics;
using doanC_.Services.Audio;

namespace doanC_.Services;

/// <summary>
/// Geofence Service - Phát audio khi vào vùng POI
/// </summary>
public class GeofenceService
{
    private HashSet<int> _triggeredPoiIds = new();  // ? L?u POI ?ã phát
    private double _geofenceRadius = 50;  // ? Bán kính (mét)
    private TTSService _ttsService;
    
    // ? Event: Khi vào vùng POI
    public event Action<LocationPoint>? OnGeofenceEntered;

    public GeofenceService(TTSService ttsService)
    {
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
    }

    /// <summary>
    /// C?p nh?t bán kính t? Settings
  /// </summary>
    public void UpdateRadius(string radiusText)
    {
      // Map t? text ("15 mét") sang s? (15)
    if (radiusText.Contains("15"))
       _geofenceRadius = 15;
        else if (radiusText.Contains("20"))
            _geofenceRadius = 20;
        else if (radiusText.Contains("25"))
            _geofenceRadius = 25;
        else if (radiusText.Contains("30"))
       _geofenceRadius = 30;
      
        Debug.WriteLine($"[GeofenceService] Radius updated: {_geofenceRadius}m");
    }

    /// <summary>
    /// Ki?m tra và kích ho?t geofence
    /// </summary>
    public async Task CheckGeofenceAsync(Location userLocation, List<LocationPoint> poiList, string language = "vi")
    {
        if (userLocation == null || poiList == null || poiList.Count == 0)
            return;

        foreach (var poi in poiList)
   {
            // ? Tính kho?ng cách gi?a user và POI
    double distance = CalculateDistance(
            userLocation.Latitude, userLocation.Longitude,
     poi.Latitude, poi.Longitude);

            Debug.WriteLine($"[GeofenceService] ?? {poi.Name}: {distance:F0}m (Radius: {_geofenceRadius}m)");

      // ? N?u trong vùng và ch?a phát
            if (distance <= _geofenceRadius && !_triggeredPoiIds.Contains(poi.PointId))
            {
    Debug.WriteLine($"[GeofenceService] ?? Entered geofence: {poi.Name}");
   _triggeredPoiIds.Add(poi.PointId);  // ? ?ánh d?u ?ã phát
       
        // ? Phát audio
     OnGeofenceEntered?.Invoke(poi);
        await PlayGeofenceAudioAsync(poi, language);
        }
        // ? N?u ra ngoài vùng, xóa kh?i triggered list
   else if (distance > _geofenceRadius && _triggeredPoiIds.Contains(poi.PointId))
            {
    Debug.WriteLine($"[GeofenceService] ?? Left geofence: {poi.Name}");
            _triggeredPoiIds.Remove(poi.PointId);  // ? Reset ?? phát l?i khi vào
   }
        }
    }

    /// <summary>
    /// Phát audio geofence
    /// </summary>
    private async Task PlayGeofenceAudioAsync(LocationPoint poi, string language)
    {
        try
        {
 string textToSpeak = $"{poi.Name}. {poi.Description ?? poi.Name}";
            
      Debug.WriteLine($"[GeofenceService] ?? Playing geofence audio: {poi.Name}");
            
            var selectedVoice = Preferences.Get("SelectedVoice", "Gi?ng n?");
            await _ttsService.SpeakAsync(textToSpeak, ConvertLanguageCode(language), selectedVoice);
    
         Debug.WriteLine($"[GeofenceService] ? Geofence audio completed");
 }
        catch (Exception ex)
        {
Debug.WriteLine($"[GeofenceService] ? Error playing geofence audio: {ex.Message}");
        }
    }

    /// <summary>
  /// Tính kho?ng cách (Haversine formula)
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;  // Bán kính Trái ??t (m)
 double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
     Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

  return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Convert language code ?? g?i TTS
    /// </summary>
    private string ConvertLanguageCode(string code)
    {
return code switch
        {
            "vi" => "vi-VN",
            "en" => "en-US",
      "fr" => "fr-FR",
   "es" => "es-ES",
      "zh" => "zh-CN",
            "ja" => "ja-JP",
     "ko" => "ko-KR",
            _ => "vi-VN"
        };
    }

  /// <summary>
    /// Reset toàn b? triggered POIs
    /// </summary>
    public void ResetTriggeredPois()
    {
        _triggeredPoiIds.Clear();
        Debug.WriteLine("[GeofenceService] ?? Triggered POIs reset");
    }
}
