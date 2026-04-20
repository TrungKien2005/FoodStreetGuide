using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("StoreOwners")]
    public class StoreOwner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OwnerId { get; set; }

        [Required]
        public int AdminId { get; set; }

        [Required, MaxLength(200)]
        public string StoreName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Phone, MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? IdentityNumber { get; set; }

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(50)]
        public string? BankAccount { get; set; }

        [MaxLength(50)]
        public string? BankName { get; set; }

        // Approval fields
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Status: Pending, Approved, Rejected
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // Rejection reason
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Statistics
        public int TotalLocations { get; set; } = 0;
        public int PendingLocations { get; set; } = 0;
        public int ApprovedLocations { get; set; } = 0;
        public int RejectedLocations { get; set; } = 0;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AdminId")]
        public virtual AdminUser? AdminUser { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual AdminUser? Approver { get; set; }
    }
}