using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Rejected = 2,
        Cancelled = 3,
        Completed = 4
    }

    public enum PaymentStatus
    {
        Unpaid = 0,
        Paid = 1,
        Refunded = 2
    }

    public class Booking
    {
        [Key]
        public Guid Id { get; set; }

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

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [StringLength(500)]
        public string? SpecialRequest { get; set; }

        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("SlotId")]
        public TurfSlot Slot { get; set; } = null!;
    }
}
