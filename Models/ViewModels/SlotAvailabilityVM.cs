namespace turf_management_system.Models.ViewModels
{
    public class SlotAvailabilityVM
    {
        public Guid SlotId { get; set; }
        public string StartTimeDisplay { get; set; } = string.Empty;  // e.g. "06:00"
        public string EndTimeDisplay { get; set; } = string.Empty;    // e.g. "07:00"
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public SlotStatus Status { get; set; }
        public string PricingVariant { get; set; } = "Morning";
    }

    public enum SlotStatus
    {
        Available = 0,
        Selected = 1,      // Locked by the current active user
        InProgress = 2,    // Locked by another user (in progress)
        Booked = 3,        // Confirmed booking exists
        Unavailable = 4    // Owner disabled this slot
    }

}
