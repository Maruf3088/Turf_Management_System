using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace turf_management_system.Models.Domain
{
    /// <summary>
    /// Per-turf booking configuration. One config per turf (1:1 relationship).
    /// </summary>
    public class TurfBookingConfig
    {
        [Key]
        [ForeignKey("Turf")]
        public Guid TurfId { get; set; }

        // --- Available Days (bitmask) ---
        // Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64
        // e.g. Mon+Wed+Fri = 2+8+32 = 42
        public int AvailableDaysMask { get; set; } = 127; // All days open by default

        // --- Operating Hours ---
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(6, 0, 0);   // 6:00 AM
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0);  // 10:00 PM

        // --- Slot Duration ---
        public int SlotDurationMinutes { get; set; } = 60; // Default 60 minutes

        // --- Advance Booking ---
        public int MaxAdvanceBookingDays { get; set; } = 7; // Max 7 days ahead

        // --- Payment Rules ---
        public bool RequireFullPayment { get; set; } = false;

        [Column(TypeName = "decimal(5,2)")]
        public decimal AdvancePaymentPercent { get; set; } = 50; // 50% deposit by default

        public bool AcceptBkash { get; set; } = true;
        public bool AcceptNagad { get; set; } = true;
        public bool AcceptRocket { get; set; } = true;

        // --- Cancellation Rules ---
        public bool CancellationAllowed { get; set; } = true;

        [Column(TypeName = "decimal(5,2)")]
        public decimal RefundPercent { get; set; } = 100; // Full refund by default

        public int CancellationDeadlineHours { get; set; } = 24; // Must cancel 24h before

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Turf Turf { get; set; } = null!;

        // Helper: Check if a given DayOfWeek is available
        public bool IsDayAvailable(DayOfWeek day)
        {
            int bit = 1 << (int)day; // Sunday=1, Monday=2, etc.
            return (AvailableDaysMask & bit) != 0;
        }
    }
}
