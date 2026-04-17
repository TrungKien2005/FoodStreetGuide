using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("QRCodes")]
    public class QRCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QrId { get; set; }

        public int PointId { get; set; }

        public string? Name { get; set; }

        public string? QrContent { get; set; }

        public string? QrImagePath { get; set; }

        public DateTime? CreatedAt { get; set; }

        [ForeignKey("PointId")]
        public virtual LocationPoint? LocationPoint { get; set; }
    }
}