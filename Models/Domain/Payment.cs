using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public enum PaymentMethod
    {
        Bkash = 1,
        Nagad = 2,
        Rocket = 3
    }

    public enum PaymentVerificationStatus
    {
        Pending = 0,    // Submitted by customer, awaiting admin/owner verification
        Verified = 1,   // Confirmed by owner/admin
        Failed = 2,     // Rejected (invalid TX ID, wrong amount, etc.)
        Refunded = 3    // Refunded after cancellation
    }

    public enum PaymentType
    {
        Full = 1,
        Partial = 2     // Advance/deposit payment
    }

    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public PaymentVerificationStatus Status { get; set; } = PaymentVerificationStatus.Pending;

        [Required]
        public PaymentType PaymentType { get; set; } = PaymentType.Full;

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }

        public int? VerifiedByAdminId { get; set; }

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("VerifiedByAdminId")]
        public User? VerifiedByAdmin { get; set; }
    }
}
