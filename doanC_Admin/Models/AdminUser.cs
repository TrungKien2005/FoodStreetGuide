using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("AdminUsers")]
    public class AdminUser
    {
        [Key]
        public int AdminId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; } = "Admin";

        public bool? IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        public DateTime? LastLogout { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    }
}