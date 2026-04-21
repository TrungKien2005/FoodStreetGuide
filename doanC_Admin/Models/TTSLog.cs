using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("TTSLogs")]
    public class TTSLog
    {
        [Key]
        public int TtsLogId { get; set; }
        public int PointId { get; set; }
        public int LanguageId { get; set; }
        public DateTime PlayedAt { get; set; }  
        public int? DurationSeconds { get; set; }
        [ForeignKey("PointId")]
        public virtual LocationPoint? LocationPoint { get; set; }
    }
}