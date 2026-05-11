using System.ComponentModel.DataAnnotations;

namespace turf_management_system.DTOs.Booking
{
    public class CreateBookingDto
    {
        [Required]
        public Guid TurfId { get; set; }

        [Required]
        public Guid SlotId { get; set; }

        [Required]
        public DateOnly BookingDate { get; set; }

        [StringLength(500)]
        public string? SpecialRequest { get; set; }
    }
}
