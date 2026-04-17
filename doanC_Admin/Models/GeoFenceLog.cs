using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("GeoFenceLogs")]
    public class GeoFenceLog
    {
        [Key]  // 👈 THÊM DÒNG NÀY
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeoLogId { get; set; }  // Đảm bảo có property này

        public int PointId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime? EnterTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public int? DurationSeconds { get; set; }
    }
}