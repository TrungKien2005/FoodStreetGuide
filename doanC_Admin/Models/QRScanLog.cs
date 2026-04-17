using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("QRScanLogs")]
    public class QRScanLog
    {
        [Key]
        public int LogId { get; set; }
        public int PointId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime ScanTime { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [ForeignKey("PointId")]
        public virtual LocationPoint? LocationPoint { get; set; }
    }
}