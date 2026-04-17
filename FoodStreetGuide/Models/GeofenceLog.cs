using System;

namespace doanC_.Models
{
    public class GeofenceLog
    {
        public int Id { get; set; }
        public int LocationPointId { get; set; }
        public string LocationPointName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public double Distance { get; set; }
        public bool IsInside { get; set; }
        public DateTime ScannedAt { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}