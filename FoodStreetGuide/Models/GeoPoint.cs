using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace doanC_.Models
{
    public class GeoPoint
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        private List<GeoPoint> _points = new List<GeoPoint>
        {
            new GeoPoint
            {
                Name = "Trường",
                Latitude = 10.7715226,
                Longitude = 106.6923968,
                Radius = 100
            },
            new GeoPoint
            {
                Name = "Nhà",
                Latitude = 10.772,
                Longitude = 106.693,
                Radius = 80
            }
        };
    }
}
