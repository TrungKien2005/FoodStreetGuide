using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("DeviceTracking")]
    public class DeviceTracking
    {
        [Key]
        public int DeviceId { get; set; }

        public string DeviceUniqueId { get; set; } = string.Empty;

        public string? DeviceName { get; set; }

        public string? Platform { get; set; }

        public string? OSVersion { get; set; }

        public string? AppVersion { get; set; }

        public DateTime LastActivity { get; set; }

        public DateTime FirstSeen { get; set; }

        public double? LastLocationLat { get; set; }

        public double? LastLocationLng { get; set; }

        public bool IsActive { get; set; }

        public int TotalScans { get; set; }

        public int TotalListens { get; set; }
    }
}