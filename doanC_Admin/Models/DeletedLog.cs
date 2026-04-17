using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("DeletedLogs")]
    public class DeletedLog
    {
        [Key]
        public int LogId { get; set; }

        public int PointId { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime DeletedAt { get; set; }

        [MaxLength(100)]
        public string DeletedBy { get; set; } = string.Empty;
    }
}