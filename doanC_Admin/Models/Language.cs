using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key]
        public int LanguageId { get; set; }

        public string LanguageCode { get; set; } = string.Empty;

        public string LanguageName { get; set; } = string.Empty;

        public string? FlagIcon { get; set; } 

        public int DisplayOrder { get; set; } = 0; 

        public bool IsActive { get; set; } = true;
    }
}