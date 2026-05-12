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
    }

    public enum SlotStatus
    {
        Available = 0,
        Locked = 1,     // Temporarily reserved (within 5 min lock window)
        Booked = 2,     // Confirmed booking exists
        Unavailable = 3 // Owner disabled this slot
    }
}
