using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public enum BookingStatus
    {
        PendingPayment = 0,  // Slot locked, awaiting payment submission
        SlotLocked = 1,      // Legacy/alias for PendingPayment
        Confirmed = 2,       // Payment verified, booking active
        Cancelled = 3,       // Cancelled by user or owner
        Expired = 4,         // Lock expired before payment was made
        Refunded = 5,        // Refund processed after cancellation
        Completed = 6        // Booking date/time has passed
    }

    public enum PaymentStatus
    {
        Unpaid = 0,
        PartiallyPaid = 1,   // Deposit paid, balance pending at venue
        FullyPaid = 2,
        Refunded = 3
    }

    public class Booking
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TurfId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public Guid SlotId { get; set; }

        [Required]
        public DateOnly BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalHours { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [StringLength(500)]
        public string? SpecialRequest { get; set; }

        public string? CancellationReason { get; set; }

        // The SlotLock ID that reserved this slot
        public Guid? SlotLockId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("SlotId")]
        public TurfSlot Slot { get; set; } = null!;

        // Payments for this booking
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
