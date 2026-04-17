using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("AudioFiles")]
    public class AudioFile
    {
        [Key]
        public int AudioId { get; set; }

        public int PointId { get; set; }

        public int LanguageId { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public int? Duration { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PointId")]
        public virtual LocationPoint? LocationPoint { get; set; }

        [ForeignKey("LanguageId")]
        public virtual Language? Language { get; set; }
    }
}