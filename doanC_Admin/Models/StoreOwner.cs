using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanC_Admin.Models
{
    [Table("StoreOwners")]
    public class StoreOwner
    {
        [Key]
        public int OwnerId { get; set; }

        public int AdminId { get; set; }

        public string? PhoneNumber { get; set; }

        public string? IdentityNumber { get; set; }

        public string? TaxCode { get; set; }

        public string? BankAccount { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? Status { get; set; } = "Pending";

        [ForeignKey("AdminId")]
        public virtual AdminUser? AdminUser { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual AdminUser? Approver { get; set; }
    }
}