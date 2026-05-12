using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using turf_management_system.Models.Enums;

namespace turf_management_system.Models.Domain
{
    public class TurfOwner
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? BusinessAddress { get; set; }

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        [StringLength(50)]
        public string? NationalIdNumber { get; set; }

        [StringLength(255)]
        public string? NidFrontImagePath { get; set; }

        [StringLength(255)]
        public string? NidBackImagePath { get; set; }

        [StringLength(255)]
        public string? TradeLicenseImagePath { get; set; }

        [StringLength(255)]
        public string? UtilityBillImagePath { get; set; }

        [StringLength(1000)]
        public string? AdminComments { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}
