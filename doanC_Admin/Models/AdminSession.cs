using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("AdminSessions")]
    public class AdminSession
    {
        [Key]
        public int SessionId { get; set; }

        public int AdminId { get; set; }

        public string Username { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; }

        public DateTime LastActivity { get; set; }

        public DateTime LastHeartbeat { get; set; } = DateTime.Now;

        public string? SessionToken { get; set; }

        public string? IPAddress { get; set; }

        public string? DeviceInfo { get; set; }

        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;

        public int SessionTimeoutMinutes { get; set; } = 30;

        [ForeignKey("AdminId")]
        public virtual AdminUser? AdminUser { get; set; }
    }
}