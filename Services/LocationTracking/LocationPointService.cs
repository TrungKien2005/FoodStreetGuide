using doanC_.Models;

namespace doanC_.Services.LocationTracking;

public class LocationPointService
{
    public List<LocationPoint> GetLocations()
    {
        return new List<LocationPoint>
        {
            new LocationPoint(
                "Chợ Bến Thành",
                "Biểu tượng du lịch TP.HCM",
                10.7726,
                106.6980
            ),

            new LocationPoint(
                "Nhà thờ Đức Bà",
                "Nhà thờ nổi tiếng tại Sài Gòn",
                10.7798,
                106.6990
            ),

            new LocationPoint(
                "Bưu điện Thành phố",
                "Công trình kiến trúc Pháp nổi tiếng",
                10.7801,
                106.6995
            )
        };
    }
}