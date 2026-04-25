using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("LocationPoints")]
    public class LocationPoint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PointId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double Radius { get; set; } = 100;

        public string? AudioFile { get; set; }

        public string Language { get; set; } = "vi";

        public string? Address { get; set; }

        public string? Category { get; set; }

        public string? Image { get; set; }

        public double Rating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public string? OpeningHours { get; set; }

        public string? PriceRange { get; set; }

        public string? Phone { get; set; }

        // ✅ Owner & Approval fields
        public int? OwnerId { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsApproved { get; set; } = false;
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; } 

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual AdminUser? Admin { get; set; }

        [ForeignKey("OwnerId")]
        public virtual StoreOwner? Owner { get; set; }
        // Navigation collections
        public virtual ICollection<TTSLog>? TTSLogs { get; set; }
        public virtual ICollection<QRScanLog>? QRScanLogs { get; set; }
        public virtual ICollection<GeoFenceLog>? GeoFenceLogs { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual AdminUser? Approver { get; set; }
    }
}