using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace doanC_.Services
{
    public class GeoFenceService
    {
        private const double TargetLat = 10.7715226;
        private const double TargetLng = 106.6923968;
        private const double Radius = 100; // mét

        public void CheckLocation(Location location)
        {
            double distance = CalculateDistance(
                location.Latitude,
                location.Longitude,
                TargetLat,
                TargetLng);

            if (distance <= Radius)
            {
                Console.WriteLine("Đã vào vùng geofence");
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000; // mét
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRad(double val) => val * Math.PI / 180;
    }
}
