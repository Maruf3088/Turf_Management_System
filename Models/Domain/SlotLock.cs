using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    /// <summary>
    /// Temporary slot reservation lock (5 minutes) to prevent race conditions.
    /// Auto-released by background job when expired.
    /// </summary>
    public class SlotLock
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TurfId { get; set; }

        [Required]
        public DateOnly BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int LockedByUserId { get; set; }

        /// <summary>Booking that this lock is associated with. Null until booking is created.</summary>
        public Guid? BookingId { get; set; }

        [Required]
        public DateTime LockedUntil { get; set; }

        public bool IsReleased { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("TurfId")]
        public Turf Turf { get; set; } = null!;

        [ForeignKey("LockedByUserId")]
        public User LockedByUser { get; set; } = null!;

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }

        // Helper: Check if this lock is still active
        public bool IsActive => !IsReleased && LockedUntil > DateTime.UtcNow;
    }
}
