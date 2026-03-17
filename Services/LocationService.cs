using Microsoft.Maui.Devices.Sensors;

namespace doanC_.Services;

public class LocationService
{
    private bool isTracking = false;

    // 🔁 Bắt đầu theo dõi liên tục
    public async Task StartTrackingAsync(Action<Location> onLocationUpdated)
    {
        if (isTracking) return;

        isTracking = true;

        while (isTracking)
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(10));

                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    onLocationUpdated?.Invoke(location);
                }
            }
            catch
            {
                // Có thể log lỗi ở đây
            }

            await Task.Delay(10000); // ⏱ 10 giây cập nhật 1 lần
        }
    }

    // ⛔ Dừng tracking
    public void StopTracking()
    {
        isTracking = false;
    }

    // 📍 Giữ lại nếu bạn vẫn cần gọi 1 lần
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(10));

            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch
        {
            return null;
        }
    }
}