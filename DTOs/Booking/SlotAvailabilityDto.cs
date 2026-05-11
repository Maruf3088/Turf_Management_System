namespace turf_management_system.DTOs.Booking
{
    public class SlotAvailabilityDto
    {
        public Guid SlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; } // Based on the slot's general availability
        public bool IsAlreadyBooked { get; set; } // Specifically for the requested date
    }
}
