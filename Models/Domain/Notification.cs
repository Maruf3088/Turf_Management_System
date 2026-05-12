using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    public enum NotificationType
    {
        BookingCreated = 1,
        BookingConfirmed = 2,
        BookingCancelled = 3,
        BookingExpired = 4,
        PaymentSubmitted = 5,
        PaymentVerified = 6,
        PaymentFailed = 7,
        RefundProcessed = 8,
        SlotLocked = 9,
        General = 10
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; } = NotificationType.General;

        /// <summary>Optional link to the related entity (e.g. bookingId)</summary>
        [StringLength(200)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
