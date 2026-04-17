using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("AdminLoginLogs")]
    public class AdminLoginLog
    {
        [Key]
        public int LogId { get; set; }

        public int AdminId { get; set; }

        public string Username { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        public string? IPAddress { get; set; }

        public string? DeviceInfo { get; set; }

        public string? Status { get; set; } = "Success";

        public string? FailureReason { get; set; }

        [ForeignKey("AdminId")]
        public virtual AdminUser? AdminUser { get; set; }
    }
}