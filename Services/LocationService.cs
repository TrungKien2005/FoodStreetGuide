using Microsoft.Maui.Devices.Sensors;

namespace doanC_.Services;

public class LocationService
{
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(10));
            // gọi api để lấy vị trí
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch
        {
            return null;
        }
    }
}